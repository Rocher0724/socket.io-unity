using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class On {
    private On() {
    }

    public static On.IHandle Create(Emitter obj, string ev, IListener fn) {
      obj.On(ev, fn);
      return (On.IHandle) new On.HandleImpl(obj, ev, fn);
    }

    public class HandleImpl : On.IHandle {
      private Emitter obj;
      private string ev;
      private IListener fn;

      public HandleImpl(Emitter obj, string ev, IListener fn) {
        this.obj = obj;
        this.ev = ev;
        this.fn = fn;
      }

      public void Destroy() {
        this.obj.Off(this.ev, this.fn);
      }
    }

    public class ActionHandleImpl : On.IHandle {
      private ActionTrigger fn;

      public ActionHandleImpl(ActionTrigger fn) {
        this.fn = fn;
      }

      public void Destroy() {
        this.fn();
      }
    }

    public interface IHandle {
      void Destroy();
    }
  }
}
