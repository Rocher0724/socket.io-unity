namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public class BinaryCommandInfo : CommandInfo<byte[]> {
    public BinaryCommandInfo(string key, byte[] data)
      : base(key, data) {
    }
  }
}