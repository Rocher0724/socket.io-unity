using System;
using System.Collections.Generic;
using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Protocol {
  internal abstract class ProtocolProcessorBase : IProtocolProcessor {
    private static char[] s_SpaceSpliter = new char[1] {
      ' '
    };

    protected const string HeaderItemFormat = "{0}: {1}";

    public ProtocolProcessorBase (WebSocketVersion version, ICloseStatusCode closeStatusCode) {
      this.CloseStatusCode = closeStatusCode;
      this.Version = version;
      this.VersionTag = ((int) version).ToString ();
    }

    public abstract void SendHandshake (WebSocket websocket);

    public abstract ReaderBase CreateHandshakeReader (WebSocket websocket);

    public abstract bool VerifyHandshake (
      WebSocket websocket,
      WebSocketCommandInfo handshakeInfo,
      out string description);

    public abstract void SendMessage (WebSocket websocket, string message);

    public abstract void SendCloseHandshake (
      WebSocket websocket,
      int statusCode,
      string closeReason);

    public abstract void SendPing (WebSocket websocket, string ping);

    public abstract void SendPong (WebSocket websocket, string pong);

    public abstract void SendData (WebSocket websocket, byte[] data, int offset, int length);

    public abstract void SendData (WebSocket websocket, IList<ArraySegment<byte>> segments);

    public abstract bool SupportBinary { get; }

    public abstract bool SupportPingPong { get; }

    public ICloseStatusCode CloseStatusCode { get; private set; }

    public WebSocketVersion Version { get; private set; }

    protected string VersionTag { get; private set; }

    protected virtual bool ValidateVerbLine (string verbLine) {
      string[] strArray =
        verbLine.Split (ProtocolProcessorBase.s_SpaceSpliter, 3, StringSplitOptions.RemoveEmptyEntries);
      if (strArray.Length < 3 || !strArray[0].StartsWith ("HTTP/"))
        return false;
      int result = 0;
      if (!int.TryParse (strArray[1], out result))
        return false;
      return result == 101;
    }
  }
}