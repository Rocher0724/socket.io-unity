using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public abstract class ClientSession : IClientSession, IBufferSetter {
    private DataEventArgs m_DataArgs = new DataEventArgs();
    private EventHandler m_Closed;
    private EventHandler<ErrorEventArgs> m_Error;
    private EventHandler m_Connected;
    private EventHandler<DataEventArgs> m_DataReceived;

    protected global::System.Net.Sockets.Socket Client { get; set; }

    protected EndPoint RemoteEndPoint { get; set; }

    public bool IsConnected { get; private set; }

    public bool NoDeplay { get; set; }

    public ClientSession() {
    }

    public ClientSession(EndPoint remoteEndPoint) {
      if (remoteEndPoint == null)
        throw new ArgumentNullException(nameof(remoteEndPoint));
      this.RemoteEndPoint = remoteEndPoint;
    }

    public int SendingQueueSize { get; set; }

    public abstract void Connect();

    public abstract bool TrySend(ArraySegment<byte> segment);

    public abstract bool TrySend(IList<ArraySegment<byte>> segments);

    public void Send(byte[] data, int offset, int length) {
      this.Send(new ArraySegment<byte>(data, offset, length));
    }

    public void Send(ArraySegment<byte> segment) {
      if (this.TrySend(segment))
        return;
      do {
        Thread.SpinWait(1);
      } while (!this.TrySend(segment));
    }

    public void Send(IList<ArraySegment<byte>> segments) {
      if (this.TrySend(segments))
        return;
      do {
        Thread.SpinWait(1);
      } while (!this.TrySend(segments));
    }

    public abstract void Close();

    public event EventHandler Closed {
      add { this.m_Closed += value; }
      remove { this.m_Closed -= value; }
    }

    protected virtual void OnClosed() {
      this.IsConnected = false;
      EventHandler closed = this.m_Closed;
      if (closed == null)
        return;
      closed((object) this, EventArgs.Empty);
    }

    public event EventHandler<ErrorEventArgs> Error {
      add { this.m_Error += value; }
      remove { this.m_Error -= value; }
    }

    protected virtual void OnError(Exception e) {
      EventHandler<ErrorEventArgs> error = this.m_Error;
      if (error == null)
        return;
      error((object) this, new ErrorEventArgs(e));
    }

    public event EventHandler Connected {
      add { this.m_Connected += value; }
      remove { this.m_Connected -= value; }
    }

    protected virtual void OnConnected() {
      global::System.Net.Sockets.Socket client = this.Client;
      if (client != null && client.NoDelay != this.NoDeplay)
        client.NoDelay = this.NoDeplay;
      this.IsConnected = true;
      EventHandler connected = this.m_Connected;
      if (connected == null)
        return;
      connected((object) this, EventArgs.Empty);
    }

    public event EventHandler<DataEventArgs> DataReceived {
      add { this.m_DataReceived += value; }
      remove { this.m_DataReceived -= value; }
    }

    protected virtual void OnDataReceived(byte[] data, int offset, int length) {
      EventHandler<DataEventArgs> dataReceived = this.m_DataReceived;
      if (dataReceived == null)
        return;
      this.m_DataArgs.Data = data;
      this.m_DataArgs.Offset = offset;
      this.m_DataArgs.Length = length;
      dataReceived((object) this, this.m_DataArgs);
    }

    public virtual int ReceiveBufferSize { get; set; }

    public IProxyConnector Proxy { get; set; }

    protected ArraySegment<byte> Buffer { get; set; }

    void IBufferSetter.SetBuffer(ArraySegment<byte> bufferSegment) {
      this.SetBuffer(bufferSegment);
    }

    protected virtual void SetBuffer(ArraySegment<byte> bufferSegment) {
      this.Buffer = bufferSegment;
    }
  }
}