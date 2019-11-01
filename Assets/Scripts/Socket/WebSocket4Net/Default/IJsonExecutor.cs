using System;

namespace Socket.WebSocket4Net.Default {
  internal interface IJsonExecutor {
    Type Type { get; }

    void Execute (JsonWebSocket websocket, string token, object param);
  }
}