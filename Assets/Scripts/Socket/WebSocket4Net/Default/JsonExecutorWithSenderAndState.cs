using System;

namespace Socket.WebSocket4Net.Default {
  internal class JsonExecutorWithSenderAndState<T> : JsonExecutorBase<T>
  {
    private Action<JsonWebSocket, T, object> m_ExecutorAction;
    private object m_State;

    public JsonExecutorWithSenderAndState(Action<JsonWebSocket, T, object> action, object state)
    {
      this.m_ExecutorAction = action;
      this.m_State = state;
    }

    public override void Execute(JsonWebSocket websocket, string token, object param)
    {
      this.m_ExecutorAction.Method.Invoke(this.m_ExecutorAction.Target, new object[3]
      {
        (object) websocket,
        param,
        this.m_State
      });
    }
  }
}
