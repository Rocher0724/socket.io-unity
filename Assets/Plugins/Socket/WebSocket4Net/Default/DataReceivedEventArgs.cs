using System;

namespace Socket.WebSocket4Net.Default {
  public class DataReceivedEventArgs : EventArgs {
    public DataReceivedEventArgs (byte[] data) {
      this.Data = data;
    }

    public byte[] Data { get; private set; }
  }
}