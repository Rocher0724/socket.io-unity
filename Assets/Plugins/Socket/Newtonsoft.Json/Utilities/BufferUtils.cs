namespace Socket.Newtonsoft.Json.Utilities {
  internal static class BufferUtils {
    public static char[] RentBuffer(IArrayPool<char> bufferPool, int minSize) {
      if (bufferPool == null)
        return new char[minSize];
      return bufferPool.Rent(minSize);
    }

    public static void ReturnBuffer(IArrayPool<char> bufferPool, char[] buffer) {
      bufferPool?.Return(buffer);
    }

    public static char[] EnsureBufferSize(IArrayPool<char> bufferPool, int size, char[] buffer) {
      if (bufferPool == null)
        return new char[size];
      if (buffer != null)
        bufferPool.Return(buffer);
      return bufferPool.Rent(size);
    }
  }
}