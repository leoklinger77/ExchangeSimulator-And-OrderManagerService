namespace ExchangeAcceptor.Config {
    public class SymbolsConfig : BaseConfig<SymbolsConfig> {
        public Dictionary<string, Symbols> Symbols { get; set; } = new Dictionary<string, Symbols>();
    }

    public class Symbols {
        public string Symbol { get; set; }
        public decimal LastPx { get; set; }
    }
}
