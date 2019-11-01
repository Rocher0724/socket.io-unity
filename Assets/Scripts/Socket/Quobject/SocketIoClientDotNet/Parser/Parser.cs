using System;
using System.Collections.Generic;
using System.Text;
using Socket.Newtonsoft.Json.Linq;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.SocketIoClientDotNet.Client;

namespace Socket.Quobject.SocketIoClientDotNet.Parser {
  public class Parser {
    public static List<string> types = new List<string>() {
      nameof(CONNECT),
      nameof(DISCONNECT),
      nameof(EVENT),
      nameof(BINARY_EVENT),
      nameof(ACK),
      nameof(BINARY_ACK),
      nameof(ERROR)
    };

    private static Packet ErrorPacket = new Packet(4, (object) "parser error");
    public const int CONNECT = 0;
    public const int DISCONNECT = 1;
    public const int EVENT = 2;
    public const int ACK = 3;
    public const int ERROR = 4;
    public const int BINARY_EVENT = 5;
    public const int BINARY_ACK = 6;
    public const int protocol = 4;

    private Parser() {
    }

    public class Encoder {
      public void Encode(Packet obj, Quobject.SocketIoClientDotNet.Parser.Parser.Encoder.ICallback callback) {
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format("encoding packet {0}", (object) obj));
        if (5 == obj.Type || 6 == obj.Type) {
          this.EncodeAsBinary(obj, callback);
        } else {
          string str = this.EncodeAsString(obj);
          callback.Call(new object[1] {(object) str});
        }
      }

      private string EncodeAsString(Packet obj) {
        StringBuilder stringBuilder = new StringBuilder();
        bool flag = false;
        stringBuilder.Append(obj.Type);
        if (5 == obj.Type || 6 == obj.Type) {
          stringBuilder.Append(obj.Attachments);
          stringBuilder.Append("-");
        }

        if (!string.IsNullOrEmpty(obj.Nsp) && !"/".Equals(obj.Nsp)) {
          flag = true;
          stringBuilder.Append(obj.Nsp);
        }

        if (obj.Id >= 0) {
          if (flag) {
            stringBuilder.Append(",");
            flag = false;
          }

          stringBuilder.Append(obj.Id);
        }

        if (obj.Data != null) {
          if (flag)
            stringBuilder.Append(",");
          stringBuilder.Append(obj.Data);
        }

        LogManager.GetLogger(Global.CallerName("", 0, ""))
          .Info(string.Format("encoded {0} as {1}", (object) obj, (object) stringBuilder));
        return stringBuilder.ToString();
      }

      private void EncodeAsBinary(Packet obj, Quobject.SocketIoClientDotNet.Parser.Parser.Encoder.ICallback callback) {
        Binary.DeconstructedPacket deconstructedPacket = Binary.DeconstructPacket(obj);
        string str = this.EncodeAsString(deconstructedPacket.Packet);
        List<object> objectList = new List<object>();
        foreach (byte[] buffer in deconstructedPacket.Buffers)
          objectList.Add((object) buffer);
        objectList.Insert(0, (object) str);
        callback.Call(objectList.ToArray());
      }

      public interface ICallback {
        void Call(object[] data);
      }

      public class CallbackImp : Quobject.SocketIoClientDotNet.Parser.Parser.Encoder.ICallback {
        private readonly Action<object[]> Fn;

        public CallbackImp(Action<object[]> fn) {
          this.Fn = fn;
        }

        public void Call(object[] data) {
          this.Fn(data);
        }
      }
    }

    public class Decoder : Emitter {
      public Quobject.SocketIoClientDotNet.Parser.Parser.BinaryReconstructor Reconstructor =
        (Quobject.SocketIoClientDotNet.Parser.Parser.BinaryReconstructor) null;

      public const string EVENT_DECODED = "decoded";

