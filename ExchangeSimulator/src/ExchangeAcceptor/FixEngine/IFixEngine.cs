namespace ExchangeAcceptor.FixEngine {
    using ExchangeAcceptor.Entities;
    using QuickFix;
    public interface IFixEngine : IApplication {
        IObservable<BooksDto> NewOrderSingle { get; }
        IObservable<Message> OrderCancelReplaceRequest { get; }
        IObservable<Message> OrderCancelRequest { get; }
        IObservable<Message> ExecutionReport { get; }
        IObservable<Message> BusinessMessageReject { get; }
    }
}
