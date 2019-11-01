using System.Text;
using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Protocol {
  internal class DraftHybi00DataReader : ReaderBase {
    private const byte m_ClosingHandshakeType = 255;
    private byte? m_Type;
    private int m_TempLength;
    private int? m_Length;

    public DraftHybi00DataReader (ReaderBase previousCommandReader)
      : base (previousCommandReader) { }

    public override WebSocketCommandInfo GetCommandInfo (
      byte[] readBuffer,
      int offset,
      int length,
      out int left) {
      left = 0;
      int offset1 = 0;
      if (!this.m_Type.HasValue) {
        byte num = readBuffer[offset];
        offset1 = 1;
        this.m_Type = new byte? (num);
      }

      if (((int) this.m_Type.Value & 128) == 0) {
        byte maxValue = byte.MaxValue;
        for (int index = offset + offset1; index < offset + length; ++index) {
          if ((int) readBuffer[index] == (int) maxValue) {
            left = length - (index - offset + 1);
            if (this.BufferSegments.Count <= 0) {
              WebSocketCommandInfo socketCommandInfo = new WebSocketCommandInfo (1.ToString (),
                Encoding.UTF8.GetString (readBuffer, offset + offset1, index - offset - offset1));
              this.Reset (false);
              return socketCommandInfo;
            }

            this.BufferSegments.AddSegment (readBuffer, offset + offset1, index - offset - offset1, false);
            WebSocketCommandInfo socketCommandInfo1 =
              new WebSocketCommandInfo (1.ToString (), this.BufferSegments.Decode (Encoding.UTF8));
            this.Reset (true);
            return socketCommandInfo1;
          }
        }

        this.AddArraySegment (readBuffer, offset + offset1, length - offset1);
        return (WebSocketCommandInfo) null;
      }

      while (!this.m_Length.HasValue) {
        if (length <= offset1)
          return (WebSocketCommandInfo) null;
        byte num = readBuffer[offset1];
        if (num == (byte) 0 && this.m_Type.Value == byte.MaxValue) {
          WebSocketCommandInfo socketCommandInfo = new WebSocketCommandInfo (8.ToString ());
          this.Reset (true);
          return socketCommandInfo;
        }

        this.m_TempLength = this.m_TempLength * 128 + ((int) num & (int) sbyte.MaxValue);
        ++offset1;
        if (((int) num & 128) != 128) {
          this.m_Length = new int? (this.m_TempLength);
          break;
        }
      }

      int num1 = this.m_Length.Value - this.BufferSegments.Count;
      int num2 = length - offset1;
      if (num2 < num1) {
        this.AddArraySegment (readBuffer, offset1, length - offset1);
        return (WebSocketCommandInfo) null;
      }

      left = num2 - num1;
      if (this.BufferSegments.Count <= 0) {
        WebSocketCommandInfo socketCommandInfo = new WebSocketCommandInfo (1.ToString (),
          Encoding.UTF8.GetString (readBuffer, offset + offset1, num1));
        this.Reset (false);
        return socketCommandInfo;
      }

      this.BufferSegments.AddSegment (readBuffer, offset + offset1, num1, false);
      WebSocketCommandInfo socketCommandInfo2 = new WebSocketCommandInfo (this.BufferSegments.Decode (Encoding.UTF8));
      this.Reset (true);
      return socketCommandInfo2;
    }

    private void Reset (bool clearBuffer) {
      this.m_Type = new byte? ();
      this.m_Length = new int? ();
      this.m_TempLength = 0;
      if (!clearBuffer)
        return;
      this.BufferSegments.ClearSegements ();
    }
  }
}