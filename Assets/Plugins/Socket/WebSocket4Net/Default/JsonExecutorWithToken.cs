using System;

namespace Socket.WebSocket4Net.Default {
  internal class JsonExecutorWithToken<T> : JsonExecutorBase<T> {
    private Action<string, T> m_ExecutorAction;

    public JsonExecutorWithToken (Action<string, T> action) {
      this.m_ExecutorAction = action;
    }

    public override void Execute (JsonWebSocket websocket, string token, object param) {
      this.m_ExecutorAction.Method.Invoke (this.m_ExecutorAction.Target, new object[2] {
        (object) token,
        param
      });
    }
  }
}