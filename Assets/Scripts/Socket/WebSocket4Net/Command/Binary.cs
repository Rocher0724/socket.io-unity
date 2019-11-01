using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Command {
  public class Binary : WebSocketCommandBase {
    public override void ExecuteCommand (WebSocket session, WebSocketCommandInfo commandInfo) {
      session.FireDataReceived (commandInfo.Data);
    }

    public override string Name {
      get { return 2.ToString (); }
    }
  }
}