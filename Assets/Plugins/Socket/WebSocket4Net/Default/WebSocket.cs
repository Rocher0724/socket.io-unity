using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Socket.WebSocket4Net.Command;
using Socket.WebSocket4Net.Protocol;
using Socket.WebSocket4Net.SuperSocket.ClientEngine;
using Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol;

namespace Socket.WebSocket4Net.Default {
  public class WebSocket : IDisposable {
    private Dictionary<string, ICommand<WebSocket, WebSocketCommandInfo>> m_CommandDict =
      new Dictionary<string, ICommand<WebSocket, WebSocketCommandInfo>>(
        (IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);

    private static ProtocolProcessorFactory m_ProtocolProcessorFactory = new ProtocolProcessorFactory(
      new IProtocolProcessor[3] {
        (IProtocolProcessor) new Rfc6455Processor(),
        (IProtocolProcessor) new DraftHybi10Processor(),
        (IProtocolProcessor) new DraftHybi00Processor()
      });

    protected const string UserAgentKey = "UserAgent";
    private const string m_UriScheme = "ws";
    private const string m_UriPrefix = "ws://";
    private const string m_SecureUriScheme = "wss";
    private const int m_SecurePort = 443;
    private const string m_SecureUriPrefix = "wss://";
    private const string m_NotOpenSendingMessage = "You must send data by websocket after websocket is opened!";
    private int m_StateCode;
    private EndPoint m_HttpConnectProxy;
    private Timer m_WebSocketTimer;
    private string m_LastPingRequest;
    private bool m_disposed;
    private EventHandler m_Opened;
    private EventHandler<MessageReceivedEventArgs> m_MessageReceived;
    private EventHandler<DataReceivedEventArgs> m_DataReceived;
    private ClosedEventArgs m_ClosedArgs;
    private EventHandler m_Closed;
    private EventHandler<ErrorEventArgs> m_Error;
    private bool m_AllowUnstrustedCertificate;

    internal TcpClientSession Client { get; private set; }

    public WebSocketVersion Version { get; private set; }

    public DateTime LastActiveTime { get; internal set; }

    public bool EnableAutoSendPing { get; set; }

    public int AutoSendPingInterval { get; set; }

    internal IProtocolProcessor ProtocolProcessor { get; private set; }

    public bool SupportBinary {
      get { return this.ProtocolProcessor.SupportBinary; }
    }

    internal Uri TargetUri { get; private set; }

    internal string SubProtocol { get; private set; }

    internal IDictionary<string, object> Items { get; private set; }

    internal List<KeyValuePair<string, string>> Cookies { get; private set; }

    internal List<KeyValuePair<string, string>> CustomHeaderItems { get; private set; }

    internal int StateCode {
      get { return this.m_StateCode; }
    }

    public WebSocketState State {
      get { return (WebSocketState) this.m_StateCode; }
    }

    public bool Handshaked { get; private set; }

    public IProxyConnector Proxy { get; set; }

    internal EndPoint HttpConnectProxy {
      get { return this.m_HttpConnectProxy; }
    }

    protected IClientCommandReader<WebSocketCommandInfo> CommandReader { get; private set; }

    internal bool NotSpecifiedVersion { get; private set; }

    internal string LastPongResponse { get; set; }

    internal string HandshakeHost { get; private set; }

    internal string Origin { get; private set; }

    public bool NoDelay { get; set; }

    private EndPoint ResolveUri(string uri, int defaultPort, out int port) {
      this.TargetUri = new Uri(uri);
      if (string.IsNullOrEmpty(this.Origin))
        this.Origin = this.TargetUri.GetOrigin();
      port = this.TargetUri.Port;
      if (port <= 0)
        port = defaultPort;
      IPAddress address;
      return !IPAddress.TryParse(this.TargetUri.Host, out address)
        ? (EndPoint) new DnsEndPoint(this.TargetUri.Host, port)
        : (EndPoint) new IPEndPoint(address, port);
    }

    private TcpClientSession CreateClient(string uri) {
      int port;
      EndPoint endPoint = this.ResolveUri(uri, 80, out port);
      this.HandshakeHost = port != 80 ? this.TargetUri.Host + ":" + (object) port : this.TargetUri.Host;
      return (TcpClientSession) new AsyncTcpSession(this.m_HttpConnectProxy ?? endPoint);
    }

    private TcpClientSession CreateSecureClient(string uri) {
      int num = uri.IndexOf('/', "wss://".Length);
      if (num < 0) {
        if (uri.IndexOf(':', "wss://".Length, uri.Length - "wss://".Length) < 0)
          uri = uri + ":" + (object) 443 + "/";
        else
          uri += "/";
      } else {
        if (num == "wss://".Length)
          throw new ArgumentException("Invalid uri", nameof(uri));
        if (uri.IndexOf(':', "wss://".Length, num - "wss://".Length) < 0)
          uri = uri.Substring(0, num) + ":" + (object) 443 + uri.Substring(num);
      }

      int port;
      EndPoint endPoint = this.ResolveUri(uri, 443, out port);
      this.HandshakeHost = port != 443 ? this.TargetUri.Host + ":" + (object) port : this.TargetUri.Host;
      return (TcpClientSession) new SslStreamTcpSession(this.m_HttpConnectProxy ?? endPoint);
    }

    private void Initialize(
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      List<KeyValuePair<string, string>> customHeaderItems,
      string userAgent,
      string origin,
      WebSocketVersion version,
      EndPoint httpConnectProxy) {
      if (version == WebSocketVersion.None) {
        this.NotSpecifiedVersion = true;
        version = WebSocketVersion.Rfc6455;
      }

      this.Version = version;
      this.ProtocolProcessor = WebSocket.GetProtocolProcessor(version);
      this.Cookies = cookies;
      this.Origin = origin;
      if (!string.IsNullOrEmpty(userAgent)) {
        if (customHeaderItems == null)
          customHeaderItems = new List<KeyValuePair<string, string>>();
        customHeaderItems.Add(new KeyValuePair<string, string>("UserAgent", userAgent));
      }

      if (customHeaderItems != null && customHeaderItems.Count > 0)
        this.CustomHeaderItems = customHeaderItems;
      Handshake handshake = new Handshake();
      this.m_CommandDict.Add(handshake.Name, (ICommand<WebSocket, WebSocketCommandInfo>) handshake);
      Text text = new Text();
      this.m_CommandDict.Add(text.Name, (ICommand<WebSocket, WebSocketCommandInfo>) text);
      Binary binary = new Binary();
      this.m_CommandDict.Add(binary.Name, (ICommand<WebSocket, WebSocketCommandInfo>) binary);
      Close close = new Close();
      this.m_CommandDict.Add(close.Name, (ICommand<WebSocket, WebSocketCommandInfo>) close);
      Ping ping = new Ping();
      this.m_CommandDict.Add(ping.Name, (ICommand<WebSocket, WebSocketCommandInfo>) ping);
      Pong pong = new Pong();
      this.m_CommandDict.Add(pong.Name, (ICommand<WebSocket, WebSocketCommandInfo>) pong);
      BadRequest badRequest = new BadRequest();
      this.m_CommandDict.Add(badRequest.Name, (ICommand<WebSocket, WebSocketCommandInfo>) badRequest);
      this.m_StateCode = -1;
      this.SubProtocol = subProtocol;
      this.Items =
        (IDictionary<string, object>) new Dictionary<string, object>(
          (IEqualityComparer<string>) StringComparer.OrdinalIgnoreCase);
      this.m_HttpConnectProxy = httpConnectProxy;
      TcpClientSession tcpClientSession;
      if (uri.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)) {
        tcpClientSession = this.CreateClient(uri);
      } else {
        if (!uri.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
          throw new ArgumentException("Invalid uri", nameof(uri));
        tcpClientSession = this.CreateSecureClient(uri);
      }

      tcpClientSession.Connected += new EventHandler(this.client_Connected);
      tcpClientSession.Closed += new EventHandler(this.client_Closed);
      tcpClientSession.Error += new EventHandler<ErrorEventArgs>(this.client_Error);
      tcpClientSession.DataReceived += new EventHandler<DataEventArgs>(this.client_DataReceived);
      this.Client = tcpClientSession;
      this.EnableAutoSendPing = true;
    }

    private void client_DataReceived(object sender, DataEventArgs e) {
      this.OnDataReceived(e.Data, e.Offset, e.Length);
    }

    private void client_Error(object sender, ErrorEventArgs e) {
      this.OnError(e);
      this.OnClosed();
    }

    private void client_Closed(object sender, EventArgs e) {
      this.OnClosed();
    }

    private void client_Connected(object sender, EventArgs e) {
      this.OnConnected();
    }

    internal bool GetAvailableProcessor(int[] availableVersions) {
      IProtocolProcessor processorFromAvialable =
        WebSocket.m_ProtocolProcessorFactory.GetPreferedProcessorFromAvialable(availableVersions);
      if (processorFromAvialable == null)
        return false;
      this.ProtocolProcessor = processorFromAvialable;
      return true;
    }

    public int ReceiveBufferSize {
      get { return this.Client.ReceiveBufferSize; }
      set { this.Client.ReceiveBufferSize = value; }
    }

    public void Open() {
      this.m_StateCode = 0;
      if (this.Proxy != null)
        this.Client.Proxy = this.Proxy;
      this.Client.NoDeplay = this.NoDelay;
      this.Client.Connect();
    }

    private static IProtocolProcessor GetProtocolProcessor(
      WebSocketVersion version) {
      IProtocolProcessor processorByVersion = WebSocket.m_ProtocolProcessorFactory.GetProcessorByVersion(version);
      if (processorByVersion == null)
        throw new ArgumentException("Invalid websocket version");
      return processorByVersion;
    }

    private void OnConnected() {
      this.CommandReader =
        (IClientCommandReader<WebSocketCommandInfo>) this.ProtocolProcessor.CreateHandshakeReader(this);
      if (this.Items.Count > 0)
        this.Items.Clear();
      this.ProtocolProcessor.SendHandshake(this);
    }

    protected internal virtual void OnHandshaked() {
      this.m_StateCode = 1;
      this.Handshaked = true;
      if (this.EnableAutoSendPing && this.ProtocolProcessor.SupportPingPong) {
        if (this.AutoSendPingInterval <= 0)
          this.AutoSendPingInterval = 60;
        this.m_WebSocketTimer = new Timer(new TimerCallback(this.OnPingTimerCallback),
          (object) this.ProtocolProcessor, this.AutoSendPingInterval * 1000, this.AutoSendPingInterval * 1000);
      }

      if (this.m_Opened == null)
        return;
      this.m_Opened((object) this, EventArgs.Empty);
    }

    private void OnPingTimerCallback(object state) {
      if (!string.IsNullOrEmpty(this.m_LastPingRequest) && !this.m_LastPingRequest.Equals(this.LastPongResponse))
        return;
      IProtocolProcessor protocolProcessor = state as IProtocolProcessor;
      this.m_LastPingRequest = DateTime.Now.ToString();
      try {
        protocolProcessor.SendPing(this, this.m_LastPingRequest);
      } catch (Exception ex) {
        this.OnError(ex);
      }
    }

    public event EventHandler Opened {
      add { this.m_Opened += value; }
      remove { this.m_Opened -= value; }
    }

    public event EventHandler<MessageReceivedEventArgs> MessageReceived {
      add { this.m_MessageReceived += value; }
      remove { this.m_MessageReceived -= value; }
    }

    internal void FireMessageReceived(string message) {
      if (this.m_MessageReceived == null)
        return;
      this.m_MessageReceived((object) this, new MessageReceivedEventArgs(message));
    }

    public event EventHandler<DataReceivedEventArgs> DataReceived {
      add { this.m_DataReceived += value; }
      remove { this.m_DataReceived -= value; }
    }

    internal void FireDataReceived(byte[] data) {
      if (this.m_DataReceived == null)
        return;
      this.m_DataReceived((object) this, new DataReceivedEventArgs(data));
    }

    private bool EnsureWebSocketOpen() {
      if (this.Handshaked)
        return true;
      this.OnError(new Exception("You must send data by websocket after websocket is opened!"));
      return false;
    }

    public void Send(string message) {
      if (!this.EnsureWebSocketOpen())
        return;
      this.ProtocolProcessor.SendMessage(this, message);
    }

    public void Send(byte[] data, int offset, int length) {
      if (!this.EnsureWebSocketOpen())
        return;
      this.ProtocolProcessor.SendData(this, data, offset, length);
    }

    public void Send(IList<ArraySegment<byte>> segments) {
      if (!this.EnsureWebSocketOpen())
        return;
      this.ProtocolProcessor.SendData(this, segments);
    }

    private void OnClosed() {
      bool flag = false;
      if (this.m_StateCode == 2 || this.m_StateCode == 1 || this.m_StateCode == 0)
        flag = true;
      this.m_StateCode = 3;
      if (!flag)
        return;
      this.FireClosed();
    }

    public void Close() {
      this.Close(string.Empty);
    }

    public void Close(string reason) {
      this.Close((int) this.ProtocolProcessor.CloseStatusCode.NormalClosure, reason);
    }

    public void Close(int statusCode, string reason) {
      this.m_ClosedArgs = new ClosedEventArgs((short) statusCode, reason);
      if (Interlocked.CompareExchange(ref this.m_StateCode, 3, -1) == -1)
        this.OnClosed();
      else if (Interlocked.CompareExchange(ref this.m_StateCode, 2, 0) == 0) {
        TcpClientSession client = this.Client;
        if (client != null && client.IsConnected)
          client.Close();
        else
          this.OnClosed();
      } else {
        this.m_StateCode = 2;
        this.ClearTimer();
        this.m_WebSocketTimer =
          new Timer(new TimerCallback(this.CheckCloseHandshake), (object) null, 5000, -1);
        this.ProtocolProcessor.SendCloseHandshake(this, statusCode, reason);
      }
    }

    private void CheckCloseHandshake(object state) {
      if (this.m_StateCode == 3)
        return;
      try {
        this.CloseWithoutHandshake();
      } catch (Exception ex) {
        this.OnError(ex);
      }
    }

    internal void CloseWithoutHandshake() {
      this.Client?.Close();
    }

    protected void ExecuteCommand(WebSocketCommandInfo commandInfo) {
      ICommand<WebSocket, WebSocketCommandInfo> command;
      if (!this.m_CommandDict.TryGetValue(commandInfo.Key, out command))
        return;
      command.ExecuteCommand(this, commandInfo);
    }

    private void OnDataReceived(byte[] data, int offset, int length) {
      while (true) {
        int left;
        WebSocketCommandInfo commandInfo = this.CommandReader.GetCommandInfo(data, offset, length, out left);
        if (this.CommandReader.NextCommandReader != null)
          this.CommandReader = this.CommandReader.NextCommandReader;
        if (commandInfo != null)
          this.ExecuteCommand(commandInfo);
        if (left > 0) {
          offset = offset + length - left;
          length = left;
        } else
          break;
      }
    }

    internal void FireError(Exception error) {
      this.OnError(error);
    }

    public event EventHandler Closed {
      add { this.m_Closed += value; }
      remove { this.m_Closed -= value; }
    }

    private void ClearTimer() {
      if (this.m_WebSocketTimer == null)
        return;
      this.m_WebSocketTimer.Change(-1, -1);
      this.m_WebSocketTimer.Dispose();
      this.m_WebSocketTimer = (Timer) null;
    }

    private void FireClosed() {
      this.ClearTimer();
      EventHandler closed = this.m_Closed;
      if (closed == null)
        return;
      closed((object) this, (EventArgs) this.m_ClosedArgs ?? EventArgs.Empty);
    }

    public event EventHandler<ErrorEventArgs> Error {
      add { this.m_Error += value; }
      remove { this.m_Error -= value; }
    }

    private void OnError(ErrorEventArgs e) {
      EventHandler<ErrorEventArgs> error = this.m_Error;
      if (error == null)
        return;
      error((object) this, e);
    }

    private void OnError(Exception e) {
      this.OnError(new ErrorEventArgs(e));
    }

    public void Dispose() {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing) {
      if (this.m_disposed)
        return;
      if (disposing) {
        TcpClientSession client = this.Client;
        if (client != null) {
          client.Connected -= new EventHandler(this.client_Connected);
          client.Closed -= new EventHandler(this.client_Closed);
          client.Error -= new EventHandler<ErrorEventArgs>(this.client_Error);
          client.DataReceived -= new EventHandler<DataEventArgs>(this.client_DataReceived);
          if (client.IsConnected)
            client.Close();
          this.Client = (TcpClientSession) null;
        }

        if (this.m_WebSocketTimer != null)
          this.m_WebSocketTimer.Dispose();
      }

      this.m_disposed = true;
    }

    ~WebSocket() {
      this.Dispose(false);
    }

    public bool AllowUnstrustedCertificate {
      get { return this.m_AllowUnstrustedCertificate; }
      set {
        this.m_AllowUnstrustedCertificate = value;
        SslStreamTcpSession client = this.Client as SslStreamTcpSession;
        if (client == null)
          return;
        client.AllowUnstrustedCertificate = this.m_AllowUnstrustedCertificate;
      }
    }

    public WebSocket(string uri)
      : this(uri, string.Empty) {
    }

    public WebSocket(string uri, WebSocketVersion version)
      : this(uri, string.Empty, (List<KeyValuePair<string, string>>) null, version) {
    }

    public WebSocket(string uri, string subProtocol)
      : this(uri, subProtocol, (List<KeyValuePair<string, string>>) null, WebSocketVersion.None) {
    }

    public WebSocket(string uri, List<KeyValuePair<string, string>> cookies)
      : this(uri, string.Empty, cookies, WebSocketVersion.None) {
    }

    public WebSocket(string uri, string subProtocol, List<KeyValuePair<string, string>> cookies)
      : this(uri, subProtocol, cookies, WebSocketVersion.None) {
    }

    public WebSocket(string uri, string subProtocol, WebSocketVersion version)
      : this(uri, subProtocol, (List<KeyValuePair<string, string>>) null, version) {
    }

    public WebSocket(
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      WebSocketVersion version)
      : this(uri, subProtocol, cookies, new List<KeyValuePair<string, string>>(), (string) null, version) {
    }

    public WebSocket(
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      string userAgent,
      WebSocketVersion version)
      : this(uri, subProtocol, cookies, (List<KeyValuePair<string, string>>) null, userAgent, version) {
    }

    public WebSocket(
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      List<KeyValuePair<string, string>> customHeaderItems,
      string userAgent,
      WebSocketVersion version)
      : this(uri, subProtocol, cookies, customHeaderItems, userAgent, string.Empty, version) {
    }

    public WebSocket(
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      List<KeyValuePair<string, string>> customHeaderItems,
      string userAgent,
      string origin,
      WebSocketVersion version)
      : this(uri, subProtocol, cookies, customHeaderItems, userAgent, origin, version, (EndPoint) null) {
    }

    public WebSocket(
      string uri,
      string subProtocol,
      List<KeyValuePair<string, string>> cookies,
      List<KeyValuePair<string, string>> customHeaderItems,
      string userAgent,
      string origin,
      WebSocketVersion version,
      EndPoint httpConnectProxy) {
      this.Initialize(uri, subProtocol, cookies, customHeaderItems, userAgent, origin, version, httpConnectProxy);
    }
  }
}