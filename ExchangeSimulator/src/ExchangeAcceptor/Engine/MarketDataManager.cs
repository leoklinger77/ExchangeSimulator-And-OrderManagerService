using ExchangeAcceptor.Entities;
using System.Collections.Concurrent;

namespace ExchangeAcceptor.Engine {
    public class MarketDataManager {
        private ConcurrentDictionary<string, Instrument> _instrument = new ConcurrentDictionary<string, Instrument>();
        public MarketDataManager()
        {
        }

        public void Initialize() {            
            foreach (var instrument in Instrument.Get("Instrument")) {
                if (!_instrument.ContainsKey(instrument.Symbol)) {
                    _instrument.TryAdd(instrument.Symbol, instrument);
                }
            }
        }
        
    }
}
