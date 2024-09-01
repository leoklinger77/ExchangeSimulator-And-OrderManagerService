namespace ExchangeAcceptor.Exchange {
    using ExchangeAcceptor.Entities;
    using QuickFix;
    using QuickFix.Fields;
    using Serilog;
    using System.Threading;

    public class OrderBook {
        private readonly ILogger _log = Serilog.Log.Logger;
        private Timer _timer;
        private SortedDictionary<decimal, List<Fix>> buyOrders = new SortedDictionary<decimal, List<Fix>>(Comparer<decimal>.Create((x, y) => y.CompareTo(x)));
        private SortedDictionary<decimal, List<Fix>> sellOrders = new SortedDictionary<decimal, List<Fix>>();

        private List<AvgPxExecution> _execution = new List<AvgPxExecution>();
        private string Symbol { get; }
        public OrderBook(string symbol) {
            Symbol = symbol;

            _timer = new Timer(PrintOrderBook, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public void AddOrder(Fix order) {
            var orders = order.Side == Side.BUY ? buyOrders : sellOrders;

            if (!orders.ContainsKey(order.Price)) {
                orders[order.Price] = new List<Fix>();
            }

            orders[order.Price].Add(order);
            MatchOrders();
            SendMarketDataNotification();
        }

        public void RemoveOrder(Fix order) {
            var orders = order.Side == Side.BUY ? buyOrders : sellOrders;

            if (orders.ContainsKey(order.Price)) {
                orders[order.Price].Remove(order);
                if (!orders[order.Price].Any()) {
                    orders.Remove(order.Price);
                }
                SendMarketDataNotification();
            }
        }

        private void MatchOrders() {
            while (buyOrders.Any() && sellOrders.Any()) {
                var highestBuyOrder = buyOrders.First();
                var lowestSellOrder = sellOrders.First();

                if (highestBuyOrder.Key >= lowestSellOrder.Key) {
                    var buyOrder = highestBuyOrder.Value.First();
                    var sellOrder = lowestSellOrder.Value.First();

                    var matchedQuantity = Math.Min(buyOrder.OrderQty, sellOrder.OrderQty);

                    _log.Information($"Matched {matchedQuantity} units at {lowestSellOrder.Key}");

                    buyOrder.OrderQty -= matchedQuantity;
                    buyOrder.SetLastQty(matchedQuantity);
                    
                    sellOrder.OrderQty -= matchedQuantity;
                    sellOrder.SetLastQty(matchedQuantity);

                    _execution.Add(new AvgPxExecution(highestBuyOrder.Key, matchedQuantity));
                    _execution.Add(new AvgPxExecution(lowestSellOrder.Key, matchedQuantity));

                    // Verificar se as ordens são "Partial Fill" ou "Filled"
                    if (buyOrder.OrderQty > 0) {
                        _log.Information($"Order {buyOrder.OrderID} is Partially Filled: {matchedQuantity} units executed, {buyOrder.OrderQty} units remaining.");
                        PartiallyFilled(buyOrder);
                    } else {
                        _log.Information($"Order {buyOrder.OrderID} is Fully Filled: {matchedQuantity} units executed.");
                        RemoveOrder(buyOrder); // Remover ordem do livro
                        FullyFilled(buyOrder);
                    }

                    if (sellOrder.OrderQty > 0) {
                        _log.Information($"Order {sellOrder.OrderID} is Partially Filled: {matchedQuantity} units executed, {sellOrder.OrderQty} units remaining.");
                        PartiallyFilled(sellOrder);
                    } else {
                        _log.Information($"Order {sellOrder.OrderID} is Fully Filled: {matchedQuantity} units executed.");
                        RemoveOrder(sellOrder); // Remover ordem do livro
                        FullyFilled(sellOrder);
                    }
                    SendMarketDataNotification();
                } else {
                    break;
                }
            }
        }

        public void PrintOrderBook(object ob) {
            Console.WriteLine($"Symbol {Symbol}");
            Console.WriteLine("Buy Orders:");
            foreach (var price in buyOrders.Keys) {
                var book = buyOrders[price];
                if (book.Any()) {
                    Console.WriteLine($"Price: {price}, Quantity: {book.Sum(x => x.OrderQty)}");
                }
            }

            Console.WriteLine("Sell Orders:");
            foreach (var price in sellOrders.Keys) {
                var book = sellOrders[price];
                if (book.Any()) {
                    Console.WriteLine($"Price: {price}, Quantity: {book.Sum(x => x.OrderQty)}");
                }
            }
        }

        private void PartiallyFilled(Fix order) {
            var msg = new QuickFix.FIX44.ExecutionReport();

            msg.SetField(new OrderID(order.OrderID)); // ID da Ordem
            msg.SetField(new ExecID(Guid.NewGuid().ToString("N"))); // ID da Execução

            msg.SetField(new ExecType(ExecType.PARTIAL_FILL)); // Tipo de Execução (Partial Fill, Fill, etc.)
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

            msg.SetField(new ExecType(ExecType.FILL)); // Tipo de Execução (Partial Fill, Fill, etc.)
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
            var bestBid = buyOrders.Any() ? buyOrders.First().Key : 0;
            var bestAsk = sellOrders.Any() ? sellOrders.First().Key : 0;

            //OnMarketDataUpdate?.Invoke(bestBid, bestAsk);
        }

        public decimal CalculateAvgPx() {
            decimal totalValue = 0;
            decimal totalQuantity = 0;

            foreach (var execution in _execution) {
                totalValue += execution.Price * execution.Quantity;
                totalQuantity += execution.Quantity;
            }

            if (totalQuantity == 0)
                return 0;

            return totalValue / totalQuantity;
        }
    }
}