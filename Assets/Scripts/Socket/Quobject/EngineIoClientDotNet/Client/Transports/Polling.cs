
using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Utilities.LinqBridge;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.EngineIoClientDotNet.Parser;

namespace Socket.Quobject.EngineIoClientDotNet.Client.Transports {
  public class Polling : Transport
  {
    public static readonly string NAME = "polling";
    public static readonly string EVENT_POLL = "poll";
    public static readonly string EVENT_POLL_COMPLETE = "pollComplete";
    private bool IsPolling = false;

    public Polling(Transport.Options opts)
      : base(opts)
    {
      this.Name = Polling.NAME;
    }

    protected override void DoOpen()
    {
      this.Poll();
    }

    public void Pause(ActionTrigger onPause)
    {
      this.ReadyState = Transport.ReadyStateEnum.PAUSED;
      ActionTrigger pause = (ActionTrigger) (() =>
      {
        this.ReadyState = Transport.ReadyStateEnum.PAUSED;
        onPause();
      });
      if (this.IsPolling || !this.Writable)
      {
        int[] total = new int[1];
        if (this.IsPolling)
        {
          ++total[0];
          this.Once(Polling.EVENT_POLL_COMPLETE, (IListener) new Polling.PauseEventPollCompleteListener(total, pause));
        }
        if (this.Writable)
          return;
        ++total[0];
        this.Once(Transport.EVENT_DRAIN, (IListener) new Polling.PauseEventDrainListener(total, pause));
      }
      else
        pause();
    }

    private void Poll()
    {
      this.IsPolling = true;
      this.DoPoll();
      this.Emit(Polling.EVENT_POLL);
    }

    protected override void OnData(string data)
    {
      this._onData((object) data);
    }

    protected override void OnData(byte[] data)
    {
      this._onData((object) data);
    }

    private void _onData(object data)
    {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      logger.Info(string.Format("polling got data {0}", data));
      Polling.DecodePayloadCallback decodePayloadCallback = new Polling.DecodePayloadCallback(this);
      if (data is string)
        Quobject.EngineIoClientDotNet.Parser.Parser2.DecodePayload((string) data, (IDecodePayloadCallback) decodePayloadCallback);
      else if (data is byte[])
        Quobject.EngineIoClientDotNet.Parser.Parser2.DecodePayload((byte[]) data, (IDecodePayloadCallback) decodePayloadCallback);
      if (this.ReadyState == Transport.ReadyStateEnum.CLOSED)
        return;
      this.IsPolling = false;
      logger.Info("ReadyState != ReadyStateEnum.CLOSED");
      this.Emit(Polling.EVENT_POLL_COMPLETE);
      if (this.ReadyState == Transport.ReadyStateEnum.OPEN)
        this.Poll();
      else
        logger.Info(string.Format("ignoring poll - transport state {0}", (object) this.ReadyState));
    }

    protected override void DoClose()
    {
      LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
      Polling.CloseListener closeListener = new Polling.CloseListener(this);
      if (this.ReadyState == Transport.ReadyStateEnum.OPEN)
      {
        logger.Info("transport open - closing");
        closeListener.Call();
      }
      else
      {
        logger.Info("transport not open - deferring close");
        this.Once(Transport.EVENT_OPEN, (IListener) closeListener);
      }
    }

    protected override void Write(ImmutableList<Packet> packets)
    {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("Write packets.Count = " + (object) packets.Count);
      this.Writable = false;
      Polling.SendEncodeCallback sendEncodeCallback = new Polling.SendEncodeCallback(this);
      Quobject.EngineIoClientDotNet.Parser.Parser2.EncodePayload(packets.ToArray<Packet>(), (IEncodeCallback) sendEncodeCallback);
    }

    public string Uri()
    {
      Dictionary<string, string> dictionary = new Dictionary<string, string>((IDictionary<string, string>) this.Query);
      string str1 = this.Secure ? "https" : "http";
      string str2 = "";
      if (this.TimestampRequests)
        dictionary.Add(this.TimestampParam, DateTime.Now.Ticks.ToString() + "-" + (object) Transport.Timestamps++);
      dictionary.Add("b64", "1");
      string str3 = ParseQS.Encode(dictionary);
      if (this.Port > 0 && ("https" == str1 && this.Port != 443 || "http" == str1 && this.Port != 80))
        str2 = ":" + (object) this.Port;
      if (str3.Length > 0)
        str3 = "?" + str3;
      return str1 + "://" + this.Hostname + str2 + this.Path + str3;
    }

    protected virtual void DoWrite(byte[] data, ActionTrigger action)
    {
    }

    protected virtual void DoPoll()
    {
    }

    private class PauseEventDrainListener : IListener, IComparable<IListener>
    {
      private int[] total;
      private ActionTrigger pause;

      public PauseEventDrainListener(int[] total, ActionTrigger pause)
      {
        this.total = total;
        this.pause = pause;
      }

      public void Call(params object[] args)
      {
        if (--this.total[0] != 0)
          return;
        this.pause();
      }

      public int CompareTo(IListener other)
      {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId()
      {
        return 0;
      }
    }

    private class PauseEventPollCompleteListener : IListener, IComparable<IListener>
    {
      private int[] total;
      private ActionTrigger pause;

      public PauseEventPollCompleteListener(int[] total, ActionTrigger pause)
      {
        this.total = total;
        this.pause = pause;
      }

      public void Call(params object[] args)
      {
        if (--this.total[0] != 0)
          return;
        this.pause();
      }

      public int CompareTo(IListener other)
      {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId()
      {
        return 0;
      }
    }

    private class DecodePayloadCallback : IDecodePayloadCallback
    {
      private Polling polling;

      public DecodePayloadCallback(Polling polling)
      {
        this.polling = polling;
      }

      public bool Call(Packet packet, int index, int total)
      {
        if (this.polling.ReadyState == Transport.ReadyStateEnum.OPENING)
          this.polling.OnOpen();
        if (packet.Type == Packet.CLOSE)
        {
          this.polling.OnClose();
          return false;
        }
        this.polling.OnPacket(packet);
        return true;
      }
    }

    private class CloseListener : IListener, IComparable<IListener>
    {
      private Polling polling;

      public CloseListener(Polling polling)
      {
        this.polling = polling;
      }

      public void Call(params object[] args)
      {
        this.polling.Write(ImmutableList<Packet>.Empty.Add(new Packet(Packet.CLOSE)));
      }

      public int CompareTo(IListener other)
      {
        return this.GetId().CompareTo(other.GetId());
      }

      public int GetId()
      {
        return 0;
      }
    }

    public class SendEncodeCallback : IEncodeCallback
    {
      private Polling polling;

      public SendEncodeCallback(Polling polling)
      {
        this.polling = polling;
      }

      public void Call(object data)
      {
        this.polling.DoWrite((byte[]) data, (ActionTrigger) (() =>
        {
          this.polling.Writable = true;
          this.polling.Emit(Transport.EVENT_DRAIN);
        }));
      }
    }
  }
}
