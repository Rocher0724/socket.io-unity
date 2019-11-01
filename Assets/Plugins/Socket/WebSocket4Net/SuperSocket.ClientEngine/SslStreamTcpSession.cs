using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public class SslStreamTcpSession : TcpClientSession {
    private SslStream m_SslStream;

    public bool AllowUnstrustedCertificate { get; set; }

    public SslStreamTcpSession(EndPoint remoteEndPoint)
      : base(remoteEndPoint) {
    }

    public SslStreamTcpSession(EndPoint remoteEndPoint, int receiveBufferSize)
      : base(remoteEndPoint, receiveBufferSize) {
    }

    protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e) {
      this.ProcessConnect(sender as global::System.Net.Sockets.Socket, (object) null, e);
    }

    protected override void OnGetSocket(SocketAsyncEventArgs e) {
      try {
        SslStream sslStream = new SslStream((Stream) new NetworkStream(this.Client), false,
          new RemoteCertificateValidationCallback(this.ValidateRemoteCertificate));
        sslStream.BeginAuthenticateAsClient(this.HostName, new AsyncCallback(this.OnAuthenticated), (object) sslStream);
      } catch (Exception ex) {
        if (this.IsIgnorableException(ex))
          return;
        this.OnError(ex);
      }
    }

    private void OnAuthenticated(IAsyncResult result) {
      SslStream asyncState = result.AsyncState as SslStream;
      if (asyncState == null) {
        this.EnsureSocketClosed();
        this.OnError((Exception) new NullReferenceException("Ssl Stream is null OnAuthenticated"));
      } else {
        try {
          asyncState.EndAuthenticateAsClient(result);
        } catch (Exception ex) {
          this.EnsureSocketClosed();
          this.OnError(ex);
          return;
        }

        this.m_SslStream = asyncState;
        this.OnConnected();
        if (this.Buffer.Array == null)
          this.Buffer = new ArraySegment<byte>(new byte[this.ReceiveBufferSize], 0, this.ReceiveBufferSize);
        this.BeginRead();
      }
    }

    private void OnDataRead(IAsyncResult result) {
      SslStreamTcpSession.SslAsyncState asyncState = result.AsyncState as SslStreamTcpSession.SslAsyncState;
      if (asyncState == null || asyncState.SslStream == null) {
        this.OnError((Exception) new NullReferenceException("Null state or stream."));
      } else {
        SslStream sslStream = asyncState.SslStream;
        int length;
        try {
          length = sslStream.EndRead(result);
        } catch (Exception ex) {
          if (!this.IsIgnorableException(ex))
            this.OnError(ex);
          if (!this.EnsureSocketClosed(asyncState.Client))
            return;
          this.OnClosed();
          return;
        }

        if (length == 0) {
          if (!this.EnsureSocketClosed(asyncState.Client))
            return;
          this.OnClosed();
        } else {
          this.OnDataReceived(this.Buffer.Array, this.Buffer.Offset, length);
          this.BeginRead();
        }
      }
    }

    private void BeginRead() {
      global::System.Net.Sockets.Socket client = this.Client;
      if (client == null)
        return;
      if (this.m_SslStream == null)
        return;
      try {
        this.m_SslStream.BeginRead(this.Buffer.Array, this.Buffer.Offset, this.Buffer.Count,
          new AsyncCallback(this.OnDataRead), (object) new SslStreamTcpSession.SslAsyncState() {
            SslStream = this.m_SslStream,
            Client = client
          });
      } catch (Exception ex) {
        if (!this.IsIgnorableException(ex))
          this.OnError(ex);
        if (!this.EnsureSocketClosed(client))
          return;
        this.OnClosed();
      }
    }

    private bool ValidateRemoteCertificate(
      object sender,
      X509Certificate certificate,
      X509Chain chain,
      SslPolicyErrors sslPolicyErrors) {
      RemoteCertificateValidationCallback validationCallback = ServicePointManager.ServerCertificateValidationCallback;
      if (validationCallback != null)
        return validationCallback(sender, certificate, chain, sslPolicyErrors);
      if (sslPolicyErrors == SslPolicyErrors.None)
        return true;
      if (!this.AllowUnstrustedCertificate) {
        this.OnError(new Exception(sslPolicyErrors.ToString()));
        return false;
      }

      if (sslPolicyErrors != SslPolicyErrors.None && sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors) {
        this.OnError(new Exception(sslPolicyErrors.ToString()));
        return false;
      }

      if (chain != null && chain.ChainStatus != null) {
        foreach (X509ChainStatus chainStatu in chain.ChainStatus) {
          if ((!(certificate.Subject == certificate.Issuer) ||
               chainStatu.Status != X509ChainStatusFlags.UntrustedRoot) &&
              chainStatu.Status != X509ChainStatusFlags.NoError) {
            this.OnError(new Exception(sslPolicyErrors.ToString()));
            return false;
          }
        }
      }

      return true;
    }

    protected override bool IsIgnorableException(Exception e) {
      return base.IsIgnorableException(e) || e is IOException &&
             (e.InnerException is ObjectDisposedException || e.InnerException is IOException &&
              e.InnerException.InnerException is ObjectDisposedException);
    }

    protected override void SendInternal(PosList<ArraySegment<byte>> items) {
      global::System.Net.Sockets.Socket client = this.Client;
      try {
        ArraySegment<byte> arraySegment = items[items.Position];
        this.m_SslStream.BeginWrite(arraySegment.Array, arraySegment.Offset, arraySegment.Count,
          new AsyncCallback(this.OnWriteComplete), (object) new SslStreamTcpSession.SslAsyncState() {
            SslStream = this.m_SslStream,
            Client = client,
            SendingItems = items
          });
      } catch (Exception ex) {
        if (!this.IsIgnorableException(ex))
          this.OnError(ex);
        if (!this.EnsureSocketClosed(client))
          return;
        this.OnClosed();
      }
    }

    private void OnWriteComplete(IAsyncResult result) {
      SslStreamTcpSession.SslAsyncState asyncState = result.AsyncState as SslStreamTcpSession.SslAsyncState;
      if (asyncState == null || asyncState.SslStream == null) {
        this.OnError((Exception) new NullReferenceException("State of Ssl stream is null."));
      } else {
        SslStream sslStream = asyncState.SslStream;
        try {
          sslStream.EndWrite(result);
        } catch (Exception ex) {
          if (!this.IsIgnorableException(ex))
            this.OnError(ex);
          if (!this.EnsureSocketClosed(asyncState.Client))
            return;
          this.OnClosed();
          return;
        }

        PosList<ArraySegment<byte>> sendingItems = asyncState.SendingItems;
        int num = sendingItems.Position + 1;
        if (num < sendingItems.Count) {
          sendingItems.Position = num;
          this.SendInternal(sendingItems);
        } else {
          try {
            this.m_SslStream.Flush();
          } catch (Exception ex) {
            if (!this.IsIgnorableException(ex))
              this.OnError(ex);
            if (!this.EnsureSocketClosed(asyncState.Client))
              return;
            this.OnClosed();
            return;
          }

          this.OnSendingCompleted();
        }
      }
    }

    public override void Close() {
      SslStream sslStream = this.m_SslStream;
      if (sslStream != null) {
        sslStream.Close();
        sslStream.Dispose();
        this.m_SslStream = (SslStream) null;
      }

      base.Close();
    }

    private class SslAsyncState {
      public SslStream SslStream { get; set; }

      public global::System.Net.Sockets.Socket Client { get; set; }

      public PosList<ArraySegment<byte>> SendingItems { get; set; }
    }
  }
}