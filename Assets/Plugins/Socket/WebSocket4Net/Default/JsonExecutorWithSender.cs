using System;

namespace Socket.WebSocket4Net.Default {
  internal class JsonExecutorWithSender<T> : JsonExecutorBase<T> {
    private Action<JsonWebSocket, T> m_ExecutorAction;

    public JsonExecutorWithSender (Action<JsonWebSocket, T> action) {
      this.m_ExecutorAction = action;
    }

    public override void Execute (JsonWebSocket websocket, string token, object param) {
      this.m_ExecutorAction.Method.Invoke (this.m_ExecutorAction.Target, new object[2] {
        (object) websocket,
        param
      });
    }
  }
}