namespace Socket.Quobject.EngineIoClientDotNet.Parser {
  public class Parser2 {
    public static readonly int Protocol = 3;

    private Parser2() {
    }

    public static void EncodePacket(Packet packet, IEncodeCallback callback) {
      packet.Encode(callback, false);
    }

    public static Packet DecodePacket(string data, bool utf8decode = false) {
      return Packet.DecodePacket(data, utf8decode);
    }

    public static Packet DecodePacket(byte[] data) {
      return Packet.DecodePacket(data);
    }

    public static void EncodePayload(Packet[] packets, IEncodeCallback callback) {
      Packet.EncodePayload(packets, callback);
    }

    public static void DecodePayload(string data, IDecodePayloadCallback callback) {
      Packet.DecodePayload(data, callback);
    }

    public static void DecodePayload(byte[] data, IDecodePayloadCallback callback) {
      Packet.DecodePayload(data, callback);
    }
  }
}