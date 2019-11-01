namespace Socket.Quobject.EngineIoClientDotNet.Parser {
  public interface IDecodePayloadCallback {
    bool Call(Packet packet, int index, int total);
  }
}