using System;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public class ErrorEventArgs : EventArgs {
    public Exception Exception { get; private set; }

    public ErrorEventArgs(Exception exception) {
      this.Exception = exception;
    }
  }
}