using Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol;
using NotImplementedException = System.NotImplementedException;

namespace Socket.WebSocket4Net.Protocol {
  public class WebSocketDataFrame {
    private long m_ActualPayloadLength = -1;
    private ArraySegmentList m_InnerData;

    public ArraySegmentList InnerData {
      get { return this.m_InnerData; }
    }

    public WebSocketDataFrame (ArraySegmentList data) {
      this.m_InnerData = data;
      this.m_InnerData.ClearSegements ();
    }

    public bool FIN {
      get { return ((int) this.m_InnerData[0] & 128) == 128; }
    }

    public bool RSV1 {
      get { return ((int) this.m_InnerData[0] & 64) == 64; }
    }

    public bool RSV2 {
      get { return ((int) this.m_InnerData[0] & 32) == 32; }
    }

    public bool RSV3 {
      get { return ((int) this.m_InnerData[0] & 16) == 16; }
    }

    public sbyte OpCode {
      get { return (sbyte) ((int) this.m_InnerData[0] & 15); }
    }

    public bool HasMask {
      get { return ((int) this.m_InnerData[1] & 128) == 128; }
    }

    public sbyte PayloadLenght {
      get { return (sbyte) ((int) this.m_InnerData[1] & (int) sbyte.MaxValue); }
    }

    public long ActualPayloadLength {
      get {
        if (this.m_ActualPayloadLength >= 0L)
          return this.m_ActualPayloadLength;
        sbyte payloadLenght = this.PayloadLenght;
        if (payloadLenght < (sbyte) 126)
          this.m_ActualPayloadLength = (long) payloadLenght;
        else if (payloadLenght == (sbyte) 126) {
          this.m_ActualPayloadLength = (long) ((int) this.m_InnerData[2] * 256 + (int) this.m_InnerData[3]);
        } else {
          long num1 = 0;
          int num2 = 1;
          for (int index = 7; index >= 0; --index) {
            num1 += (long) ((int) this.m_InnerData[index + 2] * num2);
            num2 *= 256;
          }

          this.m_ActualPayloadLength = num1;
        }

        return this.m_ActualPayloadLength;
      }
    }

    public byte[] MaskKey { get; set; }

    public byte[] ExtensionData { get; set; }

    public byte[] ApplicationData { get; set; }

    public int Length {
      get { return this.m_InnerData.Count; }
    }

    public void Clear () {
      this.m_InnerData.ClearSegements ();
      this.ExtensionData = new byte[0];
      this.ApplicationData = new byte[0];
      this.m_ActualPayloadLength = -1L;
    }
  }
}