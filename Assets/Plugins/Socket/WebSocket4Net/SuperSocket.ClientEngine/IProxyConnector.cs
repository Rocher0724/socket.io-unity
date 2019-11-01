using System;
using System.Net;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public interface IProxyConnector
  {
    void Connect(EndPoint remoteEndPoint);

    event EventHandler<ProxyEventArgs> Completed;
  }
}