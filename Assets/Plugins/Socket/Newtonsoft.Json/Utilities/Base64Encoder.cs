using System;
using System.IO;

namespace Socket.Newtonsoft.Json.Utilities {
  internal class Base64Encoder {
    private readonly char[] _charsLine = new char[76];
    private const int Base64LineSize = 76;
    private const int LineSizeInBytes = 57;
    private readonly TextWriter _writer;
    private byte[] _leftOverBytes;
    private int _leftOverBytesCount;

    public Base64Encoder(TextWriter writer) {
      ValidationUtils.ArgumentNotNull((object) writer, nameof(writer));
      this._writer = writer;
    }

    private void ValidateEncode(byte[] buffer, int index, int count) {
      if (buffer == null)
        throw new ArgumentNullException(nameof(buffer));
      if (index < 0)
        throw new ArgumentOutOfRangeException(nameof(index));
      if (count < 0)
        throw new ArgumentOutOfRangeException(nameof(count));
      if (count > buffer.Length - index)
        throw new ArgumentOutOfRangeException(nameof(count));
    }

    public void Encode(byte[] buffer, int index, int count) {
      this.ValidateEncode(buffer, index, count);
      if (this._leftOverBytesCount > 0) {
        if (this.FulfillFromLeftover(buffer, index, ref count))
          return;
        this.WriteChars(this._charsLine, 0, Convert.ToBase64CharArray(this._leftOverBytes, 0, 3, this._charsLine, 0));
      }

      this.StoreLeftOverBytes(buffer, index, ref count);
      int num = index + count;
      int length = 57;
      for (; index < num; index += length) {
        if (index + length > num)
          length = num - index;
        this.WriteChars(this._charsLine, 0, Convert.ToBase64CharArray(buffer, index, length, this._charsLine, 0));
      }
    }

    private void StoreLeftOverBytes(byte[] buffer, int index, ref int count) {
      int num = count % 3;
      if (num > 0) {
        count -= num;
        if (this._leftOverBytes == null)
          this._leftOverBytes = new byte[3];
        for (int index1 = 0; index1 < num; ++index1)
          this._leftOverBytes[index1] = buffer[index + count + index1];
      }

      this._leftOverBytesCount = num;
    }

    private bool FulfillFromLeftover(byte[] buffer, int index, ref int count) {
      int leftOverBytesCount = this._leftOverBytesCount;
      while (leftOverBytesCount < 3 && count > 0) {
        this._leftOverBytes[leftOverBytesCount++] = buffer[index++];
        --count;
      }

      if (count != 0 || leftOverBytesCount >= 3)
        return false;
      this._leftOverBytesCount = leftOverBytesCount;
      return true;
    }

    public void Flush() {
      if (this._leftOverBytesCount <= 0)
        return;
      this.WriteChars(this._charsLine, 0,
        Convert.ToBase64CharArray(this._leftOverBytes, 0, this._leftOverBytesCount, this._charsLine, 0));
      this._leftOverBytesCount = 0;
    }

    private void WriteChars(char[] chars, int index, int count) {
      this._writer.Write(chars, index, count);
    }
  }
}