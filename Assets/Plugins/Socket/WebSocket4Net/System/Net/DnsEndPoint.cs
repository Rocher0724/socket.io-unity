using System.Net;

namespace Socket.WebSocket4Net.System.Net {
  public class DnsEndPoint : EndPoint {
    public string Host { get; private set; }

    public int Port { get; private set; }

    public DnsEndPoint (string host, int port) {
      this.Host = host;
      this.Port = port;
    }
  }
}