      public void Add(string obj) {
        Packet packet = this.decodeString(obj);
        if (packet.Type == 5 || packet.Type == 6) {
          this.Reconstructor = new Quobject.SocketIoClientDotNet.Parser.Parser.BinaryReconstructor(packet);
          if (this.Reconstructor.reconPack.Attachments != 0)
            return;
          this.Emit("decoded", (object) packet);
        } else
          this.Emit("decoded", (object) packet);
      }

      public void Add(byte[] obj) {
        if (this.Reconstructor == null)
          throw new SocketIOException("got binary data when not reconstructing a packet");
        Packet binaryData = this.Reconstructor.TakeBinaryData(obj);
        if (binaryData == null)
          return;
        this.Reconstructor = (Quobject.SocketIoClientDotNet.Parser.Parser.BinaryReconstructor) null;
        this.Emit("decoded", (object) binaryData);
      }

      private Packet decodeString(string str) {
        Packet packet = new Packet();
        int startIndex1 = 0;
        packet.Type = int.Parse(str.Substring(0, 1));
        if (packet.Type < 0 || packet.Type > Quobject.SocketIoClientDotNet.Parser.Parser.types.Count - 1)
          return Quobject.SocketIoClientDotNet.Parser.Parser.ErrorPacket;
        if (5 == packet.Type || 6 == packet.Type) {
          StringBuilder stringBuilder = new StringBuilder();
          while (str.Substring(++startIndex1, 1) != "-")
            stringBuilder.Append(str.Substring(startIndex1, 1));
          packet.Attachments = int.Parse(stringBuilder.ToString());
        }

        if (str.Length > startIndex1 + 1 && "/" == str.Substring(startIndex1 + 1, 1)) {
          StringBuilder stringBuilder = new StringBuilder();
          do {
            ++startIndex1;
            string str1 = str.Substring(startIndex1, 1);
            if (!("," == str1))
              stringBuilder.Append(str1);
            else
              break;
          } while (startIndex1 + 1 != str.Length);

          packet.Nsp = stringBuilder.ToString();
        } else
          packet.Nsp = "/";

        string s1 = startIndex1 + 1 >= str.Length ? (string) null : str.Substring(startIndex1 + 1, 1);
        int result;
        if (s1 != null && int.TryParse(s1, out result)) {
          StringBuilder stringBuilder = new StringBuilder();
          do {
            ++startIndex1;
            string s2 = str.Substring(startIndex1, 1);
            if (int.TryParse(s2, out result))
              stringBuilder.Append(s2);
            else
              goto label_15;
          } while (startIndex1 + 1 < str.Length);

          goto label_18;
          label_15:
          --startIndex1;
          label_18:
          packet.Id = int.Parse(stringBuilder.ToString());
        }

        int num = startIndex1;
        int startIndex2 = num + 1;
        if (num < str.Length) {
          try {
            string str1 = str.Substring(startIndex2);
            packet.Data = (object) new JValue(str1);
          } catch (ArgumentOutOfRangeException ex) {
          } catch (Exception ex) {
            return Quobject.SocketIoClientDotNet.Parser.Parser.ErrorPacket;
          }
        }

        LogManager.GetLogger(Global.CallerName("", 0, ""))
          .Info(string.Format("decoded {0} as {1}", (object) str, (object) packet));
        return packet;
      }

      public void Destroy() {
        if (this.Reconstructor == null)
          return;
        this.Reconstructor.FinishReconstruction();
      }
    }

    public class BinaryReconstructor {
      public Packet reconPack;
      public List<byte[]> Buffers;

      public BinaryReconstructor(Packet packet) {
        this.reconPack = packet;
        this.Buffers = new List<byte[]>();
      }

      public Packet TakeBinaryData(byte[] binData) {
        this.Buffers.Add(binData);
        if (this.Buffers.Count != this.reconPack.Attachments)
          return (Packet) null;
        Packet packet = Binary.ReconstructPacket(this.reconPack, this.Buffers.ToArray());
        this.FinishReconstruction();
        return packet;
      }

      public void FinishReconstruction() {
        this.reconPack = (Packet) null;
        this.Buffers = new List<byte[]>();
      }
    }
  }
}