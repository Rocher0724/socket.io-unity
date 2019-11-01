using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Socket.Quobject.EngineIoClientDotNet.ComponentEmitter;
using Socket.Quobject.EngineIoClientDotNet.Modules;
using Socket.Quobject.EngineIoClientDotNet.Thread;
using UnityEngine;

namespace Socket.Quobject.EngineIoClientDotNet.Client.Transports {
  public class PollingXHR : Polling
  {
    private PollingXHR.XHRRequest sendXhr;

    public PollingXHR(Transport.Options options)
      : base(options)
    {
    }

    protected PollingXHR.XHRRequest Request()
    {
      return this.Request((PollingXHR.XHRRequest.RequestOptions) null);
    }

    protected PollingXHR.XHRRequest Request(PollingXHR.XHRRequest.RequestOptions opts)
    {
      if (opts == null)
        opts = new PollingXHR.XHRRequest.RequestOptions();
      opts.Uri = this.Uri();
      opts.ExtraHeaders = this.ExtraHeaders;
      PollingXHR.XHRRequest xhrRequest = new PollingXHR.XHRRequest(opts);
      xhrRequest.On(Transport.EVENT_REQUEST_HEADERS, (IListener) new PollingXHR.EventRequestHeadersListener(this)).On(Transport.EVENT_RESPONSE_HEADERS, (IListener) new PollingXHR.EventResponseHeadersListener(this));
      return xhrRequest;
    }

    protected override void DoWrite(byte[] data, ActionTrigger action)
    {
      PollingXHR.XHRRequest.RequestOptions opts = new PollingXHR.XHRRequest.RequestOptions()
      {
        Method = "POST",
        Data = data,
        CookieHeaderValue = this.Cookie
      };
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("DoWrite data = " + (object) data);
      this.sendXhr = this.Request(opts);
      this.sendXhr.On(Transport.EVENT_SUCCESS, (IListener) new PollingXHR.SendEventSuccessListener(action));
      this.sendXhr.On(Transport.EVENT_ERROR, (IListener) new PollingXHR.SendEventErrorListener(this));
      this.sendXhr.Create();
    }

    protected override void DoPoll()
    {
      LogManager.GetLogger(Global.CallerName("", 0, "")).Info("xhr DoPoll");
      this.sendXhr = this.Request(new PollingXHR.XHRRequest.RequestOptions()
      {
        CookieHeaderValue = this.Cookie
      });
      this.sendXhr.On(Transport.EVENT_DATA, (IListener) new PollingXHR.DoPollEventDataListener(this));
      this.sendXhr.On(Transport.EVENT_ERROR, (IListener) new PollingXHR.DoPollEventErrorListener(this));
      this.sendXhr.Create();
    }

    private class EventRequestHeadersListener : IListener, IComparable<IListener>
    {
      private PollingXHR pollingXHR;

      public EventRequestHeadersListener(PollingXHR pollingXHR)
      {
        this.pollingXHR = pollingXHR;
      }

