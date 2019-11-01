

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public class AsyncTcpSession : TcpClientSession {
    private SocketAsyncEventArgs m_SocketEventArgs;
    private SocketAsyncEventArgs m_SocketEventArgsSend;

    public AsyncTcpSession(EndPoint remoteEndPoint)
      : base(remoteEndPoint) {
    }

    public AsyncTcpSession(EndPoint remoteEndPoint, int receiveBufferSize)
      : base(remoteEndPoint, receiveBufferSize) {
    }

    protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e) {
      if (e.LastOperation == SocketAsyncOperation.Connect)
        this.ProcessConnect(sender as global::System.Net.Sockets.Socket, (object) null, e);
      else
        this.ProcessReceive(e);
    }

    protected override void SetBuffer(ArraySegment<byte> bufferSegment) {
      base.SetBuffer(bufferSegment);
      if (this.m_SocketEventArgs == null)
        return;
      this.m_SocketEventArgs.SetBuffer(bufferSegment.Array, bufferSegment.Offset, bufferSegment.Count);
    }

    protected override void OnGetSocket(SocketAsyncEventArgs e) {
      if (this.Buffer.Array == null)
        this.Buffer = new ArraySegment<byte>(new byte[this.ReceiveBufferSize], 0, this.ReceiveBufferSize);
      e.SetBuffer(this.Buffer.Array, this.Buffer.Offset, this.Buffer.Count);
      this.m_SocketEventArgs = e;
      this.OnConnected();
      this.StartReceive();
    }

    private void BeginReceive() {
      if (this.Client.ReceiveAsync(this.m_SocketEventArgs))
        return;
      this.ProcessReceive(this.m_SocketEventArgs);
    }

    private void ProcessReceive(SocketAsyncEventArgs e) {
      if (e.SocketError != SocketError.Success) {
        if (this.EnsureSocketClosed())
          this.OnClosed();
        if (this.IsIgnorableSocketError((int) e.SocketError))
          return;
        this.OnError((Exception) new SocketException((int) e.SocketError));
      } else if (e.BytesTransferred == 0) {
        if (!this.EnsureSocketClosed())
          return;
        this.OnClosed();
      } else {
        this.OnDataReceived(e.Buffer, e.Offset, e.BytesTransferred);
        this.StartReceive();
      }
    }

    private void StartReceive() {
      global::System.Net.Sockets.Socket client = this.Client;
      if (client == null)
        return;
      bool async;
      try {
        async = client.ReceiveAsync(this.m_SocketEventArgs);
      } catch (SocketException ex) {
        if (!this.IsIgnorableSocketError(ex.ErrorCode))
          this.OnError((Exception) ex);
        if (!this.EnsureSocketClosed(client))
          return;
        this.OnClosed();
        return;
      } catch (Exception ex) {
        if (!this.IsIgnorableException(ex))
          this.OnError(ex);
        if (!this.EnsureSocketClosed(client))
          return;
        this.OnClosed();
        return;
      }

      if (async)
        return;
      this.ProcessReceive(this.m_SocketEventArgs);
    }

    protected override void SendInternal(PosList<ArraySegment<byte>> items) {
      if (this.m_SocketEventArgsSend == null) {
        this.m_SocketEventArgsSend = new SocketAsyncEventArgs();
        this.m_SocketEventArgsSend.Completed += new EventHandler<SocketAsyncEventArgs>(this.Sending_Completed);
      }

      bool flag;
      try {
        if (items.Count > 1) {
          if (this.m_SocketEventArgsSend.Buffer != null)
            this.m_SocketEventArgsSend.SetBuffer((byte[]) null, 0, 0);
          this.m_SocketEventArgsSend.BufferList = (IList<ArraySegment<byte>>) items;
        } else {
          ArraySegment<byte> arraySegment = items[0];
          try {
            if (this.m_SocketEventArgsSend.BufferList != null)
              this.m_SocketEventArgsSend.BufferList = (IList<ArraySegment<byte>>) null;
          } catch {
          }

          this.m_SocketEventArgsSend.SetBuffer(arraySegment.Array, 0, arraySegment.Count);
        }

        flag = this.Client.SendAsync(this.m_SocketEventArgsSend);
      } catch (SocketException ex) {
        int errorCode = ex.ErrorCode;
        if (!this.EnsureSocketClosed() || this.IsIgnorableSocketError(errorCode))
          return;
        this.OnError((Exception) ex);
        return;
      } catch (Exception ex) {
        if (!this.EnsureSocketClosed() || !this.IsIgnorableException(ex))
          return;
        this.OnError(ex);
        return;
      }

      if (flag)
        return;
      this.Sending_Completed((object) this.Client, this.m_SocketEventArgsSend);
    }

    private void Sending_Completed(object sender, SocketAsyncEventArgs e) {
      if (e.SocketError != SocketError.Success || e.BytesTransferred == 0) {
        if (this.EnsureSocketClosed())
          this.OnClosed();
        if (e.SocketError == SocketError.Success || this.IsIgnorableSocketError((int) e.SocketError))
          return;
        this.OnError((Exception) new SocketException((int) e.SocketError));
      } else
        this.OnSendingCompleted();
    }

    protected override void OnClosed() {
      if (this.m_SocketEventArgsSend != null) {
        this.m_SocketEventArgsSend.Dispose();
        this.m_SocketEventArgsSend = (SocketAsyncEventArgs) null;
      }

      if (this.m_SocketEventArgs != null) {
        this.m_SocketEventArgs.Dispose();
        this.m_SocketEventArgs = (SocketAsyncEventArgs) null;
      }

      base.OnClosed();
    }
  }
}