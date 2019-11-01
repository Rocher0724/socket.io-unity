using System;

namespace Socket.Quobject.SocketIoClientDotNet.Client {
  public class Url {
    private Url() {
    }

    public static Uri Parse(string uri) {
      if (uri.StartsWith("//"))
        uri = "http:" + uri;
      return new Uri(uri);
    }

    public static string ExtractId(string url) {
      return Url.ExtractId(new Uri(url));
    }

    public static string ExtractId(Uri uri) {
      string scheme = uri.Scheme;
      int num = uri.Port;
      if (num == -1) {
        if (uri.Scheme.StartsWith("https"))
          num = 443;
        else if (uri.Scheme.StartsWith("http"))
          num = 80;
      }

      return string.Format("{0}://{1}:{2}", (object) scheme, (object) uri.Host, (object) num);
    }
  }
}