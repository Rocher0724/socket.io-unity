using System;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class AckImpl : IAck {
    private readonly ActionTrigger fn;
    private readonly Action<object> fn1;
    private readonly Action<object, object> fn2;
    private readonly Action<object, object, object> fn3;

    public AckImpl(ActionTrigger fn) {
      this.fn = fn;
    }

    public AckImpl(Action<object> fn) {
      this.fn1 = fn;
    }

    public AckImpl(Action<object, object> fn) {
      this.fn2 = fn;
    }

    public AckImpl(Action<object, object, object> fn) {
      this.fn3 = fn;
    }

    public void Call(params object[] args) {
      if (this.fn != null)
        this.fn();
      else if (this.fn1 != null)
        this.fn1(args.Length != 0 ? args[0] : (object) null);
      else if (this.fn2 != null) {
        this.fn2(args.Length != 0 ? args[0] : (object) null, args.Length > 1 ? args[1] : (object) null);
      } else {
        if (this.fn3 == null)
          return;
        this.fn3(args.Length != 0 ? args[0] : (object) null, args.Length > 1 ? args[1] : (object) null,
          args.Length > 2 ? args[2] : (object) null);
      }
    }
  }
}
