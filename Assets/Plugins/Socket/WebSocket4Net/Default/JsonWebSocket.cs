using System;
using System.Collections.Generic;
using Socket.WebSocket4Net.SuperSocket.ClientEngine;

namespace Socket.WebSocket4Net.Default {
  public class JsonWebSocket : IDisposable {
    private static Random m_Random = new Random ();

    private Dictionary<string, IJsonExecutor> m_ExecutorDict =
      new Dictionary<string, IJsonExecutor> ((IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);

    private const string m_QueryTemplateA = "{0}-{1} {2}";
    private const string m_QueryTemplateB = "{0}-{1}";
    private const string m_QueryTemplateC = "{0} {1}";
    private const string m_QueryKeyTokenTemplate = "{0}-{1}";
    private Func<object, string> m_JsonSerialzier;
    private Func<string, Type, object> m_JsonDeserialzier;
    private WebSocket m_WebSocket;
    private bool m_disposed;
    private EventHandler<ErrorEventArgs> m_Error;
    private EventHandler m_Opened;
    private EventHandler m_Closed;

    public void ConfigJsonSerialzier (
      Func<object, string> serialzier,
      Func<string, Type, object> deserializer) {
      this.m_JsonSerialzier = serialzier;
      this.m_JsonDeserialzier = deserializer;
    }

    protected virtual string SerializeObject (object target) {
      Func<object, string> jsonSerialzier = this.m_JsonSerialzier;
      if (jsonSerialzier == null)
        throw new Exception ("Json serialzier is not configured yet.");
      return jsonSerialzier (target);
    }

    protected virtual object DeserializeObject (string json, Type type) {
      Func<string, Type, object> jsonDeserialzier = this.m_JsonDeserialzier;
      if (jsonDeserialzier == null)
        throw new Exception ("Json serialzier is not configured yet.");
      return jsonDeserialzier (json, type);
    }

    public bool EnableAutoSendPing {
      get { return this.m_WebSocket.EnableAutoSendPing; }
      set { this.m_WebSocket.EnableAutoSendPing = value; }
    }

    public int AutoSendPingInterval {
      get { return this.m_WebSocket.AutoSendPingInterval; }
      set { this.m_WebSocket.AutoSendPingInterval = value; }
    }

    public WebSocketState State {
      get { return this.m_WebSocket.State; }
    }

    public JsonWebSocket (string uri)
      : this (uri, string.Empty) { }

    public JsonWebSocket (string uri, WebSocketVersion version)
      : this (uri, string.Empty, (List<KeyValuePair<string, string>>) null, version) { }

    public JsonWebSocket (string uri, string subProtocol)
      : this (uri, subProtocol, (List<KeyValuePair<string, string>>) null, WebSocketVersion.None) { }

    public JsonWebSocket (string uri, List<KeyValuePair<string, string>> cookies)
      : this (uri, string.Empty, cookies, WebSocketVersion.None) { }

    public JsonWebSocket (
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies)
      : this (uri, subProtocol, cookies, WebSocketVersion.None) { }

    public JsonWebSocket (string uri, string subProtocol, WebSocketVersion version)
      : this (uri, subProtocol, (List<KeyValuePair<string, string>>) null, version) { }

    public JsonWebSocket (
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      WebSocketVersion version)
      : this (uri, subProtocol, cookies, (List<KeyValuePair<string, string>>) null, string.Empty, string.Empty,
        version) { }

    public JsonWebSocket (
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      List<KeyValuePair<string, string>> customHeaderItems,
      string userAgent,
      WebSocketVersion version)
      : this (uri, subProtocol, cookies, customHeaderItems, userAgent, string.Empty, version) { }

    public JsonWebSocket (
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      List<KeyValuePair<string, string>> customHeaderItems,
      string userAgent,
      string origin,
      WebSocketVersion version) {
      this.m_WebSocket = new WebSocket (uri, subProtocol, cookies, customHeaderItems, userAgent, origin, version);
      this.m_WebSocket.EnableAutoSendPing = true;
      this.SubscribeEvents ();
    }

    public JsonWebSocket (WebSocket websocket) {
      if (websocket == null)
        throw new ArgumentNullException (nameof (websocket));
      if (websocket.State != WebSocketState.None)
        throw new ArgumentException ("Thed websocket must be in the initial state.", nameof (websocket));
      this.m_WebSocket = websocket;
      this.SubscribeEvents ();
    }

    private void SubscribeEvents () {
      this.m_WebSocket.Closed += new EventHandler (this.m_WebSocket_Closed);
      this.m_WebSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs> (this.m_WebSocket_MessageReceived);
      this.m_WebSocket.Opened += new EventHandler (this.m_WebSocket_Opened);
      this.m_WebSocket.Error += new EventHandler<ErrorEventArgs> (this.m_WebSocket_Error);
    }

    public int ReceiveBufferSize {
      get { return this.m_WebSocket.ReceiveBufferSize; }
      set { this.m_WebSocket.ReceiveBufferSize = value; }
    }

    public void Open () {
      if (this.m_WebSocket.StateCode != -1 && this.m_WebSocket.StateCode != 3)
        return;
      this.m_WebSocket.Open ();
    }

    public void Close () {
      if (this.m_WebSocket == null || this.m_WebSocket.StateCode != 1 && this.m_WebSocket.StateCode != 0)
        return;
      this.m_WebSocket.Close ();
    }

    public event EventHandler<ErrorEventArgs> Error {
      add { this.m_Error += value; }
      remove { this.m_Error -= value; }
    }

    private void m_WebSocket_Error (object sender, ErrorEventArgs e) {
      if (this.m_Error == null)
        return;
      this.m_Error ((object) this, e);
    }

    public event EventHandler Opened {
      add { this.m_Opened += value; }
      remove { this.m_Opened -= value; }
    }

    private void m_WebSocket_Opened (object sender, EventArgs e) {
      if (this.m_Opened == null)
        return;
      this.m_Opened ((object) this, e);
    }

    private void m_WebSocket_MessageReceived (object sender, MessageReceivedEventArgs e) {
      if (string.IsNullOrEmpty (e.Message))
        return;
      int length1 = e.Message.IndexOf (' ');
      string token = string.Empty;
      string name;
      string json;
      if (length1 > 0) {
        name = e.Message.Substring (0, length1);
        json = e.Message.Substring (length1 + 1);
        int length2 = name.IndexOf ('-');
        if (length2 > 0) {
          token = name.Substring (length2 + 1);
          name = name.Substring (0, length2);
        }
      } else {
        name = e.Message;
        json = string.Empty;
      }

      IJsonExecutor executor = this.GetExecutor (name, token);
      if (executor == null)
        return;
      object obj;
      try {
        obj = executor.Type.IsSimpleType ()
          ? ((object) json.GetType () != (object) executor.Type
            ? Convert.ChangeType ((object) json, executor.Type, (IFormatProvider) null)
            : (object) json)
          : this.DeserializeObject (json, executor.Type);
      }
      catch (Exception ex) {
        this.m_WebSocket_Error ((object) this, new ErrorEventArgs (new Exception ("DeserializeObject exception", ex)));
        return;
      }

      try {
        executor.Execute (this, token, obj);
      }
      catch (Exception ex) {
        this.m_WebSocket_Error ((object) this, new ErrorEventArgs (new Exception ("Message handling exception", ex)));
      }
    }

    public event EventHandler Closed {
      add { this.m_Closed += value; }
      remove { this.m_Closed -= value; }
    }

    private void m_WebSocket_Closed (object sender, EventArgs e) {
      if (this.m_Closed == null)
        return;
      this.m_Closed ((object) this, e);
    }

    public void On<T> (string name, Action<T> executor) {
      this.RegisterExecutor<T> (name, string.Empty, (IJsonExecutor) new JsonExecutor<T> (executor));
    }

    public void On<T> (string name, Action<JsonWebSocket, T> executor) {
      this.RegisterExecutor<T> (name, string.Empty, (IJsonExecutor) new JsonExecutorWithSender<T> (executor));
    }

    public void Send (string name, object content) {
      if (string.IsNullOrEmpty (name))
        throw new ArgumentNullException (nameof (name));
      if (content != null) {
        if (!content.GetType ().IsSimpleType ())
          this.m_WebSocket.Send (string.Format ("{0} {1}", (object) name, (object) this.SerializeObject (content)));
        else
          this.m_WebSocket.Send (string.Format ("{0} {1}", (object) name, content));
      } else
        this.m_WebSocket.Send (name);
    }

    public string Query<T> (string name, object content, Action<T> executor) {
      return this.Query<T> (name, content, (IJsonExecutor) new JsonExecutor<T> (executor));
    }

    public string Query<T> (string name, object content, Action<string, T> executor) {
      return this.Query<T> (name, content, (IJsonExecutor) new JsonExecutorWithToken<T> (executor));
    }

    public string Query<T> (string name, object content, Action<JsonWebSocket, T> executor) {
      return this.Query<T> (name, content, (IJsonExecutor) new JsonExecutorWithSender<T> (executor));
    }

    public string Query<T> (string name, object content, Action<JsonWebSocket, string, T> executor) {
      return this.Query<T> (name, content, (IJsonExecutor) new JsonExecutorFull<T> (executor));
    }

    public string Query<T> (
      string name,
      object content,
      Action<JsonWebSocket, T, object> executor,
      object state) {
      return this.Query<T> (name, content, (IJsonExecutor) new JsonExecutorWithSenderAndState<T> (executor, state));
    }

    private string Query<T> (string name, object content, IJsonExecutor executor) {
      if (string.IsNullOrEmpty (name))
        throw new ArgumentNullException (nameof (name));
      int num = JsonWebSocket.m_Random.Next (1000, 9999);
      this.RegisterExecutor<T> (name, num.ToString (), executor);
      if (content != null) {
        if (!content.GetType ().IsSimpleType ())
          this.m_WebSocket.Send (string.Format ("{0}-{1} {2}", (object) name, (object) num,
            (object) this.SerializeObject (content)));
        else
          this.m_WebSocket.Send (string.Format ("{0}-{1} {2}", (object) name, (object) num, content));
      } else
        this.m_WebSocket.Send (string.Format ("{0}-{1}", (object) name, (object) num));

      return num.ToString ();
    }

    private void RegisterExecutor<T> (string name, string token, IJsonExecutor executor) {
      lock (this.m_ExecutorDict) {
        if (string.IsNullOrEmpty (token))
          this.m_ExecutorDict.Add (name, executor);
        else
          this.m_ExecutorDict.Add (string.Format ("{0}-{1}", (object) name, (object) token), executor);
      }
    }

    private IJsonExecutor GetExecutor (string name, string token) {
      string key = name;
      bool flag = false;
      if (!string.IsNullOrEmpty (token)) {
        key = string.Format ("{0}-{1}", (object) name, (object) token);
        flag = true;
      }

      lock (this.m_ExecutorDict) {
        IJsonExecutor jsonExecutor;
        if (!this.m_ExecutorDict.TryGetValue (key, out jsonExecutor))
          return (IJsonExecutor) null;
        if (flag)
          this.m_ExecutorDict.Remove (key);
        return jsonExecutor;
      }
    }

    public void Dispose () {
      this.Dispose (true);
      GC.SuppressFinalize ((object) this);
    }

    protected virtual void Dispose (bool disposing) {
      if (this.m_disposed)
        return;
      if (disposing && this.m_WebSocket != null)
        this.m_WebSocket.Dispose ();
      this.m_disposed = true;
    }

    ~JsonWebSocket () {
      this.Dispose (false);
    }

    public bool AllowUnstrustedCertificate {
      get { return this.m_WebSocket.AllowUnstrustedCertificate; }
      set { this.m_WebSocket.AllowUnstrustedCertificate = value; }
    }
  }
}