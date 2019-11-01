using System;
using System.Collections.Generic;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public interface IClientSession {
    IProxyConnector Proxy { get; set; }

    int ReceiveBufferSize { get; set; }

    int SendingQueueSize { get; set; }

    bool IsConnected { get; }

    void Connect();

    void Send(ArraySegment<byte> segment);

    void Send(IList<ArraySegment<byte>> segments);

    void Send(byte[] data, int offset, int length);

    bool TrySend(ArraySegment<byte> segment);

    bool TrySend(IList<ArraySegment<byte>> segments);

    void Close();

    event EventHandler Connected;

    event EventHandler Closed;

    event EventHandler<ErrorEventArgs> Error;

    event EventHandler<DataEventArgs> DataReceived;
  }
}