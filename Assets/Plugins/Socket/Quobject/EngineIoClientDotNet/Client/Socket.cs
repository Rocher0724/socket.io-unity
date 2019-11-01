using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.Client.Transports;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.EngineIoClientDotNet.Parser;
using Socket.Quobject.EngineIoClientDotNet.Thread;


namespace Socket.Quobject.EngineIoClientDotNet.Client {
  public class Socket : Emitter {
    public static readonly string EVENT_OPEN = "open";
    public static readonly string EVENT_CLOSE = "close";
    public static readonly string EVENT_PACKET = "packet";
    public static readonly string EVENT_DRAIN = "drain";
    public static readonly string EVENT_ERROR = "error";
    public static readonly string EVENT_DATA = "data";
    public static readonly string EVENT_MESSAGE = "message";
    public static readonly string EVENT_UPGRADE_ERROR = "upgradeError";
    public static readonly string EVENT_FLUSH = "flush";
    public static readonly string EVENT_HANDSHAKE = "handshake";
    public static readonly string EVENT_UPGRADING = "upgrading";
    public static readonly string EVENT_UPGRADE = "upgrade";
    public static readonly string EVENT_PACKET_CREATE = "packetCreate";
    public static readonly string EVENT_HEARTBEAT = "heartbeat";
    public static readonly string EVENT_TRANSPORT = "transport";
    public static readonly int Protocol = Parser2.Protocol;
    public static bool PriorWebsocketSuccess = false;
    private bool TimestampRequests = true;
    private ImmutableList<Packet> WriteBuffer = ImmutableList<Packet>.Empty;
    private ImmutableList<ActionTrigger> CallbackBuffer = ImmutableList<ActionTrigger>.Empty;
    private Dictionary<string, string> Cookies = new Dictionary<string, string>();
    public Transport mTransport = (Transport) null;
    private bool Agent = false;
    private bool ForceBase64 = false;
    private bool ForceJsonp = false;
    private int _errorCount = 0;
    private bool Secure;
    private bool Upgrade;
    private bool Upgrading;
    private bool RememberUpgrade;
    private int Port;
    private int PolicyPort;
    private int PrevBufferLen;
    private long PingInterval;
    private long PingTimeout;
    public string Id;
    private string Hostname;
    private string Path;
    private string TimestampParam;
    private ImmutableList<string> Transports;
    private ImmutableList<string> Upgrades;
    private Dictionary<string, string> Query;
    private EasyTimer PingTimeoutTimer;
    private EasyTimer PingIntervalTimer;
    private Socket.ReadyStateEnum ReadyState;
    public Dictionary<string, string> ExtraHeaders;

    public Socket()
      : this(new Socket.Options()) {
    }

    public Socket(string uri)
      : this(uri, (Socket.Options) null) {
    }

    public Socket(string uri, Socket.Options options)
      : this(uri == null ? (Uri) null : Socket.String2Uri(uri), options) {
    }

    private static Uri String2Uri(string uri) {
      if (uri.StartsWith("http") || uri.StartsWith("ws"))
        return new Uri(uri);
      return new Uri("http://" + uri);
    }

    public Socket(Uri uri, Socket.Options options)
      : this(uri == (Uri) null ? options : Socket.Options.FromURI(uri, options)) {
    }

    public Socket(Socket.Options options) {
      if (options.Host != null) {
        string[] strArray = options.Host.Split(':');
        options.Hostname = strArray[0];
        if (strArray.Length > 1)
          options.Port = int.Parse(strArray[strArray.Length - 1]);
      }

      this.Secure = options.Secure;
      this.Hostname = options.Hostname;
      this.Port = options.Port;
      this.Query = options.QueryString != null ? ParseQS.Decode(options.QueryString) : new Dictionary<string, string>();
      if (options.Query != null) {
        foreach (KeyValuePair<string, string> keyValuePair in options.Query)
          this.Query.Add(keyValuePair.Key, keyValuePair.Value);
      }

      this.Upgrade = options.Upgrade;
      this.Path = (options.Path ?? "/engine.io").Replace("/$", "") + "/";
      this.TimestampParam = options.TimestampParam ?? "t";
      this.TimestampRequests = options.TimestampRequests;
      this.Transports = options.Transports ?? ImmutableList<string>.Empty.Add(Polling.NAME).Add(WebSocket.NAME);
      this.PolicyPort = options.PolicyPort != 0 ? options.PolicyPort : 843;
      this.RememberUpgrade = options.RememberUpgrade;
      this.Cookies = options.Cookies;
      if (options.IgnoreServerCertificateValidation)
        ServerCertificate.IgnoreServerCertificateValidation();
      this.ExtraHeaders = options.ExtraHeaders;
    }

