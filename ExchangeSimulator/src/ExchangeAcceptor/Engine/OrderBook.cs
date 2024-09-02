namespace ExchangeAcceptor.Engine {
    using ExchangeAcceptor.Entities;
    using QuickFix;
    using QuickFix.Fields;
    using Serilog;
    using Serilog.Events;
    using System.Threading;

    public class OrderBook {
        private readonly ILogger _log = Serilog.Log.Logger;
        private readonly MarketDataManager _marketDataManager;
        private Timer _timer;
        private SortedDictionary<decimal, List<Fix>> _buyOrders = new SortedDictionary<decimal, List<Fix>>(Comparer<decimal>.Create((x, y) => y.CompareTo(x)));
        private SortedDictionary<decimal, List<Fix>> _sellOrders = new SortedDictionary<decimal, List<Fix>>();

        public decimal BestBid => _buyOrders.Any() ? _buyOrders.First().Key : 0;
        public decimal BestAsk => _sellOrders.Any() ? _sellOrders.First().Key : 0;
        private string Symbol { get; }
        private decimal _quantityTraded = decimal.Zero;
        private decimal _priceTraded = decimal.Zero;
        public OrderBook(string symbol, MarketDataManager marketDataManager) {
            Symbol = symbol;

            _timer = new Timer(PrintOrderBook, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            _marketDataManager = marketDataManager;
        }

        public void AddOrder(Fix order) {
            var orders = order.Side == Side.BUY ? _buyOrders : _sellOrders;

            if (!orders.ContainsKey(order.Price)) {
                orders[order.Price] = new List<Fix>();
            }

            orders[order.Price].Add(order);
            MatchOrders();
        }

        public void RemoveOrder(Fix order) {
            var orders = order.Side == Side.BUY ? _buyOrders : _sellOrders;

            if (orders.ContainsKey(order.Price)) {
                orders[order.Price].Remove(order);
                if (!orders[order.Price].Any()) {
                    orders.Remove(order.Price);
                }
            }
        }

        private void MatchOrders() {
            while (_buyOrders.Any() && _sellOrders.Any()) {
                var highestBuyOrder = _buyOrders.First();
                var lowestSellOrder = _sellOrders.First();

                var buyOrder = highestBuyOrder.Value.First();
                var sellOrder = lowestSellOrder.Value.First();

                decimal tradePrice;

                if (buyOrder.OrdType == OrdType.MARKET && sellOrder.OrdType == OrdType.MARKET) {
                    // Executar ao preço mais próximo do mercado
                    tradePrice = lowestSellOrder.Key; // Pode ser qualquer um, já que é market vs market
                } else if (buyOrder.OrdType == OrdType.MARKET) {
                    tradePrice = lowestSellOrder.Key; // Market buy, executar ao preço de venda
                } else if (sellOrder.OrdType == OrdType.MARKET) {
                    tradePrice = highestBuyOrder.Key; // Market sell, executar ao preço de compra
                } else if (highestBuyOrder.Key >= lowestSellOrder.Key) {
                    tradePrice = lowestSellOrder.Key; // Limit vs Limit, executa ao preço mais baixo de venda
                } else {
                    break; // Não há execução possível se os preços não se cruzam
                }

                var matchedQuantity = Math.Min(buyOrder.OrderQty, sellOrder.OrderQty);

                if (_log.IsEnabled(LogEventLevel.Debug)) {
                    _log.Debug($"Matched {matchedQuantity} units at {tradePrice}");
                }

                buyOrder.OrderQty -= matchedQuantity;
                buyOrder.SetLastQty(matchedQuantity);

                sellOrder.OrderQty -= matchedQuantity;
                sellOrder.SetLastQty(matchedQuantity);

                _quantityTraded += matchedQuantity;
                _priceTraded += tradePrice;

                // Verificar se as ordens são "Partial Fill" ou "Filled"
                if (buyOrder.OrderQty > 0) {
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"Order {buyOrder.OrderID} is Partially Filled: {matchedQuantity} units executed, {buyOrder.OrderQty} units remaining.");
                    }
                    PartiallyFilled(buyOrder);
                } else {
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"Order {buyOrder.OrderID} is Fully Filled: {matchedQuantity} units executed.");
                    }
                    RemoveOrder(buyOrder); // Remover ordem do livro
                    FullyFilled(buyOrder);
                }

                if (sellOrder.OrderQty > 0) {
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"Order {sellOrder.OrderID} is Partially Filled: {matchedQuantity} units executed, {sellOrder.OrderQty} units remaining.");
                    }
                    PartiallyFilled(sellOrder);
                } else {
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Information($"Order {sellOrder.OrderID} is Fully Filled: {matchedQuantity} units executed.");
                    }
                    RemoveOrder(sellOrder); // Remover ordem do livro
                    FullyFilled(sellOrder);
                }
                SendMarketDataNotification();
            }
        }

        public decimal GetMarketPrice() {
            if (BestBid == 0 || BestAsk == 0) {
                return 0; // Se não houver ofertas, não há preço de mercado
            }

            return (BestBid + BestAsk) / 2; // Média simples do Bid e Ask
        }

        // Método para imprimir o livro de ofertas com o preço de mercado
        public void PrintOrderBook(object ob) {
            Console.WriteLine($"Symbol {Symbol}");
            Console.WriteLine("Buy Orders:");
            var buy = _buyOrders.Keys.ToList();
            foreach (var price in buy) {
                var book = _buyOrders[price];
                if (book.Any()) {
                    Console.WriteLine($"Price: {price}, Quantity: {book.Sum(x => x.OrderQty)}");
                }
            }

            Console.WriteLine("Sell Orders:");
            var sell = _sellOrders.Keys.ToList();
            foreach (var price in sell) {
                var book = _sellOrders[price];
                if (book.Any()) {
                    Console.WriteLine($"Price: {price}, Quantity: {book.Sum(x => x.OrderQty)}");
                }
            }

            // Exibir o preço de mercado
            Console.WriteLine($"Market Price: {GetMarketPrice()} (Best Bid: {BestBid}, Best Ask: {BestAsk})");
        }

        private void PartiallyFilled(Fix order) {
            var msg = new QuickFix.FIX44.ExecutionReport();

            msg.SetField(new OrderID(order.OrderID)); // ID da Ordem
            msg.SetField(new ExecID(Guid.NewGuid().ToString("N"))); // ID da Execução

            msg.SetField(new ExecType(ExecType.TRADE)); // Tipo de Execução (Partial Fill, Fill, etc.)
            msg.SetField(new OrdStatus(OrdStatus.PARTIALLY_FILLED)); // Status da Ordem (New, Partially Filled, Filled, etc.)

            msg.SetField(new Side(order.Side));
            msg.SetField(new LeavesQty(order.LeavesQty)); // Quantidade Restante
            msg.SetField(new CumQty(order.CumQty)); // Quantidade Acumulada
            msg.SetField(new AvgPx(order.AvgPx)); // Preço Médio

            // Campos opcionais que você pode adicionar conforme necessário
            msg.SetField(new LastQty(order.LastQty)); // Quantidade na última execução
            msg.SetField(new LastPx(order.LastPx)); // Preço na última execução
            msg.SetField(new ClOrdID(order.ClOrdID));
            msg.SetField(new TransactTime(DateTime.UtcNow)); // Hora da Transação

            msg.SetField(new OrdType(order.OrdType));
            msg.SetField(new Symbol(order.Symbol));

            msg.SetField(new OrderQty(order.OrderQty));

            msg.SetField(new Price(order.Price));
            msg.SetField(new TimeInForce(order.TimeInForce));

            msg.SetField(new LeavesQty(10));
            msg.SetField(new CumQty(10));
            msg.SetField(new AvgPx(CalculateAvgPx()));

            // Enviar a mensagem            
            Session.SendToTarget(msg, order.Session);
        }

        private void FullyFilled(Fix order) {
            var msg = new QuickFix.FIX44.ExecutionReport();

            msg.SetField(new OrderID(order.OrderID)); // ID da Ordem
            msg.SetField(new ExecID(Guid.NewGuid().ToString("N"))); // ID da Execução

            msg.SetField(new ExecType(ExecType.TRADE)); // Tipo de Execução (Partial Fill, Fill, etc.)
            msg.SetField(new OrdStatus(OrdStatus.FILLED)); // Status da Ordem (New, Partially Filled, Filled, etc.)

            msg.SetField(new Side(order.Side));
            msg.SetField(new LeavesQty(order.LeavesQty)); // Quantidade Restante
            msg.SetField(new CumQty(order.CumQty)); // Quantidade Acumulada
            msg.SetField(new AvgPx(order.AvgPx)); // Preço Médio

            // Campos opcionais que você pode adicionar conforme necessário
            msg.SetField(new LastQty(order.LastQty)); // Quantidade na última execução
            msg.SetField(new LastPx(order.LastPx)); // Preço na última execução
            msg.SetField(new ClOrdID(order.ClOrdID));
            msg.SetField(new TransactTime(DateTime.UtcNow)); // Hora da Transação

            msg.SetField(new OrdType(order.OrdType));
            msg.SetField(new Symbol(order.Symbol));

            msg.SetField(new OrderQty(order.OrderQty));

            msg.SetField(new Price(order.Price));
            msg.SetField(new TimeInForce(order.TimeInForce));

            msg.SetField(new LeavesQty(10));
            msg.SetField(new CumQty(10));
            msg.SetField(new AvgPx(CalculateAvgPx()));

            // Enviar a mensagem            
            Session.SendToTarget(msg, order.Session);
        }

        private void SendMarketDataNotification() {
            // Notificar os novos melhores preços de compra e venda
            var bestBid = _buyOrders.Any() ? _buyOrders.First().Key : 0;
            var bestAsk = _sellOrders.Any() ? _sellOrders.First().Key : 0;

            //OnMarketDataUpdate?.Invoke(bestBid, bestAsk);
        }

        public decimal CalculateAvgPx() {
            if (_priceTraded == decimal.Zero || _quantityTraded == decimal.Zero) {
                return decimal.Zero;
            }
            return _priceTraded / _quantityTraded;
        }
    }
}