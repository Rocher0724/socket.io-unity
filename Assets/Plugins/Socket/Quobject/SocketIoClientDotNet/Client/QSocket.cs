using System;
using System.Collections.Generic;
using System.Threading;
using Socket.Newtonsoft.Json.Linq;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.SocketIoClientDotNet.Modules;
using Socket.Quobject.SocketIoClientDotNet.Parser;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class QSocket : Emitter {
    public static readonly string EVENT_CONNECT = "connect";
    public static readonly string EVENT_DISCONNECT = "disconnect";
    public static readonly string EVENT_ERROR = "error";
    public static readonly string EVENT_MESSAGE = "message";
    public static readonly string EVENT_CONNECT_ERROR = Manager.EVENT_CONNECT_ERROR;
    public static readonly string EVENT_CONNECT_TIMEOUT = Manager.EVENT_CONNECT_TIMEOUT;
    public static readonly string EVENT_RECONNECT = Manager.EVENT_RECONNECT;
    public static readonly string EVENT_RECONNECT_ERROR = Manager.EVENT_RECONNECT_ERROR;
    public static readonly string EVENT_RECONNECT_FAILED = Manager.EVENT_RECONNECT_FAILED;
    public static readonly string EVENT_RECONNECT_ATTEMPT = Manager.EVENT_RECONNECT_ATTEMPT;
    public static readonly string EVENT_RECONNECTING = Manager.EVENT_RECONNECTING;

    private static readonly List<string> Events = new List<string>() {
      QSocket.EVENT_CONNECT,
      QSocket.EVENT_CONNECT_ERROR,
      QSocket.EVENT_CONNECT_TIMEOUT,
      QSocket.EVENT_DISCONNECT,
      QSocket.EVENT_ERROR,
      QSocket.EVENT_RECONNECT,
      QSocket.EVENT_RECONNECT_ATTEMPT,
      QSocket.EVENT_RECONNECT_FAILED,
      QSocket.EVENT_RECONNECT_ERROR,
      QSocket.EVENT_RECONNECTING
    };

    private ImmutableDictionary<int, IAck> Acks = ImmutableDictionary.Create<int, IAck>();
    private ImmutableQueue<List<object>> ReceiveBuffer = ImmutableQueue.Create<List<object>>();
    private ImmutableQueue<Packet> SendBuffer = ImmutableQueue.Create<Packet>();
    private bool Connected;
    private int Ids;
    private string Nsp;
    private Manager _io;
    private ImmutableQueue<ClientOn.IHandle> Subs;

    public QSocket(Manager io, string nsp) {
      this._io = io;
      this.Nsp = nsp;
      this.SubEvents();
    }

    private void SubEvents() {
      Manager io = this._io;
      this.Subs = ImmutableQueue.Create<ClientOn.IHandle>();
      this.Subs = this.Subs.Enqueue(ClientOn.Create((Emitter) io, Manager.EVENT_OPEN,
        (IListener) new ListenerImpl(new ActionTrigger(this.OnOpen))));
      this.Subs = this.Subs.Enqueue(ClientOn.Create((Emitter) io, Manager.EVENT_PACKET,
        (IListener) new ListenerImpl((Action<object>) (data => this.OnPacket((Packet) data)))));
      this.Subs = this.Subs.Enqueue(ClientOn.Create((Emitter) io, Manager.EVENT_CLOSE,
        (IListener) new ListenerImpl((Action<object>) (data => this.OnClose((string) data)))));
    }

    public QSocket Open() {
      ThreadPool.QueueUserWorkItem((WaitCallback) (arg => {
        if (this.Connected)
          return;
        this._io.Open();
        if (this._io.ReadyState == Manager.ReadyStateEnum.OPEN)
          this.OnOpen();
      }));
      return this;
    }

    public QSocket Connect() {
      return this.Open();
    }

    public QSocket Send(params object[] args) {
      this.Emit(QSocket.EVENT_MESSAGE, args);
      return this;
    }

    public override Emitter Emit(string eventString, params object[] args) {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      if (QSocket.Events.Contains(eventString)) {
        base.Emit(eventString, args);
        return (Emitter) this;
      }

      List<object> objectList = new List<object>() {
        (object) eventString
      };
      objectList.AddRange((IEnumerable<object>) args);
      JArray a = Packet.Args2JArray((IEnumerable<object>) objectList);
      Packet packet = new Packet(HasBinaryData.HasBinary((object) a) ? 5 : 2, (object) a);
      object obj = objectList[objectList.Count - 1];
      if (obj is IAck) {
        logger.Info(string.Format("emitting packet with ack id {0}", (object) this.Ids));
        this.Acks = this.Acks.Add(this.Ids, (IAck) obj);
        JArray jarray = Packet.Remove(a, a.Count - 1);
        packet.Data = (object) jarray;
        packet.Id = this.Ids++;
      }

      if (this.Connected)
        this.Packet_method(packet);
      else
        this.SendBuffer = this.SendBuffer.Enqueue(packet);
      return (Emitter) this;
    }

    public Emitter Emit(string eventString, IAck ack, params object[] args) {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      List<object> objectList = new List<object>() {
        (object) eventString
      };
      if (args != null)
        objectList.AddRange((IEnumerable<object>) args);
      Packet packet = new Packet(2, (object) new JArray((object) objectList));
      logger.Info(string.Format("emitting packet with ack id {0}", (object) this.Ids));
      this.Acks = this.Acks.Add(this.Ids, ack);
      packet.Id = this.Ids++;
      this.Packet_method(packet);
      return (Emitter) this;
    }

    public Emitter Emit(string eventString, ActionTrigger ack, params object[] args) {
      return this.Emit(eventString, (IAck) new AckImpl(ack), args);
    }

    public Emitter Emit(string eventString, Action<object> ack, params object[] args) {
      return this.Emit(eventString, (IAck) new AckImpl(ack), args);
    }

    public Emitter Emit(
      string eventString,
      Action<object, object> ack,
      params object[] args) {
      return this.Emit(eventString, (IAck) new AckImpl(ack), args);
    }

    public Emitter Emit(
      string eventString,
      Action<object, object, object> ack,
      params object[] args) {
      return this.Emit(eventString, (IAck) new AckImpl(ack), args);
    }

    public void Packet_method(Packet packet) {
      packet.Nsp = this.Nsp;
      this._io.Packet(packet);
    }

    private void OnOpen() {
      if (!(this.Nsp != "/"))
        return;
      this.Packet_method(new Packet(0));
    }

    private void OnClose(string reason) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info(string.Format("close ({0})", (object) reason));
      this.Connected = false;
      this.Emit(QSocket.EVENT_DISCONNECT, (object) reason);
    }

    private void OnPacket(Packet packet) {
      if (this.Nsp != packet.Nsp)
        return;
      switch (packet.Type) {
        case 0:
          this.OnConnect();
          break;
        case 1:
          this.OnDisconnect();
          break;
        case 2:
          this.OnEvent(packet);
          break;
        case 3:
          this.OnAck(packet);
          break;
        case 4:
          this.Emit(QSocket.EVENT_ERROR, packet.Data);
          break;
        case 5:
          this.OnEvent(packet);
          break;
        case 6:
          this.OnAck(packet);
          break;
      }
    }

    private void OnEvent(Packet packet) {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      List<object> dataAsList = packet.GetDataAsList();
      logger.Info(string.Format("emitting event {0}", (object) dataAsList));
      if (packet.Id >= 0) {
        logger.Info("attaching ack callback to event");
        dataAsList.Add((object) new QSocket.AckImp(this, packet.Id));
      }

      if (this.Connected) {
        string eventString = (string) dataAsList[0];
        dataAsList.Remove(dataAsList[0]);
        base.Emit(eventString, dataAsList.ToArray());
      } else
        this.ReceiveBuffer = this.ReceiveBuffer.Enqueue(dataAsList);
    }

    private void OnAck(Packet packet) {
      LogManager.GetLogger(Global.CallerName("", 0, ""))
        .Info(string.Format("calling ack {0} with {1}", (object) packet.Id, packet.Data));
      IAck ack = this.Acks[packet.Id];
      this.Acks = this.Acks.Remove(packet.Id);
      List<object> dataAsList = packet.GetDataAsList();
      ack.Call(dataAsList.ToArray());
    }

    private void OnConnect() {
      this.Connected = true;
      this.Emit(QSocket.EVENT_CONNECT);
      this.EmitBuffered();
    }

    private void EmitBuffered() {
      while (!this.ReceiveBuffer.IsEmpty) {
        List<object> objectList;
        this.ReceiveBuffer = this.ReceiveBuffer.Dequeue(out objectList);
        base.Emit((string) objectList[0], objectList.ToArray());
      }

      this.ReceiveBuffer = this.ReceiveBuffer.Clear();
      while (!this.ReceiveBuffer.IsEmpty) {
        Packet packet;
        this.SendBuffer = this.SendBuffer.Dequeue(out packet);
        this.Packet_method(packet);
      }

      this.SendBuffer = this.SendBuffer.Clear();
    }

    private void OnDisconnect() {
      LogManager.GetLogger(Global.CallerName("", 0, ""))
        .Info(string.Format("server disconnect ({0})", (object) this.Nsp));
      this.Destroy();
      this.OnClose("io server disconnect");
    }

    private void Destroy() {
      foreach (ClientOn.IHandle sub in this.Subs)
        sub.Destroy();
      this.Subs = this.Subs.Clear();
      this._io.Destroy(this);
    }

    public QSocket Close() {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      if (this.Connected) {
        logger.Info(string.Format("performing disconnect ({0})", (object) this.Nsp));
        this.Packet_method(new Packet(1));
      }

      this.Destroy();
      if (this.Connected)
        this.OnClose("io client disconnect");
      return this;
    }

    public QSocket Disconnect() {
      return this.Close();
    }

    public Manager Io() {
      return this._io;
    }

    private static IEnumerable<object> ToArray(JArray array) {
      int count = array.Count;
      object[] objArray = new object[count];
      for (int index = 0; index < count; ++index) {
        object obj;
        try {
          obj = (object) array[index];
        } catch (Exception ex) {
          obj = (object) null;
        }

        objArray[index] = obj;
      }

      return (IEnumerable<object>) objArray;
    }

    private class AckImp : IAck {
      private readonly bool[] sent = new bool[1];
      private QSocket socket;
      private int Id;

      public AckImp(QSocket socket, int id) {
        this.socket = socket;
        this.Id = id;
      }

      public void Call(params object[] args) {
        if (this.sent[0])
          return;
        this.sent[0] = true;
        LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
        JArray jarray = Packet.Args2JArray((IEnumerable<object>) args);
        logger.Info(string.Format("sending ack {0}", args.Length != 0 ? (object) jarray.ToString() : (object) "null"));
        this.socket.Packet_method(new Packet(HasBinaryData.HasBinary((object) args) ? 6 : 3, (object) jarray) {
          Id = this.Id
        });
      }
    }
  }
}