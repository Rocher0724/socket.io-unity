using System;

namespace Socket.WebSocket4Net.Default {
  internal abstract class JsonExecutorBase<T> : IJsonExecutor {
    public Type Type {
      get { return typeof (T); }
    }

    public abstract void Execute (JsonWebSocket websocket, string token, object param);
  }
}