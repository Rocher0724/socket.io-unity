using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Socket.WebSocket4Net.System.Linq;

namespace Socket.WebSocket4Net.Default {
  public static class Extensions {
    private static readonly char[] m_CrCf = new char[2] {
      '\r',
      '\n'
    };

    private static Type[] m_SimpleTypes = new Type[6] {
      typeof (string),
      typeof (Decimal),
      typeof (DateTime),
      typeof (DateTimeOffset),
      typeof (TimeSpan),
      typeof (Guid)
    };

    private const string m_Tab = "\t";
    private const char m_Colon = ':';
    private const string m_Space = " ";
    private const string m_ValueSeparator = ", ";

    public static void AppendFormatWithCrCf (this StringBuilder builder, string format, object arg) {
      builder.AppendFormat (format, arg);
      builder.Append (Extensions.m_CrCf);
    }

    public static void AppendFormatWithCrCf (
      this StringBuilder builder,
      string format,
      params object[] args) {
      builder.AppendFormat (format, args);
      builder.Append (Extensions.m_CrCf);
    }

    public static void AppendWithCrCf (this StringBuilder builder, string content) {
      builder.Append (content);
      builder.Append (Extensions.m_CrCf);
    }

    public static void AppendWithCrCf (this StringBuilder builder) {
      builder.Append (Extensions.m_CrCf);
    }

    public static bool ParseMimeHeader (
      this string source,
      IDictionary<string, object> valueContainer,
      out string verbLine) {
      verbLine = string.Empty;
      IDictionary<string, object> dictionary = valueContainer;
      string key1 = string.Empty;
      StringReader stringReader = new StringReader (source);
      string str1;
      object obj;
      while (!string.IsNullOrEmpty (str1 = stringReader.ReadLine ())) {
        if (string.IsNullOrEmpty (verbLine))
          verbLine = str1;
        else if (str1.StartsWith ("\t") && !string.IsNullOrEmpty (key1)) {
          if (!dictionary.TryGetValue (key1, out obj))
            return false;
          dictionary[key1] = (object) (obj.ToString () + str1.Trim ());
        } else {
          int length = str1.IndexOf (':');
          if (length >= 0) {
            string key2 = str1.Substring (0, length);
            if (!string.IsNullOrEmpty (key2))
              key2 = key2.Trim ();
            string str2 = str1.Substring (length + 1);
            if (!string.IsNullOrEmpty (str2) && str2.StartsWith (" ") && str2.Length > 1)
              str2 = str2.Substring (1);
            if (!string.IsNullOrEmpty (key2)) {
              if (!dictionary.TryGetValue (key2, out obj))
                dictionary.Add (key2, (object) str2);
              else
                dictionary[key2] = (object) (obj.ToString () + ", " + str2);
              key1 = key2;
            }
          }
        }
      }

      return true;
    }

    public static TValue GetValue<TValue> (
      this IDictionary<string, object> valueContainer,
      string name) {
      TValue defaultValue = default (TValue);
      return valueContainer.GetValue<TValue> (name, defaultValue);
    }

    public static TValue GetValue<TValue> (
      this IDictionary<string, object> valueContainer,
      string name,
      TValue defaultValue) {
      object obj;
      if (!valueContainer.TryGetValue (name, out obj))
        return defaultValue;
      return (TValue) obj;
    }

    internal static bool IsSimpleType (this Type type) {
      if (!type.IsValueType &&
          !type.IsPrimitive &&
          !((IEnumerable<Type>) Extensions.m_SimpleTypes).Contains<Type> (type))
        return Convert.GetTypeCode ((object) type) != TypeCode.Object;
      return true;
    }

    public static string GetOrigin (this Uri uri) {
      return uri.GetLeftPart (UriPartial.Authority);
    }

    public static byte[] ComputeMD5Hash (this byte[] source) {
      return MD5.Create ().ComputeHash (source);
    }

    public static string CalculateChallenge (this string source) {
      return Convert.ToBase64String (SHA1.Create ().ComputeHash (Encoding.ASCII.GetBytes (source)));
    }
  }
}