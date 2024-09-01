namespace ExchangeAcceptor.Entities {
    using QuickFix;

    public class BooksDto {
        public Message Message { get; private set; }
        public SessionID Session { get; private set; }

        public BooksDto(Message message, SessionID session) {
            Message = message;
            Session = session;
        }
    }
}
