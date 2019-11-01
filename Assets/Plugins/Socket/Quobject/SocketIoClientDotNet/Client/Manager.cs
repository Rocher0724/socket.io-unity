using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.EngineIoClientDotNet.Thread;
using Socket.Quobject.SocketIoClientDotNet.Parser;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class Manager : Emitter {
    public static readonly string EVENT_OPEN = "open";
    public static readonly string EVENT_CLOSE = "close";
    public static readonly string EVENT_PACKET = "packet";
    public static readonly string EVENT_ERROR = "error";
    public static readonly string EVENT_CONNECT_ERROR = "connect_error";
    public static readonly string EVENT_CONNECT_TIMEOUT = "connect_timeout";
    public static readonly string EVENT_RECONNECT = "reconnect";
    public static readonly string EVENT_RECONNECT_ERROR = "reconnect_error";
    public static readonly string EVENT_RECONNECT_FAILED = "reconnect_failed";
    public static readonly string EVENT_RECONNECT_ATTEMPT = "reconnect_attempt";
    public static readonly string EVENT_RECONNECTING = "reconnecting";
    public Manager.ReadyStateEnum ReadyState = Manager.ReadyStateEnum.CLOSED;
    private bool _reconnection;
    private bool SkipReconnect;
    private bool Reconnecting;
    private bool Encoding;
    private bool OpenReconnect;
    private int _reconnectionAttempts;
    private long _reconnectionDelay;
    private long _reconnectionDelayMax;
    private long _timeout;
    private int Attempts;
    private Uri Uri;
    private List<Packet> PacketBuffer;
    private ConcurrentQueue<ClientOn.IHandle> Subs;
    private Quobject.EngineIoClientDotNet.Client.Socket.Options Opts;
    private bool AutoConnect;
    private HashSet<QSocket> OpeningSockets;
    public Quobject.EngineIoClientDotNet.Client.Socket EngineSocket;
    private Quobject.SocketIoClientDotNet.Parser.Parser.Encoder Encoder;
    private Quobject.SocketIoClientDotNet.Parser.Parser.Decoder Decoder;
    private ImmutableDictionary<string, QSocket> Nsps;

    public Manager()
      : this((Uri) null, (Options) null) {
    }

    public Manager(Uri uri)
      : this(uri, (Options) null) {
    }

    public Manager(Options opts)
      : this((Uri) null, opts) {
    }

    public Manager(Uri uri, Options opts) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("Init Manager: " + (object) uri);
      if (opts == null)
        opts = new Options();
      if (opts.Path == null)
        opts.Path = "/socket.io";
      this.Opts = (Quobject.EngineIoClientDotNet.Client.Socket.Options) opts;
      this.Nsps = ImmutableDictionary.Create<string, QSocket>();
      this.Subs = new ConcurrentQueue<ClientOn.IHandle>();
      this.Reconnection(opts.Reconnection);
      this.ReconnectionAttempts(opts.ReconnectionAttempts != 0 ? opts.ReconnectionAttempts : int.MaxValue);
      this.ReconnectionDelay(opts.ReconnectionDelay != 0L ? opts.ReconnectionDelay : 1000L);
      this.ReconnectionDelayMax(opts.ReconnectionDelayMax != 0L ? opts.ReconnectionDelayMax : 5000L);
      this.Timeout(opts.Timeout < 0L ? 20000L : opts.Timeout);
      this.ReadyState = Manager.ReadyStateEnum.CLOSED;
      this.Uri = uri;
      this.Attempts = 0;
      this.Encoding = false;
      this.PacketBuffer = new List<Packet>();
      this.OpeningSockets = new HashSet<QSocket>();
      this.Encoder = new Quobject.SocketIoClientDotNet.Parser.Parser.Encoder();
      this.Decoder = new Quobject.SocketIoClientDotNet.Parser.Parser.Decoder();
      this.AutoConnect = opts.AutoConnect;
      if (!this.AutoConnect)
        return;
      this.Open();
    }

    private void EmitAll(string eventString, params object[] args) {
      this.Emit(eventString, args);
      foreach (Emitter emitter in this.Nsps.Values)
        emitter.Emit(eventString, args);
    }

    public bool Reconnection() {
      return this._reconnection;
    }

    private Manager Reconnection(bool v) {
      this._reconnection = v;
      return this;
    }

    public int ReconnectionAttempts() {
      return this._reconnectionAttempts;
    }

    private Manager ReconnectionAttempts(int v) {
      this._reconnectionAttempts = v;
      return this;
    }

    public long ReconnectionDelay() {
      return this._reconnectionDelay;
    }

    private Manager ReconnectionDelay(long v) {
      this._reconnectionDelay = v;
      return this;
    }

    public long ReconnectionDelayMax() {
      return this._reconnectionDelayMax;
    }

    private Manager ReconnectionDelayMax(long v) {
      this._reconnectionDelayMax = v;
      return this;
    }

    public long Timeout() {
      return this._timeout;
    }

    private Manager Timeout(long v) {
      this._timeout = v;
      return this;
    }

    private void MaybeReconnectOnOpen() {
      if (this.OpenReconnect || this.Reconnecting || !this._reconnection)
        return;
      this.OpenReconnect = true;
      this.Reconnect();
    }

    public Manager Open() {
      return this.Open((Manager.IOpenCallback) null);
    }

    private Manager Open(Manager.IOpenCallback fn) {
      LogManager log = LogManager.GetLogger(Global.CallerName("", 0, ""));
      log.Info(string.Format("readyState {0}", (object) this.ReadyState));
      if (this.ReadyState == Manager.ReadyStateEnum.OPEN)
        return this;
      log.Info(string.Format("opening {0}", (object) this.Uri));
      this.EngineSocket = (Quobject.EngineIoClientDotNet.Client.Socket) new Engine(this.Uri, this.Opts);
      Quobject.EngineIoClientDotNet.Client.Socket socket = this.EngineSocket;
      this.ReadyState = Manager.ReadyStateEnum.OPENING;
      this.OpeningSockets.Add(this.Socket(this.Uri.PathAndQuery));
      this.SkipReconnect = false;
      ClientOn.IHandle openSub = ClientOn.Create((Emitter) socket, EngineIoClientDotNet.Client.Socket.EVENT_OPEN,
        (IListener) new ListenerImpl((ActionTrigger) (() => {
          this.OnOpen();
          if (fn == null)
            return;
          fn.Call((Exception) null);
        })));
      ClientOn.IHandle handle = ClientOn.Create((Emitter) socket, EngineIoClientDotNet.Client.Socket.EVENT_ERROR,
        (IListener) new ListenerImpl((Action<object>) (data => {
          log.Info("connect_error");
          this.Cleanup();
          this.ReadyState = Manager.ReadyStateEnum.CLOSED;
          this.EmitAll(Manager.EVENT_CONNECT_ERROR, data);
          if (fn != null)
            fn.Call((Exception) new SocketIOException("Connection error",
              data is Exception ? (Exception) data : (Exception) null));
          else
            this.MaybeReconnectOnOpen();
        })));
      if (this._timeout >= 0L && this.ReadyState == Manager.ReadyStateEnum.CLOSED) {
        int timeout = (int) this._timeout;
        log.Info(string.Format("connection attempt will timeout after {0}", (object) timeout));
        this.Subs.Enqueue((ClientOn.IHandle) new ClientOn.ActionHandleImpl(new ActionTrigger(EasyTimer
          .SetTimeout((ActionTrigger) (() => {
            LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
            logger.Info("Manager Open start");
            logger.Info(string.Format("connect attempt timed out after {0}", (object) timeout));
            openSub.Destroy();
            socket.Close();
            socket.Emit(Quobject.EngineIoClientDotNet.Client.Socket.EVENT_ERROR,
              (object) new SocketIOException("timeout"));
            this.EmitAll(Manager.EVENT_CONNECT_TIMEOUT, (object) timeout);
            logger.Info("Manager Open finish");
          }), timeout).Stop)));
      }

      this.Subs.Enqueue(openSub);
      this.Subs.Enqueue(handle);
      this.EngineSocket.Open();
      return this;
    }

    private void OnOpen() {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("open");
      this.Cleanup();
      this.ReadyState = Manager.ReadyStateEnum.OPEN;
      this.Emit(Manager.EVENT_OPEN);
      Quobject.EngineIoClientDotNet.Client.Socket engineSocket = this.EngineSocket;
      this.Subs.Enqueue(ClientOn.Create((Emitter) engineSocket, Quobject.EngineIoClientDotNet.Client.Socket.EVENT_DATA,
        (IListener) new ListenerImpl((Action<object>) (data => {
          if (data is string) {
            this.OnData((string) data);
          } else {
            if (!(data is byte[]))
              return;
            this.Ondata((byte[]) data);
          }
        }))));
      this.Subs.Enqueue(ClientOn.Create((Emitter) this.Decoder, "decoded",
        (IListener) new ListenerImpl((Action<object>) (data => this.OnDecoded((Packet) data)))));
      this.Subs.Enqueue(ClientOn.Create((Emitter) engineSocket, Quobject.EngineIoClientDotNet.Client.Socket.EVENT_ERROR,
        (IListener) new ListenerImpl((Action<object>) (data => this.OnError((Exception) data)))));
      this.Subs.Enqueue(ClientOn.Create((Emitter) engineSocket, Quobject.EngineIoClientDotNet.Client.Socket.EVENT_CLOSE,
        (IListener) new ListenerImpl((Action<object>) (data => this.OnClose((string) data)))));
    }

    private void OnData(string data) {
      this.Decoder.Add(data);
    }

    private void Ondata(byte[] data) {
      this.Decoder.Add(data);
    }

    private void OnDecoded(Packet packet) {
      this.Emit(Manager.EVENT_PACKET, (object) packet);
    }

    private void OnError(Exception err) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Error("error", err);
      this.EmitAll(Manager.EVENT_ERROR, (object) err);
    }

    public QSocket Socket(string nsp) {
      if (this.Nsps.ContainsKey(nsp))
        return this.Nsps[nsp];
      QSocket socket = new QSocket(this, nsp);
      this.Nsps = this.Nsps.Add(nsp, socket);
      return socket;
    }

    internal void Destroy(QSocket socket) {
      this.OpeningSockets.Remove(socket);
      if (this.OpeningSockets.Count != 0)
        return;
      this.Close();
    }

    internal void Packet(Packet packet) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format("writing packet {0}", (object) packet));
      if (!this.Encoding) {
        this.Encoding = true;
        this.Encoder.Encode(packet,
          (Quobject.SocketIoClientDotNet.Parser.Parser.Encoder.ICallback) new
            Quobject.SocketIoClientDotNet.Parser.Parser.Encoder.CallbackImp((Action<object[]>) (data => {
              foreach (object obj in data) {
                if (obj is string)
                  this.EngineSocket.Write((string) obj, (ActionTrigger) null);
                else if (obj is byte[])
                  this.EngineSocket.Write((byte[]) obj, (ActionTrigger) null);
              }

              this.Encoding = false;
              this.ProcessPacketQueue();
            })));
      } else
        this.PacketBuffer.Add(packet);
    }

    private void ProcessPacketQueue() {
      if (this.PacketBuffer.Count <= 0 || this.Encoding)
        return;
      Packet packet = this.PacketBuffer[0];
      this.PacketBuffer.Remove(packet);
      this.Packet(packet);
    }

    private void Cleanup() {
      ClientOn.IHandle handle;
      while (this.Subs.TryDequeue(out handle))
        handle.Destroy();
    }

    public void Close() {
      this.SkipReconnect = true;
      this.Reconnecting = false;
      if (this.ReadyState != Manager.ReadyStateEnum.OPEN)
        this.Cleanup();
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("Manager.Close()");
      this.ReadyState = Manager.ReadyStateEnum.CLOSED;
      if (this.EngineSocket == null)
        return;
      this.EngineSocket.Close();
    }

    private void OnClose(string reason) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("start");
      this.Cleanup();
      this.ReadyState = Manager.ReadyStateEnum.CLOSED;
      this.Emit(Manager.EVENT_CLOSE, (object) reason);
      if (!this._reconnection || this.SkipReconnect)
        return;
      this.Reconnect();
    }

    private void Reconnect() {
      LogManager log = LogManager.GetLogger(Global.CallerName("", 0, ""));
      if (this.Reconnecting || this.SkipReconnect)
        return;
      ++this.Attempts;
      if (this.Attempts > this._reconnectionAttempts) {
        log.Info("reconnect failed");
        this.EmitAll(Manager.EVENT_RECONNECT_FAILED);
        this.Reconnecting = false;
      } else {
        long num = Math.Min((long) this.Attempts * this.ReconnectionDelay(), this.ReconnectionDelayMax());
        log.Info(string.Format("will wait {0}ms before reconnect attempt", (object) num));
        this.Reconnecting = true;
        this.Subs.Enqueue((ClientOn.IHandle) new ClientOn.ActionHandleImpl(new ActionTrigger(EasyTimer
          .SetTimeout((ActionTrigger) (() => {
            LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
            logger.Info("EasyTimer Reconnect start");
            logger.Info(string.Format("attempting reconnect"));
            this.EmitAll(Manager.EVENT_RECONNECT_ATTEMPT, (object) this.Attempts);
            this.EmitAll(Manager.EVENT_RECONNECTING, (object) this.Attempts);
            this.Open((Manager.IOpenCallback) new Manager.OpenCallbackImp((Action<object>) (err => {
              if (err != null) {
                log.Error("reconnect attempt error", (Exception) err);
                this.Reconnecting = false;
                this.Reconnect();
                this.EmitAll(Manager.EVENT_RECONNECT_ERROR, (object) (Exception) err);
              } else {
                log.Info("reconnect success");
                this.OnReconnect();
              }
            })));
            logger.Info("EasyTimer Reconnect finish");
          }), (int) num).Stop)));
      }
    }

    private void OnReconnect() {
      int attempts = this.Attempts;
      this.Attempts = 0;
      this.Reconnecting = false;
      this.EmitAll(Manager.EVENT_RECONNECT, (object) attempts);
    }

    public enum ReadyStateEnum {
      OPENING,
      OPEN,
      CLOSED,
    }

    public interface IOpenCallback {
      void Call(Exception err);
    }

    public class OpenCallbackImp : Manager.IOpenCallback {
      private Action<object> Fn;

      public OpenCallbackImp(Action<object> fn) {
        this.Fn = fn;
      }

      public void Call(Exception err) {
        this.Fn((object) err);
      }
    }
  }
}