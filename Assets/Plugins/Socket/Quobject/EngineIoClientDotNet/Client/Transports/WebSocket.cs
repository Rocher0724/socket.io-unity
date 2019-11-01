using System;
using System.Collections.Generic;
using System.Diagnostics;
using Socket.Quobject.Collections.Immutable;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.EngineIoClientDotNet.Parser;
using Socket.WebSocket4Net.Default;
using Socket.WebSocket4Net.SuperSocket.ClientEngine;
using DataReceivedEventArgs = Socket.WebSocket4Net.Default.DataReceivedEventArgs;

namespace Socket.Quobject.EngineIoClientDotNet.Client.Transports {
  public class WebSocket : Transport {
    public static readonly string NAME = "websocket";
    private WebSocket4Net.Default.WebSocket ws;
    private List<KeyValuePair<string, string>> Cookies;
    private List<KeyValuePair<string, string>> MyExtraHeaders;

    public WebSocket(Transport.Options opts)
      : base(opts) {
      this.Name = WebSocket.NAME;
      this.Cookies = new List<KeyValuePair<string, string>>();
      foreach (KeyValuePair<string, string> cookie in opts.Cookies)
        this.Cookies.Add(new KeyValuePair<string, string>(cookie.Key, cookie.Value));
      this.MyExtraHeaders = new List<KeyValuePair<string, string>>();
      foreach (KeyValuePair<string, string> extraHeader in opts.ExtraHeaders)
        this.MyExtraHeaders.Add(new KeyValuePair<string, string>(extraHeader.Key, extraHeader.Value));
    }

    protected override void DoOpen() {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("DoOpen uri =" + this.Uri());
      this.ws = new WebSocket4Net.Default.WebSocket(this.Uri(), "", this.Cookies, this.MyExtraHeaders, "", "",
        WebSocketVersion.None);
      this.ws.EnableAutoSendPing = false;
      this.ws.Opened += new EventHandler(this.ws_Opened);
      this.ws.Closed += new EventHandler(this.ws_Closed);
      this.ws.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.ws_MessageReceived);
      this.ws.DataReceived += new EventHandler<DataReceivedEventArgs>(this.ws_DataReceived);
      this.ws.Error += new EventHandler<ErrorEventArgs>(this.ws_Error);
      this.ws.Open();
    }

    private void ws_DataReceived(object sender, DataReceivedEventArgs e) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("ws_DataReceived " + (object) e.Data);
      this.OnData(e.Data);
    }

    private void ws_Opened(object sender, EventArgs e) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("ws_Opened " + this.ws.SupportBinary.ToString());
      this.OnOpen();
    }

    private void ws_Closed(object sender, EventArgs e) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info(nameof(ws_Closed));
      this.ws.Opened -= new EventHandler(this.ws_Opened);
      this.ws.Closed -= new EventHandler(this.ws_Closed);
      this.ws.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.ws_MessageReceived);
      this.ws.DataReceived -= new EventHandler<DataReceivedEventArgs>(this.ws_DataReceived);
      this.ws.Error -= new EventHandler<ErrorEventArgs>(this.ws_Error);
      this.OnClose();
    }

    private void ws_MessageReceived(object sender, MessageReceivedEventArgs e) {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("ws_MessageReceived e.Message= " + e.Message);
      this.OnData(e.Message);
    }

    private void ws_Error(object sender, ErrorEventArgs e) {
      this.OnError("websocket error", e.Exception);
    }

    protected override void Write(ImmutableList<Packet> packets) {
      this.Writable = false;
      foreach (Packet packet in packets)
        Quobject.EngineIoClientDotNet.Parser.Parser2.EncodePacket(packet,
          (IEncodeCallback) new WebSocket.WriteEncodeCallback(this));
      this.Writable = true;
      this.Emit(Transport.EVENT_DRAIN);
    }

    protected override void DoClose() {
      if (this.ws == null)
        return;
      try {
        this.ws.Close();
      } catch (Exception ex) {
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info("DoClose ws.Close() Exception= " + ex.Message);
      }
    }

    public string Uri() {
      Dictionary<string, string> dictionary = this.Query == null
        ? new Dictionary<string, string>()
        : new Dictionary<string, string>((IDictionary<string, string>) this.Query);
      string str1 = this.Secure ? "wss" : "ws";
      string str2 = "";
      if (this.TimestampRequests)
        dictionary.Add(this.TimestampParam, DateTime.Now.Ticks.ToString() + "-" + (object) Transport.Timestamps++);
      string str3 = ParseQS.Encode(dictionary);
      if (this.Port > 0 && ("wss" == str1 && this.Port != 443 || "ws" == str1 && this.Port != 80))
        str2 = ":" + (object) this.Port;
      if (str3.Length > 0)
        str3 = "?" + str3;
      return str1 + "://" + this.Hostname + str2 + this.Path + str3;
    }

    public class WriteEncodeCallback : IEncodeCallback {
      private WebSocket webSocket;

      public WriteEncodeCallback(WebSocket webSocket) {
        this.webSocket = webSocket;
      }

      public void Call(object data) {
        if (data is string) {
          this.webSocket.ws.Send((string) data);
        } else {
          if (!(data is byte[]))
            return;
          byte[] data1 = (byte[]) data;
          this.webSocket.ws.Send(data1, 0, data1.Length);
        }
      }
    }
  }
}