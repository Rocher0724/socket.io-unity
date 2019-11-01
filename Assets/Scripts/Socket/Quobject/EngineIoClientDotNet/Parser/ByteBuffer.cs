using System;
using System.IO;

namespace Socket.Quobject.EngineIoClientDotNet.Parser {
  public class ByteBuffer
  {
    private long _limit = 0;
    private readonly MemoryStream _memoryStream;

    public ByteBuffer(int length)
    {
      this._memoryStream = new MemoryStream();
      this._memoryStream.SetLength((long) length);
      this._memoryStream.Capacity = length;
      this._limit = (long) length;
    }

    public static ByteBuffer Allocate(int length)
    {
      return new ByteBuffer(length);
    }

    internal void Put(byte[] buf)
    {
      this._memoryStream.Write(buf, 0, buf.Length);
    }

    internal byte[] Array()
    {
      return this._memoryStream.ToArray();
    }

    internal static ByteBuffer Wrap(byte[] data)
    {
      ByteBuffer byteBuffer = new ByteBuffer(data.Length);
      byteBuffer.Put(data);
      return byteBuffer;
    }

    public int Capacity
    {
      get
      {
        return this._memoryStream.Capacity;
      }
    }

    internal byte Get(long index)
    {
      if (index > (long) this.Capacity)
        throw new IndexOutOfRangeException();
      this._memoryStream.Position = index;
      return (byte) this._memoryStream.ReadByte();
    }

    internal ByteBuffer Get(byte[] dst, int offset, int length)
    {
      this._memoryStream.Read(dst, offset, length);
      return this;
    }

    internal ByteBuffer Get(byte[] dst)
    {
      return this.Get(dst, 0, dst.Length);
    }

    internal void Position(long newPosition)
    {
      this._memoryStream.Position = newPosition;
    }

    internal void Limit(long newLimit)
    {
      this._limit = newLimit;
      if (this._memoryStream.Position <= newLimit)
        return;
      this._memoryStream.Position = newLimit;
    }

    internal long Remaining()
    {
      return this._limit - this._memoryStream.Position;
    }

    internal void Clear()
    {
      this.Position(0L);
      this.Limit((long) this.Capacity);
    }
  }
}
