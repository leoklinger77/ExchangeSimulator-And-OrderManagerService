namespace ExchangeAcceptor.Exchange {
    using ExchangeAcceptor.Entities;
    using ExchangeAcceptor.FixEngine;
    using QuickFix;
    using QuickFix.Fields;

    public class OrderBookManager {
        private IDictionary<string, OrderBook> _manager = new Dictionary<string, OrderBook>();
        private IFixEngine _fixEngine;

        public void Initialize(IFixEngine fixEngine) {
            _fixEngine = fixEngine;

            _fixEngine.NewOrderSingle.Subscribe(fix => {
                var order = new Fix(fix.Message, fix.Session);
                if (!order.IsValid()) {
                    SendRejectOrder(fix.Message, fix.Session);
                    return;
                }
                SendAcceptOrder(order, fix.Session);

                if (_manager.TryGetValue(order.Symbol, out var manager)) {
                    manager.AddOrder(order);
                } else {
                    _manager.Add(order.Symbol, new OrderBook(order.Symbol));
                }
            });
        }

        private void SendAcceptOrder(Fix fix, SessionID sessionID) {
            var msg = new QuickFix.FIX44.ExecutionReport();
            var ordeStatus = OrdStatus.NEW;

            msg.SetField(new OrdStatus(ordeStatus));
            msg.SetField(new OrderID(fix.OrderID));

            msg.SetField(new ClOrdID(fix.ClOrdID));

            msg.SetField(new Side(fix.Side));
            msg.SetField(new TransactTime(fix.TransactTime));
            msg.SetField(new OrdType(fix.OrdType));


            msg.SetField(new Symbol(fix.Symbol));
            msg.SetField(new OrderQty(fix.OrderQty));
            msg.SetField(new Price(fix.Price));
            msg.SetField(new TimeInForce(fix.TimeInForce));


            msg.SetField(new ExecID(fix.OrderID));
            msg.SetField(new ExecType(ordeStatus));

            msg.SetField(new LeavesQty(10));
            msg.SetField(new CumQty(10));
            msg.SetField(new AvgPx(10));

            Session.SendToTarget(msg, sessionID);
        }

        private void SendRejectOrder(Message msg, SessionID sessionID) {
            var message = new QuickFix.FIX44.Reject();

            msg.SetField(new MsgType(MsgType.REJECT));
            msg.SetField(new Text("Malformed order"));
            var msgSeqNum = msg.GetField(new MsgSeqNum());
            msg.SetField(new RefSeqNum(msgSeqNum.getValue()));

            Session.SendToTarget(msg, sessionID);
        }
    }
}
