using System;

namespace Socket.WebSocket4Net.Default {
  internal class JsonExecutorFull<T> : JsonExecutorBase<T> {
    private Action<JsonWebSocket, string, T> m_ExecutorAction;

    public JsonExecutorFull (Action<JsonWebSocket, string, T> action) {
      this.m_ExecutorAction = action;
    }

    public override void Execute (JsonWebSocket websocket, string token, object param) {
      this.m_ExecutorAction.Method.Invoke (this.m_ExecutorAction.Target, new object[3] {
        (object) websocket,
        (object) token,
        param
      });
    }
  }
}