      public void Call(params object[] args)
      {
        this.pollingXHR.Emit(Transport.EVENT_REQUEST_HEADERS, args[0]);
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

    private class EventResponseHeadersListener : IListener, IComparable<IListener>
    {
      private PollingXHR pollingXHR;

      public EventResponseHeadersListener(PollingXHR pollingXHR)
      {
        this.pollingXHR = pollingXHR;
      }

      public void Call(params object[] args)
      {
        this.pollingXHR.Emit(Transport.EVENT_RESPONSE_HEADERS, args[0]);
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

    private class SendEventErrorListener : IListener, IComparable<IListener>
    {
      private PollingXHR pollingXHR;

      public SendEventErrorListener(PollingXHR pollingXHR)
      {
        this.pollingXHR = pollingXHR;
      }

      public void Call(params object[] args)
      {
        this.pollingXHR.OnError("xhr post error", args.Length == 0 || !(args[0] is Exception) ? (Exception) null : (Exception) args[0]);
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

    private class SendEventSuccessListener : IListener, IComparable<IListener>
    {
      private ActionTrigger action;

      public SendEventSuccessListener(ActionTrigger action)
      {
        this.action = action;
      }

      public void Call(params object[] args)
      {
        this.action();
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

    private class DoPollEventDataListener : IListener, IComparable<IListener>
    {
      private PollingXHR pollingXHR;

      public DoPollEventDataListener(PollingXHR pollingXHR)
      {
        this.pollingXHR = pollingXHR;
      }

      public void Call(params object[] args)
      {
        object obj = args.Length != 0 ? args[0] : (object) null;
        if (obj is string)
        {
          this.pollingXHR.OnData((string) obj);
        }
        else
        {
          if (!(obj is byte[]))
            return;
          this.pollingXHR.OnData((byte[]) obj);
        }
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

    private class DoPollEventErrorListener : IListener, IComparable<IListener>
    {
      private PollingXHR pollingXHR;

      public DoPollEventErrorListener(PollingXHR pollingXHR)
      {
        this.pollingXHR = pollingXHR;
      }

      public void Call(params object[] args)
      {
        this.pollingXHR.OnError("xhr poll error", args.Length == 0 || !(args[0] is Exception) ? (Exception) null : (Exception) args[0]);
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

    public class XHRRequest : Emitter
    {
      private string Method;
      private string Uri;
      private byte[] Data;
      private string CookieHeaderValue;
      private HttpWebRequest Xhr;
      private Dictionary<string, string> ExtraHeaders;

      public XHRRequest(PollingXHR.XHRRequest.RequestOptions options)
      {
        this.Method = options.Method ?? "GET";
        this.Uri = options.Uri;
        this.Data = options.Data;
        this.CookieHeaderValue = options.CookieHeaderValue;
        this.ExtraHeaders = options.ExtraHeaders;
      }

      public void Create()
      {
        LogManager log = LogManager.GetLogger(Global.CallerName("", 0, ""));
        try
        {
          log.Info(string.Format("xhr open {0}: {1}", (object) this.Method, (object) this.Uri));
          this.Xhr = (HttpWebRequest) WebRequest.Create(this.Uri);
          this.Xhr.Method = this.Method;
          if (this.CookieHeaderValue != null)
            this.Xhr.Headers.Add("Cookie", this.CookieHeaderValue);
          if (this.ExtraHeaders != null)
          {
            foreach (KeyValuePair<string, string> extraHeader in this.ExtraHeaders)
              this.Xhr.Headers.Add(extraHeader.Key, extraHeader.Value);
          }
        }
        catch (Exception ex)
        {
          log.Error(ex);
          this.OnError(ex);
          return;
        }
        if (this.Method == "POST")
          this.Xhr.ContentType = "application/octet-stream";
        try
        {
          if (this.Data != null)
          {
            this.Xhr.ContentLength = (long) this.Data.Length;
            using (Stream requestStream = this.Xhr.GetRequestStream())
              requestStream.Write(this.Data, 0, this.Data.Length);
          }
          EasyTimer.TaskRun((ActionTrigger) (() =>
          {
            LogManager logger = LogManager.GetLogger(Global.CallerName("", 0, ""));
            logger.Info("Task.Run Create start");
            using (WebResponse response = this.Xhr.GetResponse())
            {
              log.Info("Xhr.GetResponse ");
              Dictionary<string, string> headers = new Dictionary<string, string>();
              for (int index = 0; index < response.Headers.Count; ++index)
                headers.Add(response.Headers.Keys[index], response.Headers[index]);
              this.OnResponseHeaders(headers);
              string header = response.Headers["Content-Type"];
              using (Stream responseStream = response.GetResponseStream())
              {
                Debug.Assert(responseStream != null, "resStream != null");
                if (header.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
                {
                  byte[] buffer = new byte[16384];
                  using (MemoryStream memoryStream = new MemoryStream())
                  {
                    int count;
                    while ((count = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                      memoryStream.Write(buffer, 0, count);
                    this.OnData(memoryStream.ToArray());
                  }
                }
                else
                {
                  using (StreamReader streamReader = new StreamReader(responseStream))
                    this.OnData(streamReader.ReadToEnd());
                }
              }
            }
            logger.Info("Task.Run Create finish");
          }));
        }
        catch (IOException ex)
        {
          log.Error("Create call failed", (Exception) ex);
          this.OnError((Exception) ex);
        }
        catch (WebException ex)
        {
          log.Error("Create call failed", (Exception) ex);
          this.OnError((Exception) ex);
        }
        catch (Exception ex)
        {
          log.Error("Create call failed", ex);
          this.OnError(ex);
        }
      }

      private void OnSuccess()
      {
        this.Emit(Transport.EVENT_SUCCESS);
      }

      private void OnData(string data)
      {
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info("OnData string = " + data);
        this.Emit(Transport.EVENT_DATA, (object) data);
        this.OnSuccess();
      }

      private void OnData(byte[] data)
      {
        LogManager.GetLogger(Global.CallerName("", 0, "")).Info("OnData byte[] =" + Encoding.UTF8.GetString(data));
        this.Emit(Transport.EVENT_DATA, (object) data);
        this.OnSuccess();
      }

      private void OnError(Exception err)
      {
        this.Emit(Transport.EVENT_ERROR, (object) err);
      }

      private void OnRequestHeaders(Dictionary<string, string> headers)
      {
        this.Emit(Transport.EVENT_REQUEST_HEADERS, (object) headers);
      }

      private void OnResponseHeaders(Dictionary<string, string> headers)
      {
        this.Emit(Transport.EVENT_RESPONSE_HEADERS, (object) headers);
      }

      public class RequestOptions
      {
        public string Uri;
        public string Method;
        public byte[] Data;
        public string CookieHeaderValue;
        public Dictionary<string, string> ExtraHeaders;
      }
    }
  }
}
