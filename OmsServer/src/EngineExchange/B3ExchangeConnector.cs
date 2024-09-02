namespace OmsServer.EngineExchange {
    using Bogus;
    using Microsoft.Extensions.Hosting;
    using QuickFix;
    using QuickFix.Fields;
    using QuickFix.FIX44;
    using QuickFix.Transport;
    using Serilog;
    using System.Diagnostics;
    using System.Drawing;

    public class B3ExchangeConnector : BackgroundService {
        private readonly ILogger _log = Serilog.Log.Logger;
        private static SocketInitiator _initiator;
        private static HashSet<SessionID> _session;
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            Initialize();
            return Task.CompletedTask;
        }

        public void Initialize() {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "B3InitiatorSettings.cfg");

            SessionSettings settings = new SessionSettings(path);

            IApplication myApp = new B3Iniciator();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);

            _initiator = new SocketInitiator(myApp, storeFactory, settings, logFactory);

            _initiator.Start();
            _session = _initiator.GetSessionIDs();

            Test();
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            _initiator?.Stop();
            return Task.CompletedTask;
        }

        private void Test() {
            var rand = new Random();
            var symbols = new string[] { "PETR4", "VALE3", "PRIO3", "OIBR3" };
            var qty = new int[] { 100, 500, 1000 };
            do {
                //Console.ReadKey();
                Console.Write("Quantidade a ser enviado: ");
                var read = Console.ReadLine(); ;
                int.TryParse(read, out int totalSend);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < totalSend; i++) {
                    var symbol = symbols[rand.Next(0, symbols.Length)];
                    SendOrder(Side.BUY, symbol, qty[rand.Next(0, qty.Length)], new Faker().Random.Decimal(10.00m, 15.00m));
                    SendOrder(Side.SELL, symbol, qty[rand.Next(0, qty.Length)], new Faker().Random.Decimal(10.00m, 15.00m));
                }
                sw.Stop();
                Console.WriteLine($"TotalMilliseconds {sw.Elapsed.TotalMilliseconds}");
            } while (true);
        }


        public void SendOrder(char side, string symbol, int qty, decimal price) {
            QuickFix.Message message = new NewOrderSingle();
            message.SetField(new ClOrdID(Guid.NewGuid().ToString("N")));

            message.SetField(new Side(side));
            message.SetField(new TransactTime(DateTime.Now));
            message.SetField(new OrdType(OrdType.LIMIT));


            message.SetField(new Symbol(symbol));
            message.SetField(new OrderQty(qty));
            message.SetField(new Price(price));
            message.SetField(new TimeInForce(TimeInForce.DAY));
            try {
                Session.SendToTarget(message, _session.First());                
            } catch (Exception ex) {
                _log.Error("Failed to send order: " + ex.Message);
            }
        }
    }
}
