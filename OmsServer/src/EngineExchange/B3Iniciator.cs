namespace OmsServer.EngineExchange {
    using QuickFix;
    using QuickFix.Fields;
    using Serilog;

    public class B3Iniciator : IApplication {
        private readonly ILogger _log = Serilog.Log.Logger;
        public void FromAdmin(Message message, SessionID sessionID) {           
            var msgType = message.Header.GetField(new MsgType());
            switch (msgType.getValue()) {
                case "0":
                    _log.Information($"HEARTBEAT: {message}");
                    break;
                default:
                    break;
            }
        }

        public void FromApp(Message message, SessionID sessionID) {
            _log.Information($"FromApp Message {message} | Session {sessionID}");
        }

        public void OnCreate(SessionID sessionID) {
            _log.Information($"OnCreate Session {sessionID}");
        }

        public void OnLogon(SessionID sessionID) {
            _log.Information($"OnLogon Session {sessionID}");
        }

        public void OnLogout(SessionID sessionID) {
            _log.Information($"OnLogout Session {sessionID}");
        }

        public void ToAdmin(Message message, SessionID sessionID) {
            _log.Information($"ToAdmin Message {message} | Session {sessionID}");
        }

        public void ToApp(Message message, SessionID sessionID) {
            _log.Information($"ToApp Message {message} | Session {sessionID}");
        }
    }
}
