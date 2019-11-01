namespace Socket.Quobject.EngineIoClientDotNet.Parser {
  internal class Buffer {
    private Buffer() {
    }

    public static byte[] Concat(byte[][] list) {
      int length = 0;
      foreach (byte[] numArray in list)
        length += numArray.Length;
      return Buffer.Concat(list, length);
    }

    public static byte[] Concat(byte[][] list, int length) {
      if (list.Length == 0)
        return new byte[0];
      if (list.Length == 1)
        return list[0];
      ByteBuffer byteBuffer = ByteBuffer.Allocate(length);
      foreach (byte[] buf in list)
        byteBuffer.Put(buf);
      return byteBuffer.Array();
    }
  }
}