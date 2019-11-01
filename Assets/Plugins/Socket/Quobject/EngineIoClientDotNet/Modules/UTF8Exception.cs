using System;

namespace Socket.Quobject.EngineIoClientDotNet.Modules {
  public class UTF8Exception : Exception
  {
    public UTF8Exception(string message)
      : base(message)
    {
    }
  }
}
