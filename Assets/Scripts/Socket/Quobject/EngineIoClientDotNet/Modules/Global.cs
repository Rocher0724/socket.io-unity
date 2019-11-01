using System;
using System.Text.RegularExpressions;

namespace Socket.Quobject.EngineIoClientDotNet.Modules {
  public static class Global
  {
    public static string EncodeURIComponent(string str)
    {
      return Uri.EscapeDataString(str);
    }

    public static string DecodeURIComponent(string str)
    {
      return Uri.UnescapeDataString(str);
    }

    public static string CallerName(string caller = "", int number = 0, string path = "")
    {
      string[] strArray = path.Split('\\');
      string str = strArray.Length != 0 ? strArray[strArray.Length - 1] : "";
      if (path.Contains("SocketIoClientDotNet.Tests"))
        path = "SocketIoClientDotNet.Tests";
      else if (path.Contains("SocketIoClientDotNet"))
        path = "SocketIoClientDotNet";
      else if (path.Contains("EngineIoClientDotNet"))
        path = "EngineIoClientDotNet";
      return string.Format("{0}-{1}:{2}#{3}", (object) path, (object) str, (object) caller, (object) number);
    }

    public static string StripInvalidUnicodeCharacters(string str)
    {
      return new Regex("([\xD800-\xDBFF](?![\xDC00-\xDFFF]))|((?<![\xD800-\xDBFF])[\xDC00-\xDFFF])").Replace(str, "");
    }
  }
}
