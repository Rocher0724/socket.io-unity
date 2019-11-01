using System;
using System.Collections.Generic;
using System.Text;
using Socket.WebSocket4Net.Protocol;
using Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol;
using Socket.WebSocket4Net.System.Linq;

namespace Socket.WebSocket4Net.Default {
  public class WebSocketCommandInfo : ICommandInfo {
    public WebSocketCommandInfo () { }

    public WebSocketCommandInfo (string key) {
      this.Key = key;
    }

    public WebSocketCommandInfo (string key, string text) {
      this.Key = key;
      this.Text = text;
    }

    public WebSocketCommandInfo (IList<WebSocketDataFrame> frames) {
      sbyte opCode = frames[0].OpCode;
      this.Key = opCode.ToString ();
      switch (opCode) {
        case 2:
          byte[] to = new byte[frames.Sum<WebSocketDataFrame> (
            (Func<WebSocketDataFrame, int>) (f => (int) f.ActualPayloadLength))];
          int toIndex = 0;
          for (int index = 0; index < frames.Count; ++index) {
            WebSocketDataFrame frame = frames[index];
            int num = frame.InnerData.Count - (int) frame.ActualPayloadLength;
            int actualPayloadLength = (int) frame.ActualPayloadLength;
            if (frame.HasMask)
              frame.InnerData.DecodeMask (frame.MaskKey, num, actualPayloadLength);
            frame.InnerData.CopyTo (to, num, toIndex, actualPayloadLength);
            toIndex += actualPayloadLength;
          }

          this.Data = to;
          break;
        case 8:
          WebSocketDataFrame frame1 = frames[0];
          int actualPayloadLength1 = (int) frame1.ActualPayloadLength;
          int offset1 = frame1.InnerData.Count - actualPayloadLength1;
          StringBuilder stringBuilder1 = new StringBuilder ();
          if (actualPayloadLength1 >= 2) {
            int startIndex = frame1.InnerData.Count - actualPayloadLength1;
            byte[] arrayData = frame1.InnerData.ToArrayData (startIndex, 2);
            this.CloseStatusCode = (short) ((int) arrayData[0] * 256 + (int) arrayData[1]);
            if (actualPayloadLength1 > 2)
              stringBuilder1.Append (frame1.InnerData.Decode (Encoding.UTF8, startIndex + 2, actualPayloadLength1 - 2));
          } else if (actualPayloadLength1 > 0)
            stringBuilder1.Append (frame1.InnerData.Decode (Encoding.UTF8, offset1, actualPayloadLength1));

          if (frames.Count > 1) {
            for (int index = 1; index < frames.Count; ++index) {
              WebSocketDataFrame frame2 = frames[index];
              int offset2 = frame2.InnerData.Count - (int) frame2.ActualPayloadLength;
              int actualPayloadLength2 = (int) frame2.ActualPayloadLength;
              if (frame2.HasMask)
                frame2.InnerData.DecodeMask (frame2.MaskKey, offset2, actualPayloadLength2);
              stringBuilder1.Append (frame2.InnerData.Decode (Encoding.UTF8, offset2, actualPayloadLength2));
            }
          }

          this.Text = stringBuilder1.ToString ();
          break;
        default:
          StringBuilder stringBuilder2 = new StringBuilder ();
          for (int index = 0; index < frames.Count; ++index) {
            WebSocketDataFrame frame2 = frames[index];
            int offset2 = frame2.InnerData.Count - (int) frame2.ActualPayloadLength;
            int actualPayloadLength2 = (int) frame2.ActualPayloadLength;
            if (frame2.HasMask)
              frame2.InnerData.DecodeMask (frame2.MaskKey, offset2, actualPayloadLength2);
            stringBuilder2.Append (frame2.InnerData.Decode (Encoding.UTF8, offset2, actualPayloadLength2));
          }

          this.Text = stringBuilder2.ToString ();
          break;
      }
    }

    public WebSocketCommandInfo (WebSocketDataFrame frame) {
      this.Key = frame.OpCode.ToString ();
      int actualPayloadLength = (int) frame.ActualPayloadLength;
      int num = frame.InnerData.Count - (int) frame.ActualPayloadLength;
      if (frame.HasMask && actualPayloadLength > 0)
        frame.InnerData.DecodeMask (frame.MaskKey, num, actualPayloadLength);
      if (frame.OpCode == (sbyte) 8 && actualPayloadLength >= 2) {
        byte[] arrayData = frame.InnerData.ToArrayData (num, 2);
        this.CloseStatusCode = (short) ((int) arrayData[0] * 256 + (int) arrayData[1]);
        if (actualPayloadLength > 2)
          this.Text = frame.InnerData.Decode (Encoding.UTF8, num + 2, actualPayloadLength - 2);
        else
          this.Text = string.Empty;
      } else if (frame.OpCode != (sbyte) 2) {
        if (actualPayloadLength > 0)
          this.Text = frame.InnerData.Decode (Encoding.UTF8, num, actualPayloadLength);
        else
          this.Text = string.Empty;
      } else if (actualPayloadLength > 0)
        this.Data = frame.InnerData.ToArrayData (num, actualPayloadLength);
      else
        this.Data = new byte[0];
    }

    public string Key { get; set; }

    public byte[] Data { get; set; }

    public string Text { get; set; }

    public short CloseStatusCode { get; private set; }
  }
}