using System;

namespace Socket.WebSocket4Net.Default {
  public class MessageReceivedEventArgs : EventArgs {
    public MessageReceivedEventArgs (string message) {
      this.Message = message;
    }

    public string Message { get; private set; }
  }
}