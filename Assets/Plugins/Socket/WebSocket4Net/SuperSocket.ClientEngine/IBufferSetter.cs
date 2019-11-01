using System;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public interface IBufferSetter
  {
    void SetBuffer(ArraySegment<byte> bufferSegment);
  }
}
