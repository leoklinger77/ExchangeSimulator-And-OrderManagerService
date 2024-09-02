namespace OmsServer.EngineAcceptor {
    using QuickFix;

    public class OmsAcceptor : IApplication {
        private readonly ILogger _log = Serilog.Log.Logger;
        public void FromAdmin(Message message, SessionID sessionID) {
            throw new NotImplementedException();
        }

        public void FromApp(Message message, SessionID sessionID) {
            throw new NotImplementedException();
        }

        public void OnCreate(SessionID sessionID) {
            throw new NotImplementedException();
        }

        public void OnLogon(SessionID sessionID) {
            throw new NotImplementedException();
        }

        public void OnLogout(SessionID sessionID) {
            throw new NotImplementedException();
        }

        public void ToAdmin(Message message, SessionID sessionID) {
            throw new NotImplementedException();
        }

        public void ToApp(Message message, SessionID sessionId) {
            throw new NotImplementedException();
        }
    }
}
