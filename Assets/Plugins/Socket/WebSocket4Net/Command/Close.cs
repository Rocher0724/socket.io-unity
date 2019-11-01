using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Command {
  public class Close : WebSocketCommandBase {
    public override void ExecuteCommand (WebSocket session, WebSocketCommandInfo commandInfo) {
      if (session.StateCode == 2) {
        session.CloseWithoutHandshake ();
      } else {
        short num = commandInfo.CloseStatusCode;
        if (num <= (short) 0)
          num = session.ProtocolProcessor.CloseStatusCode.NoStatusCode;
        session.Close ((int) num, commandInfo.Text);
      }
    }

    public override string Name {
      get { return 8.ToString (); }
    }
  }
}