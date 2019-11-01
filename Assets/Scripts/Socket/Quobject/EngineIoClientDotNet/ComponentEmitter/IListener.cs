using System;

namespace Socket.Quobject.EngineIoClientDotNet.ComponentEmitter {
  public interface IListener : IComparable<IListener>
  {
    int GetId();

    void Call(params object[] args);
  }
}