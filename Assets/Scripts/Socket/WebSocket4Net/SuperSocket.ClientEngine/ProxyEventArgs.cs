using System;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public class ProxyEventArgs : EventArgs {
    public ProxyEventArgs(global::System.Net.Sockets.Socket socket)
      : this(true, socket, (Exception) null) {
    }

    public ProxyEventArgs(Exception exception)
      : this(false, (global::System.Net.Sockets.Socket) null, exception) {
    }

    public ProxyEventArgs(bool connected, global::System.Net.Sockets.Socket socket, Exception exception) {
      this.Connected = connected;
      this.Socket = socket;
      this.Exception = exception;
    }

    public bool Connected { get; private set; }

    public global::System.Net.Sockets.Socket Socket { get; private set; }

    public Exception Exception { get; private set; }
  }
}