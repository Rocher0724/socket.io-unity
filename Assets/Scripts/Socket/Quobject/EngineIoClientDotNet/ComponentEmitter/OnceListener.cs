using System;

namespace Socket.Quobject.EngineIoClientDotNet.ComponentEmitter {
  public class OnceListener : IListener, IComparable<IListener> {
    private static int id_counter = 0;
    private int Id;
    private readonly string _eventString;
    private readonly IListener _fn;
    private readonly Emitter _emitter;

    public OnceListener(string eventString, IListener fn, Emitter emitter) {
      this._eventString = eventString;
      this._fn = fn;
      this._emitter = emitter;
      this.Id = OnceListener.id_counter++;
    }

    void IListener.Call(params object[] args) {
      this._emitter.Off(this._eventString, (IListener) this);
      this._fn.Call(args);
    }

    public int CompareTo(IListener other) {
      return this.GetId().CompareTo(other.GetId());
    }

    public int GetId() {
      return this.Id;
    }
  }
}