using System;
using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Command {
  public class Handshake : WebSocketCommandBase {
    public override void ExecuteCommand (WebSocket session, WebSocketCommandInfo commandInfo) {
      string description;
      if (!session.ProtocolProcessor.VerifyHandshake (session, commandInfo, out description)) {
        session.FireError (new Exception (description));
        session.Close ((int) session.ProtocolProcessor.CloseStatusCode.ProtocolError, description);
      } else
        session.OnHandshaked ();
    }

    public override string Name {
      get { return (-1).ToString (); }
    }
  }
}