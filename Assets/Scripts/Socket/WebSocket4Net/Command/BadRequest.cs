using System;
using System.Collections.Generic;
using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Command {
  public class BadRequest : WebSocketCommandBase {
    private static readonly string[] m_ValueSeparator = new string[1] {
      ", "
    };

    private const string m_WebSocketVersion = "Sec-WebSocket-Version";

    public override void ExecuteCommand (WebSocket session, WebSocketCommandInfo commandInfo) {
      Dictionary<string, object> valueContainer =
        new Dictionary<string, object> ((IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);
      string verbLine = string.Empty;
      commandInfo.Text.ParseMimeHeader ((IDictionary<string, object>) valueContainer, out verbLine);
      string str = valueContainer.GetValue<string> ("Sec-WebSocket-Version", string.Empty);
      if (!session.NotSpecifiedVersion) {
        if (string.IsNullOrEmpty (str))
          session.FireError (
            new Exception ("the server doesn't support the websocket protocol version your client was using"));
        else
          session.FireError (new Exception (string.Format (
            "the server(version: {0}) doesn't support the websocket protocol version your client was using",
            (object) str)));
        session.CloseWithoutHandshake ();
      } else if (string.IsNullOrEmpty (str)) {
        session.FireError (new Exception ("unknown server protocol version"));
        session.CloseWithoutHandshake ();
      } else {
        string[] strArray = str.Split (BadRequest.m_ValueSeparator, StringSplitOptions.RemoveEmptyEntries);
        int[] availableVersions = new int[strArray.Length];
        for (int index = 0; index < strArray.Length; ++index) {
          int result;
          if (!int.TryParse (strArray[index], out result)) {
            session.FireError (new Exception ("invalid websocket version"));
            session.CloseWithoutHandshake ();
            return;
          }

          availableVersions[index] = result;
        }

        if (!session.GetAvailableProcessor (availableVersions)) {
          session.FireError (new Exception ("unknown server protocol version"));
          session.CloseWithoutHandshake ();
        } else
          session.ProtocolProcessor.SendHandshake (session);
      }
    }

    public override string Name {
      get { return 400.ToString (); }
    }
  }
}