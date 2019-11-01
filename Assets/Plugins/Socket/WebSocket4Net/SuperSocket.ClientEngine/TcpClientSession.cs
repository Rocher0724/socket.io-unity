using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public abstract class TcpClientSession : ClientSession {
    private bool m_InConnecting;
    private IBatchQueue<ArraySegment<byte>> m_SendingQueue;
    private PosList<ArraySegment<byte>> m_SendingItems;
    private int m_IsSending;

    protected string HostName { get; private set; }

    public TcpClientSession(EndPoint remoteEndPoint)
      : this(remoteEndPoint, 1024) {
    }

    public TcpClientSession(EndPoint remoteEndPoint, int receiveBufferSize)
      : base(remoteEndPoint) {
      this.ReceiveBufferSize = receiveBufferSize;
      DnsEndPoint dnsEndPoint = remoteEndPoint as DnsEndPoint;
      if (dnsEndPoint != null) {
        this.HostName = dnsEndPoint.Host;
      } else {
        IPEndPoint ipEndPoint = remoteEndPoint as IPEndPoint;
        if (ipEndPoint == null)
          return;
        this.HostName = ipEndPoint.Address.ToString();
      }
    }

    public override int ReceiveBufferSize {
      get { return base.ReceiveBufferSize; }
      set {
        if (this.Buffer.Array != null)
          throw new Exception("ReceiveBufferSize cannot be set after the socket has been connected!");
        base.ReceiveBufferSize = value;
      }
    }

    protected virtual bool IsIgnorableException(Exception e) {
      return e is ObjectDisposedException || e is NullReferenceException;
    }

    protected bool IsIgnorableSocketError(int errorCode) {
      return errorCode == 10058 || errorCode == 10053 || (errorCode == 10054 || errorCode == 995);
    }

    protected abstract void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e);

    public override void Connect() {
      if (this.m_InConnecting)
        throw new Exception("The socket is connecting, cannot connect again!");
      if (this.Client != null)
        throw new Exception("The socket is connected, you needn't connect again!");
      if (this.Proxy != null) {
        this.Proxy.Completed += new EventHandler<ProxyEventArgs>(this.Proxy_Completed);
        this.Proxy.Connect(this.RemoteEndPoint);
        this.m_InConnecting = true;
      } else {
        this.m_InConnecting = true;
        ConnectAsyncExtension.ConnectAsync(this.RemoteEndPoint, new ConnectedCallback(this.ProcessConnect),
          (object) null);
      }
    }

    private void Proxy_Completed(object sender, ProxyEventArgs e) {
      this.Proxy.Completed -= new EventHandler<ProxyEventArgs>(this.Proxy_Completed);
      if (e.Connected) {
        this.ProcessConnect(e.Socket, (object) null, (SocketAsyncEventArgs) null);
      } else {
        this.OnError(new Exception("proxy error", e.Exception));
        this.m_InConnecting = false;
      }
    }

    protected void ProcessConnect(global::System.Net.Sockets.Socket socket, object state, SocketAsyncEventArgs e) {
      if (e != null && e.SocketError != SocketError.Success) {
        e.Dispose();
        this.m_InConnecting = false;
        this.OnError((Exception) new SocketException((int) e.SocketError));
      } else if (socket == null) {
        this.m_InConnecting = false;
        this.OnError((Exception) new SocketException(10053));
      } else if (!socket.Connected) {
        this.m_InConnecting = false;
        this.OnError(
          (Exception) new SocketException(
            (int) socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error)));
      } else {
        if (e == null)
          e = new SocketAsyncEventArgs();
        e.Completed += new EventHandler<SocketAsyncEventArgs>(this.SocketEventArgsCompleted);
        this.Client = socket;
        this.m_InConnecting = false;
        try {
          this.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        } catch {
        }

        this.OnGetSocket(e);
      }
    }

    protected abstract void OnGetSocket(SocketAsyncEventArgs e);

    protected bool EnsureSocketClosed() {
      return this.EnsureSocketClosed((global::System.Net.Sockets.Socket) null);
    }

    protected bool EnsureSocketClosed(global::System.Net.Sockets.Socket prevClient) {
      global::System.Net.Sockets.Socket socket = this.Client;
      if (socket == null)
        return false;
      bool flag = true;
      if (prevClient != null && prevClient != socket) {
        socket = prevClient;
        flag = false;
      } else {
        this.Client = (global::System.Net.Sockets.Socket) null;
        this.m_IsSending = 0;
      }

      try {
        socket.Shutdown(SocketShutdown.Both);
      } catch {
      } finally {
        try {
          socket.Close();
        } catch {
        }
      }

      return flag;
    }

    private bool DetectConnected() {
      if (this.Client != null)
        return true;
      this.OnError((Exception) new SocketException(10057));
      return false;
    }

    private IBatchQueue<ArraySegment<byte>> GetSendingQueue() {
      if (this.m_SendingQueue != null)
        return this.m_SendingQueue;
      lock (this) {
        if (this.m_SendingQueue != null)
          return this.m_SendingQueue;
        this.m_SendingQueue = (IBatchQueue<ArraySegment<byte>>) new ConcurrentBatchQueue<ArraySegment<byte>>(
          Math.Max(this.SendingQueueSize, 1024), (Func<ArraySegment<byte>, bool>) (t => {
            if (t.Array != null)
              return t.Count == 0;
            return true;
          }));
        return this.m_SendingQueue;
      }
    }

    private PosList<ArraySegment<byte>> GetSendingItems() {
      if (this.m_SendingItems == null)
        this.m_SendingItems = new PosList<ArraySegment<byte>>();
      return this.m_SendingItems;
    }

    protected bool IsSending {
      get { return this.m_IsSending == 1; }
    }

    public override bool TrySend(ArraySegment<byte> segment) {
      if (segment.Array == null || segment.Count == 0)
        throw new Exception("The data to be sent cannot be empty.");
      if (!this.DetectConnected())
        return true;
      bool flag = this.GetSendingQueue().Enqueue(segment);
      if (Interlocked.CompareExchange(ref this.m_IsSending, 1, 0) != 0)
        return flag;
      this.DequeueSend();
      return flag;
    }

    public override bool TrySend(IList<ArraySegment<byte>> segments) {
      if (segments == null || segments.Count == 0)
        throw new ArgumentNullException(nameof(segments));
      for (int index = 0; index < segments.Count; ++index) {
        if (segments[index].Count == 0)
          throw new Exception("The data piece to be sent cannot be empty.");
      }

      if (!this.DetectConnected())
        return true;
      bool flag = this.GetSendingQueue().Enqueue(segments);
      if (Interlocked.CompareExchange(ref this.m_IsSending, 1, 0) != 0)
        return flag;
      this.DequeueSend();
      return flag;
    }

    private void DequeueSend() {
      PosList<ArraySegment<byte>> sendingItems = this.GetSendingItems();
      if (!this.m_SendingQueue.TryDequeue((IList<ArraySegment<byte>>) sendingItems))
        this.m_IsSending = 0;
      else
        this.SendInternal(sendingItems);
    }

    protected abstract void SendInternal(PosList<ArraySegment<byte>> items);

    protected void OnSendingCompleted() {
      PosList<ArraySegment<byte>> sendingItems = this.GetSendingItems();
      sendingItems.Clear();
      sendingItems.Position = 0;
      if (!this.m_SendingQueue.TryDequeue((IList<ArraySegment<byte>>) sendingItems))
        this.m_IsSending = 0;
      else
        this.SendInternal(sendingItems);
    }

    public override void Close() {
      if (!this.EnsureSocketClosed())
        return;
      this.OnClosed();
    }
  }
}