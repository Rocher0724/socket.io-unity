using System;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class Engine : Quobject.EngineIoClientDotNet.Client.Socket
  {
    public Engine(Uri uri, Quobject.EngineIoClientDotNet.Client.Socket.Options opts)
      : base(uri, opts)
    {
    }
  }
}
