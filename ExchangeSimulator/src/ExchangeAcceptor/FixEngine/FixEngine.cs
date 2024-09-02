namespace ExchangeAcceptor.FixEngine {
    using ExchangeAcceptor.Entities;
    using QuickFix;
    using QuickFix.Fields;
    using Serilog;
    using Serilog.Events;
    using System.Reactive.Subjects;

    public class FixEngine : IFixEngine {
        private readonly ILogger _log = Serilog.Log.Logger;
        private ISubject<BooksDto> _subjectNewOrderSingle = new Subject<BooksDto>();
        private ISubject<Message> _subjectReplaceRequest = new Subject<Message>();
        private ISubject<Message> _subjectCancelRequest = new Subject<Message>();
        private ISubject<Message> _subjectExecutionReport = new Subject<Message>();
        private ISubject<Message> _subjectBusinessMessageReject = new Subject<Message>();

        private IObservable<BooksDto> _obsNewOrderSingle;
        private IObservable<Message> _obsReplaceRequest;
        private IObservable<Message> _obsCancelRequest;
        private IObservable<Message> _obsExecutionReport;
        private IObservable<Message> _obsBusinessMessageReject;

        public FixEngine() {
            _obsNewOrderSingle = _subjectNewOrderSingle;
            _obsReplaceRequest = _subjectReplaceRequest;
            _obsCancelRequest = _subjectCancelRequest;
            _obsExecutionReport = _subjectExecutionReport;
            _obsBusinessMessageReject = _subjectBusinessMessageReject;
        }

        public IObservable<BooksDto> NewOrderSingle { get { return _obsNewOrderSingle; } }
        public IObservable<Message> OrderCancelReplaceRequest { get { return _obsReplaceRequest; } }
        public IObservable<Message> OrderCancelRequest { get { return _obsCancelRequest; } }
        public IObservable<Message> ExecutionReport { get { return _subjectExecutionReport; } }
        public IObservable<Message> BusinessMessageReject { get { return _subjectBusinessMessageReject; } }

        public void FromAdmin(Message message, SessionID sessionID) {
            var msgType = message.Header.GetField(new MsgType());
            switch (msgType.getValue()) {
                case MsgType.HEARTBEAT:
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"HEARTBEAT: {message}");
                    }
                    break;
                default:
                    //_log.Information($"DEFAULT: {message}");
                    break;
            }
        }

        public void FromApp(Message message, SessionID sessionID) {
            var msgType = message.Header.GetField(new MsgType());
            switch (msgType.getValue()) {
                case MsgType.NEW_ORDER_D:
                    var book = new BooksDto(message, sessionID);
                    _subjectNewOrderSingle.OnNext(book);
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"NEW_ORDER_D {message}");
                    }
                    break;
                case MsgType.ORDER_CANCEL_REPLACE_REQUEST:
                    _subjectReplaceRequest.OnNext(message);
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"ORDER_CANCEL_REPLACE_REQUEST {message}");
                    }
                    break;
                case MsgType.ORDER_CANCEL_REJECT:
                    _subjectCancelRequest.OnNext(message);
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"ORDER_CANCEL_REJECT {message}");
                    }
                    break;
                case MsgType.EXECUTION_REPORT:
                    _subjectExecutionReport.OnNext(message);
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"EXECUTION_REPORT {message}");
                    }
                    break;
                case MsgType.BUSINESS_MESSAGE_REJECT:
                    _subjectBusinessMessageReject.OnNext(message);
                    if (_log.IsEnabled(LogEventLevel.Debug)) {
                        _log.Debug($"BUSINESS_MESSAGE_REJECT {message}");
                    }
                    break;
                default:
                    //_log.Information($"DEFAULT: {message}");
                    break;
            }
        }

        public void OnCreate(SessionID sessionID) {
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.Debug($"OnCreate Session {sessionID}");
            }
        }

        public void OnLogon(SessionID sessionID) {
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.Debug($"OnLogon Session {sessionID}");
            }
        }

        public void OnLogout(SessionID sessionID) {
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.Debug($"OnLogout Session {sessionID}");
            }
        }

        public void ToAdmin(Message message, SessionID sessionID) {
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.Debug($"ToAdmin Message {message} | Session {sessionID}");
            }
        }

        public void ToApp(Message message, SessionID sessionID) {
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.Debug($"ToApp Message {message} | Session {sessionID}");
            }
        }
    }
}