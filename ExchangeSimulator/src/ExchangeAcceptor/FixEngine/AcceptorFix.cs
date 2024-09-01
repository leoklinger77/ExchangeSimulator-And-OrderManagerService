namespace ExchangeAcceptor.FixEngine {
    using ExchangeAcceptor.Exchange;
    using Microsoft.Extensions.Hosting;
    using QuickFix;
    using Serilog;
    using System.Threading;
    using System.Threading.Tasks;

    public class AcceptorFix : BackgroundService {
        private readonly ILogger _log = Serilog.Log.Logger;
        private readonly OrderBookManager _orderBookManager;
        private static ThreadedSocketAcceptor _threadedSocketAcceptor;
        public AcceptorFix(OrderBookManager orderBookManager) {
            _orderBookManager = orderBookManager;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            Initialize();
            return Task.CompletedTask;
        }

        public void Initialize() {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "AcceptorSettings.cfg");

            SessionSettings settings = new SessionSettings(path);
            IApplication myApp = new FixEngine();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);

            _threadedSocketAcceptor = new ThreadedSocketAcceptor(myApp, storeFactory, settings, logFactory);
            _threadedSocketAcceptor.Start();

            _orderBookManager.Initialize((IFixEngine)myApp);
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            _threadedSocketAcceptor?.Stop();
            return Task.CompletedTask;
        }
    }
}