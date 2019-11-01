using System;
using System.Collections.Generic;
using System.Text;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public class ArraySegmentList : ArraySegmentList<byte> {
    public string Decode(Encoding encoding) {
      return this.Decode(encoding, 0, this.Count);
    }

    public string Decode(Encoding encoding, int offset, int length) {
      if (length == 0)
        return string.Empty;
      IList<ArraySegmentEx<byte>> segments = this.Segments;
      if (segments == null || segments.Count <= 0)
        return string.Empty;
      char[] chars = new char[encoding.GetMaxCharCount(this.Count)];
      int num1 = 0;
      int num2 = 0;
      int num3 = segments.Count - 1;
      bool flush = false;
      Decoder decoder = encoding.GetDecoder();
      int segmentIndex = 0;
      if (offset > 0)
        this.QuickSearchSegment(0, segments.Count - 1, offset, out segmentIndex);
      for (int index = segmentIndex; index < segments.Count; ++index) {
        ArraySegmentEx<byte> arraySegmentEx = segments[index];
        if (index == num3)
          flush = true;
        int byteIndex = arraySegmentEx.Offset;
        int num4 = Math.Min(length - num1, arraySegmentEx.Count);
        if (index == segmentIndex && offset > 0) {
          byteIndex = offset - arraySegmentEx.From + arraySegmentEx.Offset;
          num4 = Math.Min(arraySegmentEx.Count - offset + arraySegmentEx.From, num4);
        }

        int bytesUsed;
        int charsUsed;
        bool completed;
        decoder.Convert(arraySegmentEx.Array, byteIndex, num4, chars, num2, chars.Length - num2, flush, out bytesUsed,
          out charsUsed, out completed);
        num2 += charsUsed;
        num1 += bytesUsed;
        if (num1 >= length)
          break;
      }

      return new string(chars, 0, num2);
    }

    public void DecodeMask(byte[] mask, int offset, int length) {
      int length1 = mask.Length;
      int segmentIndex = 0;
      ArraySegmentEx<byte> arraySegmentEx =
        this.QuickSearchSegment(0, this.Segments.Count - 1, offset, out segmentIndex);
      int num1 = Math.Min(length, arraySegmentEx.Count - offset + arraySegmentEx.From);
      int num2 = offset - arraySegmentEx.From + arraySegmentEx.Offset;
      int num3 = 0;
      for (int index = num2; index < num2 + num1; ++index)
        arraySegmentEx.Array[index] = (byte) ((int) arraySegmentEx.Array[index] ^ (int) mask[num3++ % length1]);
      if (num3 >= length)
        return;
      for (int index = segmentIndex + 1; index < this.SegmentCount; ++index) {
        ArraySegmentEx<byte> segment = this.Segments[index];
        int num4 = Math.Min(length - num3, segment.Count);
        for (int offset1 = segment.Offset; offset1 < segment.Offset + num4; ++offset1)
          segment.Array[offset1] = (byte) ((int) segment.Array[offset1] ^ (int) mask[num3++ % length1]);
        if (num3 >= length)
          break;
      }
    }
  }
}