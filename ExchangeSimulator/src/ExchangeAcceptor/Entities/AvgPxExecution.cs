namespace ExchangeAcceptor.Entities {
    public class AvgPxExecution {
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }

        public AvgPxExecution(decimal price, decimal quantity) {
            Price = price;
            Quantity = quantity;
        }
    }
}
