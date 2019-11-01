using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Protocol {
  internal class Rfc6455Processor : DraftHybi10Processor {
    public Rfc6455Processor ()
      : base (WebSocketVersion.Rfc6455, (ICloseStatusCode) new CloseStatusCodeRfc6455 (), "Origin") { }
  }
}