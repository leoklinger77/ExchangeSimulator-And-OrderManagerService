using ExchangeAcceptor.Config;

namespace ExchangeAcceptor.Entities {
    public class Instrument : BaseConfig<IEnumerable<Instrument>> {
        // Identificação do Instrumento
        public string Symbol { get; set; } // Código do ativo (ex: PETR4)
        public string ISIN { get; set; } // International Securities Identification Number
        public string Name { get; set; } // Nome completo do ativo
        public string Exchange { get; set; } // Bolsa de valores ou mercado onde é negociado (ex: B3)

        // Preços e Cotações
        public decimal LastPrice { get; set; } // Último preço negociado
        public decimal OpenPrice { get; set; } // Preço de abertura
        public decimal HighPrice { get; set; } // Maior preço do dia
        public decimal LowPrice { get; set; } // Menor preço do dia
        public decimal ClosePrice { get; set; } // Preço de fechamento do dia anterior

        // Volume e Liquidez
        public long Volume { get; set; } // Volume de negociações (número de ações negociadas)
        public long TradeCount { get; set; } // Número de negócios realizados

        // Status do Ativo
        public string Status { get; set; } // Status atual (ex: Open, Close, Forbidden)
        public DateTime TradingDate { get; set; } // Data da negociação atual

        // Informações sobre o Livro de Ofertas
        public decimal BestBidPrice { get; set; } // Melhor oferta de compra
        public long BestBidQuantity { get; set; } // Quantidade na melhor oferta de compra
        public decimal BestAskPrice { get; set; } // Melhor oferta de venda
        public long BestAskQuantity { get; set; } // Quantidade na melhor oferta de venda

        // Informações Adicionais
        public decimal PreviousClosePrice { get; set; } // Preço de fechamento do dia anterior
        public decimal PriceChange { get; set; } // Variação de preço desde o fechamento anterior
        public decimal PercentageChange { get; set; } // Percentual de variação de preço
        public DateTime LastUpdate { get; set; } // Data e hora da última atualização de dados

        public void UpdateFromTrade(decimal lastPrice, long quantity, DateTime tradeTime, 
                                    decimal bestBidPrice, long bestBidQuantity, decimal bestAskPrice, long bestAskQuantity) {
            // Atualiza o último preço
            this.LastPrice = lastPrice;

            // Atualiza o volume negociado (assumindo que quantity é a quantidade negociada na última transação)
            this.Volume += quantity;

            // Incrementa o número de transações
            this.TradeCount += 1;

            // Atualiza o maior e menor preço do dia
            if (this.HighPrice < lastPrice || this.HighPrice == 0) {
                this.HighPrice = lastPrice;
            }
            if (this.LowPrice > lastPrice || this.LowPrice == 0) {
                this.LowPrice = lastPrice;
            }

            // Atualiza a variação de preço e a variação percentual em relação ao preço de fechamento anterior
            this.PriceChange = lastPrice - this.PreviousClosePrice;
            if (this.PreviousClosePrice != 0) {
                this.PercentageChange = (this.PriceChange / this.PreviousClosePrice) * 100;
            }

            // Atualiza a data e hora da última atualização
            this.LastUpdate = tradeTime;

            BestBidPrice = bestBidPrice;
            BestBidQuantity = bestBidQuantity;
            BestAskPrice = bestAskPrice;
            BestAskQuantity = bestAskQuantity;
        }
    }
}
