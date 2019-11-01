using Socket.WebSocket4Net.Default;
namespace Socket.WebSocket4Net.Command {
  public class Text : WebSocketCommandBase {
    public override void ExecuteCommand (WebSocket session, WebSocketCommandInfo commandInfo) {
      session.FireMessageReceived (commandInfo.Text);
    }

    public override string Name {
      get { return 1.ToString (); }
    }
  }
}