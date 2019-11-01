using System;

namespace Socket.Quobject.EngineIoClientDotNet.Client {
  public class EngineIOException : Exception
  {
    public string Transport;
    public object code;

    public EngineIOException(string message)
      : base(message)
    {
    }

    public EngineIOException(Exception cause)
      : base("", cause)
    {
    }

    public EngineIOException(string message, Exception cause)
      : base(message, cause)
    {
    }
  }
}
