namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class Options : Quobject.EngineIoClientDotNet.Client.Socket.Options
  {
    public bool Reconnection = true;
    public long Timeout = -1;
    public bool AutoConnect = true;
    public int ReconnectionAttempts;
    public long ReconnectionDelay;
    public long ReconnectionDelayMax;
  }
}