    public Socket Open() {
      string name = !this.RememberUpgrade || !Socket.PriorWebsocketSuccess || !this.Transports.Contains(WebSocket.NAME)
        ? this.Transports[0]
        : WebSocket.NAME;
      this.ReadyState = Socket.ReadyStateEnum.OPENING;
      Transport transport = this.CreateTransport(name);
      this.SetTransport(transport);
      ThreadPool.QueueUserWorkItem((WaitCallback) (arg => {
        LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
        logger.Info("Task.Run Open start");
        transport.Open();
        logger.Info("Task.Run Open finish");
      }));
      return this;
    }

    private Transport CreateTransport(string name) {
      Dictionary<string, string> dictionary = new Dictionary<string, string>((IDictionary<string, string>) this.Query);
      dictionary.Add("EIO", Quobject.EngineIoClientDotNet.Parser.Parser2.Protocol.ToString());
      dictionary.Add("transport", name);
      if (this.Id != null)
        dictionary.Add("sid", this.Id);
      Transport.Options options = new Transport.Options();
      options.Hostname = this.Hostname;
      options.Port = this.Port;
      options.Secure = this.Secure;
      options.Path = this.Path;
      options.Query = dictionary;
      options.TimestampRequests = this.TimestampRequests;
      options.TimestampParam = this.TimestampParam;
      options.PolicyPort = this.PolicyPort;
      options.Socket = this;
      options.Agent = this.Agent;
      options.ForceBase64 = this.ForceBase64;
      options.ForceJsonp = this.ForceJsonp;
      options.Cookies = this.Cookies;
      options.ExtraHeaders = this.ExtraHeaders;
      if (name == WebSocket.NAME)
        return (Transport) new WebSocket(options);
      if (name == Polling.NAME)
        return (Transport) new PollingXHR(options);
      throw new EngineIOException("CreateTransport failed");
    }

    private void SetTransport(Transport transport) {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      logger.Info(string.Format("SetTransport setting transport '{0}'", (object) transport.Name));
      if (this.mTransport != null) {
        logger.Info(string.Format("SetTransport clearing existing transport '{0}'", (object) transport.Name));
        this.mTransport.Off();
      }

      this.mTransport = transport;
      this.Emit(Socket.EVENT_TRANSPORT, (object) transport);
      transport.On(Socket.EVENT_DRAIN, (IListener) new Socket.EventDrainListener(this));
      transport.On(Socket.EVENT_PACKET, (IListener) new Socket.EventPacketListener(this));
      transport.On(Socket.EVENT_ERROR, (IListener) new Socket.EventErrorListener(this));
      transport.On(Socket.EVENT_CLOSE, (IListener) new Socket.EventCloseListener(this));
    }

    internal void OnDrain() {
      for (int index = 0; index < this.PrevBufferLen; ++index) {
        try {
          ActionTrigger actionTrigger = this.CallbackBuffer[index];
          if (actionTrigger != null)
            actionTrigger();
        } catch (ArgumentOutOfRangeException ex) {
          this.WriteBuffer = this.WriteBuffer.Clear();
          this.CallbackBuffer = this.CallbackBuffer.Clear();
          this.PrevBufferLen = 0;
        }
      }

      try {
        this.WriteBuffer = this.WriteBuffer.RemoveRange(0, this.PrevBufferLen);
        this.CallbackBuffer = this.CallbackBuffer.RemoveRange(0, this.PrevBufferLen);
      } catch (Exception ex) {
        this.WriteBuffer = this.WriteBuffer.Clear();
        this.CallbackBuffer = this.CallbackBuffer.Clear();
      }

      this.PrevBufferLen = 0;
      if (this.WriteBuffer.Count == 0)
        this.Emit(Socket.EVENT_DRAIN);
      else
        this.Flush();
    }

