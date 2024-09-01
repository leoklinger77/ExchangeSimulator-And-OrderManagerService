namespace ExchangeAcceptor {
    using ExchangeAcceptor.Exchange;
    using ExchangeAcceptor.FixEngine;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    internal class Program {
        static void Main(string[] args) {
            Log.Logger = LogConfiguration();
            var builder = Host.CreateApplicationBuilder(args);
            Configuration(builder).Run();
        }

        private static IHost Configuration(HostApplicationBuilder builder) {            
            builder.Services.AddScoped<OrderBookManager>();

            builder.Services.AddHostedService<AcceptorFix>();
            return builder.Build();
        }

        private static ILogger LogConfiguration() {
            var config = new LoggerConfiguration();
            //config.MinimumLevel.Debug();
            config.MinimumLevel.Information();
            config.WriteTo.Console();
            config.WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(),"Log",$"{DateTime.Now:yyyy-MM-dd}.log"));
            return config.CreateLogger();
        }
    }
}