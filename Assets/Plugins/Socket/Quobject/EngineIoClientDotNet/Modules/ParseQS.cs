using System.Collections.Generic;
using System.Text;
using Socket.Quobject.Collections.Immutable;

namespace Socket.Quobject.EngineIoClientDotNet.Modules {
  public class ParseQS {
    public static string Encode(ImmutableDictionary<string, string> obj) {
      StringBuilder stringBuilder = new StringBuilder();
      foreach (string str in new List<string>(obj.Keys)) {
        if (stringBuilder.Length > 0)
          stringBuilder.Append("&");
        stringBuilder.Append(Global.EncodeURIComponent(str));
        stringBuilder.Append("=");
        stringBuilder.Append(Global.EncodeURIComponent(obj[str]));
      }

      return stringBuilder.ToString();
    }

    internal static string Encode(Dictionary<string, string> obj) {
      StringBuilder stringBuilder = new StringBuilder();
      foreach (string key in obj.Keys) {
        if (stringBuilder.Length > 0)
          stringBuilder.Append("&");
        stringBuilder.Append(Global.EncodeURIComponent(key));
        stringBuilder.Append("=");
        stringBuilder.Append(Global.EncodeURIComponent(obj[key]));
      }

      return stringBuilder.ToString();
    }

    public static Dictionary<string, string> Decode(string qs) {
      Dictionary<string, string> dictionary = new Dictionary<string, string>();
      string str1 = qs;
      char[] chArray1 = new char[1] {'&'};
      foreach (string str2 in str1.Split(chArray1)) {
        char[] chArray2 = new char[1] {'='};
        string[] strArray = str2.Split(chArray2);
        dictionary.Add(Global.DecodeURIComponent(strArray[0]), Global.DecodeURIComponent(strArray[1]));
      }

      return dictionary;
    }
  }
}