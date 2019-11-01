using System;
using System.Collections.Generic;
using System.Text;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.EngineIoClientDotNet.Parser;

namespace Socket.Quobject.EngineIoClientDotNet.Client {
  public abstract class Transport : Emitter
  {
    public static readonly string EVENT_OPEN = "open";
    public static readonly string EVENT_CLOSE = "close";
    public static readonly string EVENT_PACKET = "packet";
    public static readonly string EVENT_DRAIN = "drain";
    public static readonly string EVENT_ERROR = "error";
    public static readonly string EVENT_SUCCESS = "success";
    public static readonly string EVENT_DATA = "data";
    public static readonly string EVENT_REQUEST_HEADERS = "requestHeaders";
    public static readonly string EVENT_RESPONSE_HEADERS = "responseHeaders";
    protected static int Timestamps = 0;
    protected bool Agent = false;
    protected bool ForceBase64 = false;
    protected bool ForceJsonp = false;
    protected Transport.ReadyStateEnum ReadyState = Transport.ReadyStateEnum.CLOSED;
    private bool _writeable;
    private int myVar;
    public string Name;
    public Dictionary<string, string> Query;
    protected bool Secure;
    protected bool TimestampRequests;
    protected int Port;
    protected string Path;
    protected string Hostname;
    protected string TimestampParam;
    protected Socket Socket;
    protected string Cookie;
    protected Dictionary<string, string> ExtraHeaders;

    public bool Writable
    {
      get
      {
        return this._writeable;
      }
      set
      {
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format("Writable: {0} sid={1}", (object) value, (object) this.Socket.Id));
        this._writeable = value;
      }
    }

    public int MyProperty
    {
      get
      {
        return this.myVar;
      }
      set
      {
        this.myVar = value;
      }
    }

    protected Transport(Transport.Options options)
    {
      this.Path = options.Path;
      this.Hostname = options.Hostname;
      this.Port = options.Port;
      this.Secure = options.Secure;
      this.Query = options.Query;
      this.TimestampParam = options.TimestampParam;
      this.TimestampRequests = options.TimestampRequests;
      this.Socket = options.Socket;
      this.Agent = options.Agent;
      this.ForceBase64 = options.ForceBase64;
      this.ForceJsonp = options.ForceJsonp;
      this.Cookie = options.GetCookiesAsString();
      this.ExtraHeaders = options.ExtraHeaders;
    }

    protected Transport OnError(string message, Exception exception)
    {
      Exception exception1 = (Exception) new EngineIOException(message, exception);
      this.Emit(Transport.EVENT_ERROR, (object) exception1);
      return this;
    }

    protected void OnOpen()
    {
      this.ReadyState = Transport.ReadyStateEnum.OPEN;
      this.Writable = true;
      this.Emit(Transport.EVENT_OPEN);
    }

    protected void OnClose()
    {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("Transport.OnClose()");
      this.ReadyState = Transport.ReadyStateEnum.CLOSED;
      this.Emit(Transport.EVENT_CLOSE);
    }

    protected virtual void OnData(string data)
    {
      this.OnPacket(Parser2.DecodePacket(data, false));
    }

    protected virtual void OnData(byte[] data)
    {
      this.OnPacket(Parser2.DecodePacket(data));
    }

    protected void OnPacket(Packet packet)
    {
      this.Emit(Transport.EVENT_PACKET, (object) packet);
    }

    public Transport Open()
    {
      if (this.ReadyState == Transport.ReadyStateEnum.CLOSED)
      {
        this.ReadyState = Transport.ReadyStateEnum.OPENING;
        this.DoOpen();
      }
      return this;
    }

    public Transport Close()
    {
      if (this.ReadyState == Transport.ReadyStateEnum.OPENING || this.ReadyState == Transport.ReadyStateEnum.OPEN)
      {
        this.DoClose();
        this.OnClose();
      }
      return this;
    }

    public Transport Send(ImmutableList<Packet> packets)
    {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("Send called with packets.Count: " + (object) packets.Count);
      int count = packets.Count;
      if (this.ReadyState != Transport.ReadyStateEnum.OPEN)
        throw new EngineIOException("Transport not open");
      this.Write(packets);
      return this;
    }

    protected abstract void DoOpen();

    protected abstract void DoClose();

    protected abstract void Write(ImmutableList<Packet> packets);

    protected enum ReadyStateEnum
    {
      OPENING,
      OPEN,
      CLOSED,
      PAUSED,
    }

    public class Options
    {
      public bool Agent = false;
      public bool ForceBase64 = false;
      public bool ForceJsonp = false;
      public bool Secure = false;
      public bool TimestampRequests = true;
      public bool IgnoreServerCertificateValidation = false;
      public Dictionary<string, string> Cookies = new Dictionary<string, string>();
      public Dictionary<string, string> ExtraHeaders = new Dictionary<string, string>();
      public string Hostname;
      public string Path;
      public string TimestampParam;
      public int Port;
      public int PolicyPort;
      public Dictionary<string, string> Query;
      internal Socket Socket;

      public string GetCookiesAsString()
      {
        StringBuilder stringBuilder = new StringBuilder();
        bool flag = true;
        foreach (KeyValuePair<string, string> cookie in this.Cookies)
        {
          if (!flag)
            stringBuilder.Append("; ");
          stringBuilder.Append(string.Format("{0}={1}", (object) cookie.Key, (object) cookie.Value));
          flag = false;
        }
        return stringBuilder.ToString();
      }
    }
  }
}
