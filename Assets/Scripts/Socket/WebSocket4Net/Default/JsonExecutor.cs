using System;

namespace Socket.WebSocket4Net.Default {
  internal class JsonExecutor<T> : JsonExecutorBase<T>
  {
    private Action<T> m_ExecutorAction;

    public JsonExecutor(Action<T> action)
    {
      this.m_ExecutorAction = action;
    }

    public override void Execute(JsonWebSocket websocket, string token, object param)
    {
      this.m_ExecutorAction.Method.Invoke(this.m_ExecutorAction.Target, new object[1]
      {
        param
      });
    }
  }
}
