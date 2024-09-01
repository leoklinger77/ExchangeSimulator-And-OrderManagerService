namespace ExchangeAcceptor.Entities {
    using QuickFix;
    using QuickFix.Fields;

    public class Fix {
        private bool isValid { get; }

        public SessionID Session { get; internal set; }
        public string MsgType { get; internal set; }
        public int MsgSeqNum { get; internal set; }
        public string ClOrdID { get; internal set; }
        public string OrderID { get; internal set; }
        public char Side { get; internal set; }
        public DateTime TransactTime { get; internal set; }
        public char OrdType { get; internal set; }
        public string Symbol { get; internal set; }
        public decimal OrderQty { get; internal set; }
        public decimal Price { get; internal set; }
        public char TimeInForce { get; internal set; }
        public string ExecID { get; internal set; }
        public char ExecType { get; internal set; }
        public char OrdStatus { get; internal set; }
        public decimal LeavesQty { get; internal set; }
        public decimal CumQty { get; internal set; }
        public decimal AvgPx { get; internal set; }
        public decimal LastQty { get; internal set; }
        public decimal LastPx { get; internal set; }

        public Fix(Message msg, SessionID session) {
            Session = session;
            isValid = true;
            var msgType = msg.Header.GetField(new MsgType());
            if (msgType != null && !string.IsNullOrEmpty(msgType.getValue())) {
                MsgType = msgType.getValue();
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(11)) {
                ClOrdID = msg.GetField(11);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(54)) {
                Side = msg.GetChar(54);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(60)) {
                TransactTime = msg.GetDateTime(60);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(40)) {
                OrdType = msg.GetChar(40);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(55)) {
                Symbol = msg.GetField(55);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(38)) {
                OrderQty = msg.GetDecimal(38);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(44)) {
                Price = msg.GetDecimal(44);
                isValid &= true;
            } else isValid &= false;
            if (msg.IsSetField(59)) {
                TimeInForce = msg.GetChar(59);
                isValid &= true;
            } else isValid &= false;

            var msgSeqNum = msg.Header.GetField(new MsgSeqNum());
            if (msgSeqNum != null && int.IsPositive(msgSeqNum.getValue())) {
                MsgSeqNum = msgSeqNum.getValue();
                isValid &= true;
            } else isValid &= false;

            if (msg.IsSetField(37)) {
                OrderID = msg.GetString(37);
            } else OrderID = Guid.NewGuid().ToString("N");
        }

        internal void SetLastQty(decimal lastQty) {
            LastQty = lastQty;
            SetLastPx();
        }

        internal void SetLastPx() {
            if (OrderQty == 0) {
                OrderQty = 0;
            } else {
                LastPx = OrderQty - LastQty;
            }
        }

        internal bool IsValid() {
            return isValid;
        }
    }
}