    private bool Flush() {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      logger.Info(string.Format("ReadyState={0} Transport.Writeable={1} Upgrading={2} WriteBuffer.Count={3}",
        (object) this.ReadyState, (object) this.mTransport.Writable, (object) this.Upgrading,
        (object) this.WriteBuffer.Count));
      if (this.ReadyState != Socket.ReadyStateEnum.CLOSED && this.mTransport.Writable && !this.Upgrading &&
          (uint) this.WriteBuffer.Count > 0U) {
        logger.Info(string.Format("Flush {0} packets in socket", (object) this.WriteBuffer.Count));
        this.PrevBufferLen = this.WriteBuffer.Count;
        this.mTransport.Send(this.WriteBuffer);
        this.Emit(Socket.EVENT_FLUSH);
        return true;
      }

      logger.Info(string.Format("Flush Not Send"));
      return false;
    }

    public void OnPacket(Packet packet) {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      if (this.ReadyState == Socket.ReadyStateEnum.OPENING || this.ReadyState == Socket.ReadyStateEnum.OPEN) {
        logger.Info(string.Format("socket received: type '{0}', data '{1}'", (object) packet.Type, packet.Data));
        this.Emit(Socket.EVENT_PACKET, (object) packet);
        if (packet.Type == Packet.OPEN)
          this.OnHandshake(new HandshakeData((string) packet.Data));
        else if (packet.Type == Packet.PONG)
          this.SetPing();
        else if (packet.Type == Packet.ERROR) {
          this.Emit(Socket.EVENT_ERROR, (object) new EngineIOException("server error") {
            code = packet.Data
          });
        } else {
          if (!(packet.Type == Packet.MESSAGE))
            return;
          this.Emit(Socket.EVENT_DATA, packet.Data);
          this.Emit(Socket.EVENT_MESSAGE, packet.Data);
        }
      } else
        logger.Info(string.Format("OnPacket packet received with socket readyState '{0}'", (object) this.ReadyState));
    }

    private void OnHandshake(HandshakeData handshakeData) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info(nameof(OnHandshake));
      this.Emit(Socket.EVENT_HANDSHAKE, (object) handshakeData);
      this.Id = handshakeData.Sid;
      this.mTransport.Query.Add("sid", handshakeData.Sid);
      this.Upgrades = this.FilterUpgrades((IEnumerable<string>) handshakeData.Upgrades);
      this.PingInterval = handshakeData.PingInterval;
      this.PingTimeout = handshakeData.PingTimeout;
      this.OnOpen();
      if (Socket.ReadyStateEnum.CLOSED == this.ReadyState)
        return;
      this.SetPing();
      this.Off(Socket.EVENT_HEARTBEAT, (IListener) new Socket.OnHeartbeatAsListener(this));
      this.On(Socket.EVENT_HEARTBEAT, (IListener) new Socket.OnHeartbeatAsListener(this));
    }

