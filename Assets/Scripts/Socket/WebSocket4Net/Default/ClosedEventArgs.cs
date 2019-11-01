using System;

namespace Socket.WebSocket4Net.Default {
  public class ClosedEventArgs : EventArgs {
    public short Code { get; private set; }

    public string Reason { get; private set; }

    public ClosedEventArgs (short code, string reason) {
      this.Code = code;
      this.Reason = reason;
    }
  }
}