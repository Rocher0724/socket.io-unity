using System;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.Modules;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class IO {
    private static readonly ImmutableDictionary<string, Manager> Managers =
      ImmutableDictionary.Create<string, Manager> ();

    public static int Protocol = 4;

    private IO () { }

    public static QSocket Socket (string uri) {
      return IO.Socket (uri, (IO.Options) null);
    }

    public static QSocket Socket (string uri, IO.Options opts) {
      return IO.Socket (Url.Parse (uri), opts);
    }

    public static QSocket Socket (Uri uri) {
      return IO.Socket (uri, (IO.Options) null);
    }

    public static QSocket Socket (Uri uri, IO.Options opts) {
      LogManager logger = LogManager.GetLogger (Global.CallerName ("", 0, ""));
      if (opts == null)
        opts = new IO.Options ();
      Manager manager;
      if (opts.ForceNew || !opts.Multiplex) {
        logger.Info (string.Format ("ignoring socket cache for {0}", (object) uri.ToString ()));
        manager = new Manager (uri, (Quobject.SocketIoClientDotNet.Client.Options) opts);
      } else {
        string id = Url.ExtractId (uri);
        if (!IO.Managers.ContainsKey (id)) {
          logger.Info (string.Format ("new io instance for {0}", (object) id));
          IO.Managers.Add (id, new Manager (uri, (Quobject.SocketIoClientDotNet.Client.Options) opts));
        }

        manager = IO.Managers[id];
      }

      return manager.Socket (uri.PathAndQuery);
    }

    public class Options : Quobject.SocketIoClientDotNet.Client.Options {
      public bool ForceNew = true;
      public bool Multiplex = true;
    }
  }
}