    private void SetPing() {
      if (this.PingIntervalTimer != null)
        this.PingIntervalTimer.Stop();
      this.PingIntervalTimer = EasyTimer.SetTimeout((ActionTrigger) (() => {
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format(
          "writing ping packet - expecting pong within {0}ms, ReadyState = {1}", (object) this.PingTimeout,
          (object) this.ReadyState));
        this.Ping();
        this.OnHeartbeat(this.PingTimeout);
      }), (int) this.PingInterval);
    }

    private void Ping() {
      this.SendPacket(Packet.PING);
    }

    public void Write(string msg, ActionTrigger fn = null) {
      this.Send(msg, fn);
    }

    public void Write(byte[] msg, ActionTrigger fn = null) {
      this.Send(msg, fn);
    }

    public void Send(string msg, ActionTrigger fn = null) {
      this.SendPacket(Packet.MESSAGE, msg, fn);
    }

    public void Send(byte[] msg, ActionTrigger fn = null) {
      this.SendPacket(Packet.MESSAGE, msg, fn);
    }

    private void SendPacket(string type) {
      this.SendPacket(new Packet(type), (ActionTrigger) null);
    }

    private void SendPacket(string type, string data, ActionTrigger fn) {
      this.SendPacket(new Packet(type, (object) data), fn);
    }

    private void SendPacket(string type, byte[] data, ActionTrigger fn) {
      this.SendPacket(new Packet(type, (object) data), fn);
    }

    private void SendPacket(Packet packet, ActionTrigger fn) {
      if (fn == null)
        fn = (ActionTrigger) (() => { });
      this.Emit(Socket.EVENT_PACKET_CREATE, (object) packet);
      this.WriteBuffer = this.WriteBuffer.Add(packet);
      this.CallbackBuffer = this.CallbackBuffer.Add(fn);
      this.Flush();
    }

    private void WaitForUpgrade() {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      Stopwatch stopwatch = new Stopwatch();
      try {
        stopwatch.Start();
        while (this.Upgrading) {
          if (stopwatch.ElapsedMilliseconds > 1000L) {
            logger.Info("Wait for upgrade timeout");
            break;
          }
        }
      } finally {
        stopwatch.Stop();
      }
    }

    private void OnOpen() {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      this.ReadyState = Socket.ReadyStateEnum.OPEN;
      Socket.PriorWebsocketSuccess = WebSocket.NAME == this.mTransport.Name;
      this.Flush();
      this.Emit(Socket.EVENT_OPEN);
      if (this.ReadyState != Socket.ReadyStateEnum.OPEN || !this.Upgrade || !(this.mTransport is Polling))
        return;
      logger.Info("OnOpen starting upgrade probes");
      this._errorCount = 0;
      foreach (string upgrade in this.Upgrades)
        this.Probe(upgrade);
    }

    private void Probe(string name) {
      LogManager.GetLogger(Global.CallerName("", 0, ""))
        .Info(string.Format("Probe probing transport '{0}'", (object) name));
      Socket.PriorWebsocketSuccess = false;
      Transport transport = this.CreateTransport(name);
      Socket.ProbeParameters parameters = new Socket.ProbeParameters() {
        Transport = ImmutableList<Transport>.Empty.Add(transport),
        Failed = ImmutableList<bool>.Empty.Add(false),
        Cleanup = ImmutableList<ActionTrigger>.Empty,
        Socket = this
      };
      Socket.OnTransportOpenListener onTransportOpen = new Socket.OnTransportOpenListener(parameters);
      Socket.FreezeTransportListener freezeTransport = new Socket.FreezeTransportListener(parameters);
      Socket.ProbingOnErrorListener onError =
        new Socket.ProbingOnErrorListener(this, parameters.Transport, (IListener) freezeTransport);
      Socket.ProbingOnTransportCloseListener onTransportClose = new Socket.ProbingOnTransportCloseListener(onError);
      Socket.ProbingOnCloseListener onClose = new Socket.ProbingOnCloseListener(onError);
      Socket.ProbingOnUpgradeListener onUpgrade =
        new Socket.ProbingOnUpgradeListener(freezeTransport, parameters.Transport);
      parameters.Cleanup = parameters.Cleanup.Add((ActionTrigger) (() => {
        if (parameters.Transport.Count < 1)
          return;
        parameters.Transport[0].Off(Transport.EVENT_OPEN, (IListener) onTransportOpen);
        parameters.Transport[0].Off(Transport.EVENT_ERROR, (IListener) onError);
        parameters.Transport[0].Off(Transport.EVENT_CLOSE, (IListener) onTransportClose);
        this.Off(Socket.EVENT_CLOSE, (IListener) onClose);
        this.Off(Socket.EVENT_UPGRADING, (IListener) onUpgrade);
      }));
      parameters.Transport[0].Once(Transport.EVENT_OPEN, (IListener) onTransportOpen);
      parameters.Transport[0].Once(Transport.EVENT_ERROR, (IListener) onError);
      parameters.Transport[0].Once(Transport.EVENT_CLOSE, (IListener) onTransportClose);
      this.Once(Socket.EVENT_CLOSE, (IListener) onClose);
      this.Once(Socket.EVENT_UPGRADING, (IListener) onUpgrade);
      parameters.Transport[0].Open();
    }

    public Socket Close() {
      if (this.ReadyState == Socket.ReadyStateEnum.OPENING || this.ReadyState == Socket.ReadyStateEnum.OPEN) {
        LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
        logger.Info("Start");
        this.OnClose("forced close", (Exception) null);
        logger.Info("socket closing - telling transport to close");
        if (this.mTransport != null)
          this.mTransport.Close();
      }

      return this;
    }

    private void OnClose(string reason, Exception desc = null) {
      if (this.ReadyState != Socket.ReadyStateEnum.OPENING && this.ReadyState != Socket.ReadyStateEnum.OPEN)
        return;
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      logger.Info(string.Format("OnClose socket close with reason: {0}", (object) reason));
      if (this.PingIntervalTimer != null)
        this.PingIntervalTimer.Stop();
      if (this.PingTimeoutTimer != null)
        this.PingTimeoutTimer.Stop();
      EasyTimer.SetTimeout((ActionTrigger) (() => {
        this.WriteBuffer = ImmutableList<Packet>.Empty;
        this.CallbackBuffer = ImmutableList<ActionTrigger>.Empty;
        this.PrevBufferLen = 0;
      }), 1);
      if (this.mTransport != null) {
        this.mTransport.Off(Socket.EVENT_CLOSE);
        this.mTransport.Close();
        this.mTransport.Off();
      }

      this.ReadyState = Socket.ReadyStateEnum.CLOSED;
      logger.Info("Socket.ReadyState = CLOSE");
      this.Id = (string) null;
      this.Emit(Socket.EVENT_CLOSE, (object) reason, (object) desc);
    }

    public ImmutableList<string> FilterUpgrades(IEnumerable<string> upgrades) {
      ImmutableList<string> immutableList = ImmutableList<string>.Empty;
      foreach (string upgrade in upgrades) {
        if (this.Transports.Contains(upgrade))
          immutableList = immutableList.Add(upgrade);
      }

      return immutableList;
    }

    internal void OnHeartbeat(long timeout) {
      if (this.PingTimeoutTimer != null) {
        this.PingTimeoutTimer.Stop();
        this.PingTimeoutTimer = (EasyTimer) null;
      }

      if (timeout <= 0L)
        timeout = this.PingInterval + this.PingTimeout;
      this.PingTimeoutTimer = EasyTimer.SetTimeout((ActionTrigger) (() => {
        LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
        logger.Info("EasyTimer OnHeartbeat start");
        if (this.ReadyState == Socket.ReadyStateEnum.CLOSED)
          return;
        this.OnClose("ping timeout", (Exception) null);
        logger.Info("EasyTimer OnHeartbeat finish");
      }), (int) timeout);
    }

    internal void OnError(Exception exception) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Error("socket error", exception);
      Socket.PriorWebsocketSuccess = false;
      if (this._errorCount != 0)
        return;
      ++this._errorCount;
      this.Emit(Socket.EVENT_ERROR, (object) exception);
      this.OnClose("transport error", exception);
    }

    private enum ReadyStateEnum {
      OPENING,
      OPEN,
      CLOSING,
      CLOSED,
    }

    private class EventDrainListener : IListener, IComparable<IListener> {
      private Socket socket;

      public EventDrainListener(Socket socket) {
        this.socket = socket;
      }

      void IListener.Call(params object[] args) {
        this.socket.OnDrain();
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class EventPacketListener : IListener, IComparable<IListener> {
      private Socket socket;

      public EventPacketListener(Socket socket) {
        this.socket = socket;
      }

      void IListener.Call(params object[] args) {
        this.socket.OnPacket(args.Length != 0 ? (Packet) args[0] : (Packet) null);
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class EventErrorListener : IListener, IComparable<IListener> {
      private Socket socket;

      public EventErrorListener(Socket socket) {
        this.socket = socket;
      }

      public void Call(params object[] args) {
        this.socket.OnError(args.Length != 0 ? (Exception) args[0] : (Exception) null);
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class EventCloseListener : IListener, IComparable<IListener> {
      private Socket socket;

      public EventCloseListener(Socket socket) {
        this.socket = socket;
      }

      public void Call(params object[] args) {
        this.socket.OnClose("transport close", (Exception) null);
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    public class Options : Transport.Options {
      public bool Upgrade = true;
      public ImmutableList<string> Transports;
      public bool RememberUpgrade;
      public string Host;
      public string QueryString;

      public static Socket.Options FromURI(Uri uri, Socket.Options opts) {
        if (opts == null)
          opts = new Socket.Options();
        opts.Host = uri.Host;
        opts.Secure = uri.Scheme == "https" || uri.Scheme == "wss";
        opts.Port = uri.Port;
        if (!string.IsNullOrEmpty(uri.Query))
          opts.QueryString = uri.Query;
        return opts;
      }
    }

    private class OnHeartbeatAsListener : IListener, IComparable<IListener> {
      private Socket socket;

      public OnHeartbeatAsListener(Socket socket) {
        this.socket = socket;
      }

      void IListener.Call(params object[] args) {
        this.socket.OnHeartbeat(args.Length != 0 ? (long) args[0] : 0L);
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class ProbeParameters {
      public ImmutableList<Transport> Transport { get; set; }

      public ImmutableList<bool> Failed { get; set; }

      public ImmutableList<ActionTrigger> Cleanup { get; set; }

      public Socket Socket { get; set; }
    }

    private class OnTransportOpenListener : IListener, IComparable<IListener> {
      private Socket.ProbeParameters Parameters;

      public OnTransportOpenListener(Socket.ProbeParameters parameters) {
        this.Parameters = parameters;
      }

      void IListener.Call(params object[] args) {
        if (this.Parameters.Failed[0])
          return;
        Packet packet = new Packet(Packet.PING, (object) "probe");
        this.Parameters.Transport[0].Once(Transport.EVENT_PACKET,
          (IListener) new Socket.OnTransportOpenListener.ProbeEventPacketListener(this));
        this.Parameters.Transport[0].Send(ImmutableList<Packet>.Empty.Add(packet));
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }

      private class ProbeEventPacketListener : IListener, IComparable<IListener> {
        private Socket.OnTransportOpenListener _onTransportOpenListener;

        public ProbeEventPacketListener(
          Socket.OnTransportOpenListener onTransportOpenListener) {
          this._onTransportOpenListener = onTransportOpenListener;
        }

        void IListener.Call(params object[] args) {
          if (this._onTransportOpenListener.Parameters.Failed[0])
            return;
          LogManager log = LogManager.GetLogger(Global.CallerName("", 0, ""));
          Packet packet = (Packet) args[0];
          if (Packet.PONG == packet.Type && "probe" == (string) packet.Data) {
            this._onTransportOpenListener.Parameters.Socket.Upgrading = true;
            this._onTransportOpenListener.Parameters.Socket.Emit(Socket.EVENT_UPGRADING,
              (object) this._onTransportOpenListener.Parameters.Transport[0]);
            Socket.PriorWebsocketSuccess = WebSocket.NAME == this._onTransportOpenListener.Parameters.Transport[0].Name;
            ((Polling) this._onTransportOpenListener.Parameters.Socket.mTransport).Pause((ActionTrigger) (() => {
              if (this._onTransportOpenListener.Parameters.Failed[0] ||
                  (Socket.ReadyStateEnum.CLOSED == this._onTransportOpenListener.Parameters.Socket.ReadyState ||
                   Socket.ReadyStateEnum.CLOSING == this._onTransportOpenListener.Parameters.Socket.ReadyState))
                return;
              log.Info("changing transport and sending upgrade packet");
              this._onTransportOpenListener.Parameters.Cleanup[0]();
              this._onTransportOpenListener.Parameters.Socket.SetTransport(this._onTransportOpenListener.Parameters
                .Transport[0]);
              ImmutableList<Packet> packets = ImmutableList<Packet>.Empty.Add(new Packet(Packet.UPGRADE));
              try {
                this._onTransportOpenListener.Parameters.Transport[0].Send(packets);
                this._onTransportOpenListener.Parameters.Socket.Upgrading = false;
                this._onTransportOpenListener.Parameters.Socket.Flush();
                this._onTransportOpenListener.Parameters.Socket.Emit(Socket.EVENT_UPGRADE,
                  (object) this._onTransportOpenListener.Parameters.Transport[0]);
                this._onTransportOpenListener.Parameters.Transport =
                  this._onTransportOpenListener.Parameters.Transport.RemoveAt(0);
              } catch (Exception ex) {
                log.Error("", ex);
              }
            }));
          } else {
            log.Info(string.Format("probe transport '{0}' failed",
              (object) this._onTransportOpenListener.Parameters.Transport[0].Name));
            EngineIOException engineIoException = new EngineIOException("probe error");
            this._onTransportOpenListener.Parameters.Socket.Emit(Socket.EVENT_UPGRADE_ERROR,
              (object) engineIoException);
          }
        }

        public int CompareTo(IListener other) {
          return this.GetId().CompareTo(other.GetId());
        }

        public int GetId() {
          return 0;
        }
      }
    }

    private class FreezeTransportListener : IListener, IComparable<IListener> {
      private Socket.ProbeParameters Parameters;

      public FreezeTransportListener(Socket.ProbeParameters parameters) {
        this.Parameters = parameters;
      }

      void IListener.Call(params object[] args) {
        if (this.Parameters.Failed[0])
          return;
        this.Parameters.Failed = this.Parameters.Failed.SetItem(0, true);
        this.Parameters.Cleanup[0]();
        if (this.Parameters.Transport.Count < 1)
          return;
        this.Parameters.Transport[0].Close();
        this.Parameters.Transport = ImmutableList<Transport>.Empty;
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class ProbingOnErrorListener : IListener, IComparable<IListener> {
      private readonly Socket _socket;
      private readonly ImmutableList<Transport> _transport;
      private readonly IListener _freezeTransport;

      public ProbingOnErrorListener(
        Socket socket,
        ImmutableList<Transport> transport,
        IListener freezeTransport) {
        this._socket = socket;
        this._transport = transport;
        this._freezeTransport = freezeTransport;
      }

      void IListener.Call(params object[] args) {
        object obj = args[0];
        EngineIOException engineIoException = !(obj is Exception)
          ? (!(obj is string)
            ? new EngineIOException("probe error")
            : new EngineIOException("probe error: " + (string) obj))
          : new EngineIOException("probe error", (Exception) obj);
        engineIoException.Transport = this._transport[0].Name;
        this._freezeTransport.Call();
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format(
          "probe transport \"{0}\" failed because of error: {1}", (object) engineIoException.Transport, obj));
        this._socket.Emit(Socket.EVENT_UPGRADE_ERROR, (object) engineIoException);
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class ProbingOnTransportCloseListener : IListener, IComparable<IListener> {
      private readonly IListener _onError;

      public ProbingOnTransportCloseListener(Socket.ProbingOnErrorListener onError) {
        this._onError = (IListener) onError;
      }

      void IListener.Call(params object[] args) {
        this._onError.Call((object) "transport closed");
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class ProbingOnCloseListener : IListener, IComparable<IListener> {
      private IListener _onError;

      public ProbingOnCloseListener(Socket.ProbingOnErrorListener onError) {
        this._onError = (IListener) onError;
      }

      void IListener.Call(params object[] args) {
        this._onError.Call((object) "socket closed");
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }

    private class ProbingOnUpgradeListener : IListener, IComparable<IListener> {
      private readonly IListener _freezeTransport;
      private readonly ImmutableList<Transport> _transport;

      public ProbingOnUpgradeListener(
        Socket.FreezeTransportListener freezeTransport,
        ImmutableList<Transport> transport) {
        this._freezeTransport = (IListener) freezeTransport;
        this._transport = transport;
      }

      void IListener.Call(params object[] args) {
        Transport transport = (Transport) args[0];
        if (this._transport[0] == null || !(transport.Name != this._transport[0].Name))
          return;
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format("'{0}' works - aborting '{1}'",
          (object) transport.Name, (object) this._transport[0].Name));
        this._freezeTransport.Call();
      }

      public int CompareTo(IListener other) {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId() {
        return 0;
      }
    }
  }
}