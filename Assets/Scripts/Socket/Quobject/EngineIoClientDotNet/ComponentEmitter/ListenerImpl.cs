using System;

namespace Socket.Quobject.EngineIoClientDotNet.ComponentEmitter {
  public class ListenerImpl : IListener, IComparable<IListener>
  {
    private static int id_counter = 0;
    private int Id;
    private readonly ActionTrigger fn1;
    private readonly Action<object> fn;

    public ListenerImpl(Action<object> fn)
    {
      this.fn = fn;
      this.Id = ListenerImpl.id_counter++;
    }

    public ListenerImpl(ActionTrigger fn)
    {
      this.fn1 = fn;
      this.Id = ListenerImpl.id_counter++;
    }

    public void Call(params object[] args)
    {
      if (this.fn != null)
        this.fn(args.Length != 0 ? args[0] : (object) null);
      else
        this.fn1();
    }

    public int CompareTo(IListener other)
    {
      return this.GetId().CompareTo(other.GetId());
    }

    public int GetId()
    {
      return this.Id;
    }
  }
}
