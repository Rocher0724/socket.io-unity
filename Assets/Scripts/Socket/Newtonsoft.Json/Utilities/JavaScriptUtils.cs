using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class JavaScriptUtils {
    internal static readonly bool[] SingleQuoteCharEscapeFlags = new bool[128];
    internal static readonly bool[] DoubleQuoteCharEscapeFlags = new bool[128];
    internal static readonly bool[] HtmlCharEscapeFlags = new bool[128];
    private const int UnicodeTextLength = 6;
    private const string EscapedUnicodeText = "!";

    static JavaScriptUtils() {
      IList<char> first = (IList<char>) new List<char>() {
        '\n',
        '\r',
        '\t',
        '\\',
        '\f',
        '\b'
      };
      for (int index = 0; index < 32; ++index)
        first.Add((char) index);
      foreach (char ch in first.Union<char>((IEnumerable<char>) new char[1] {
        '\''
      }))
        JavaScriptUtils.SingleQuoteCharEscapeFlags[(int) ch] = true;
      foreach (char ch in first.Union<char>((IEnumerable<char>) new char[1] {
        '"'
      }))
        JavaScriptUtils.DoubleQuoteCharEscapeFlags[(int) ch] = true;
      foreach (char ch in first.Union<char>((IEnumerable<char>) new char[5] {
        '"',
        '\'',
        '<',
        '>',
        '&'
      }))
        JavaScriptUtils.HtmlCharEscapeFlags[(int) ch] = true;
    }

    public static bool[] GetCharEscapeFlags(
      StringEscapeHandling stringEscapeHandling,
      char quoteChar) {
      if (stringEscapeHandling == StringEscapeHandling.EscapeHtml)
        return JavaScriptUtils.HtmlCharEscapeFlags;
      if (quoteChar == '"')
        return JavaScriptUtils.DoubleQuoteCharEscapeFlags;
      return JavaScriptUtils.SingleQuoteCharEscapeFlags;
    }

    public static bool ShouldEscapeJavaScriptString(string s, bool[] charEscapeFlags) {
      if (s == null)
        return false;
      foreach (char ch in s) {
        if ((int) ch >= charEscapeFlags.Length || charEscapeFlags[(int) ch])
          return true;
      }

      return false;
    }

    public static void WriteEscapedJavaScriptString(
      TextWriter writer,
      string s,
      char delimiter,
      bool appendDelimiters,
      bool[] charEscapeFlags,
      StringEscapeHandling stringEscapeHandling,
      IArrayPool<char> bufferPool,
      ref char[] writeBuffer) {
      if (appendDelimiters)
        writer.Write(delimiter);
      if (!string.IsNullOrEmpty(s)) {
        int num1 = JavaScriptUtils.FirstCharToEscape(s, charEscapeFlags, stringEscapeHandling);
        switch (num1) {
          case -1:
            writer.Write(s);
            break;
          case 0:
            for (int index = num1; index < s.Length; ++index) {
              char c = s[index];
              if ((int) c >= charEscapeFlags.Length || charEscapeFlags[(int) c]) {
                string a;
                switch (c) {
                  case '\b':
                    a = "\\b";
                    break;
                  case '\t':
                    a = "\\t";
                    break;
                  case '\n':
                    a = "\\n";
                    break;
                  case '\f':
                    a = "\\f";
                    break;
                  case '\r':
                    a = "\\r";
                    break;
                  case '\\':
                    a = "\\\\";
                    break;
                  case '\x0085':
                    a = "\\u0085";
                    break;
                  case '\x2028':
                    a = "\\u2028";
                    break;
                  case '\x2029':
                    a = "\\u2029";
                    break;
                  default:
                    if ((int) c < charEscapeFlags.Length ||
                        stringEscapeHandling == StringEscapeHandling.EscapeNonAscii) {
                      if (c == '\'' && stringEscapeHandling != StringEscapeHandling.EscapeHtml) {
                        a = "\\'";
                        break;
                      }

                      if (c == '"' && stringEscapeHandling != StringEscapeHandling.EscapeHtml) {
                        a = "\\\"";
                        break;
                      }

                      if (writeBuffer == null || writeBuffer.Length < 6)
                        writeBuffer = BufferUtils.EnsureBufferSize(bufferPool, 6, writeBuffer);
                      StringUtils.ToCharAsUnicode(c, writeBuffer);
                      a = "!";
                      break;
                    }

                    a = (string) null;
                    break;
                }

                if (a != null) {
                  bool flag = string.Equals(a, "!");
                  if (index > num1) {
                    int minSize = index - num1 + (flag ? 6 : 0);
                    int num2 = flag ? 6 : 0;
                    if (writeBuffer == null || writeBuffer.Length < minSize) {
                      char[] chArray = BufferUtils.RentBuffer(bufferPool, minSize);
                      if (flag)
                        Array.Copy((Array) writeBuffer, (Array) chArray, 6);
                      BufferUtils.ReturnBuffer(bufferPool, writeBuffer);
                      writeBuffer = chArray;
                    }

                    s.CopyTo(num1, writeBuffer, num2, minSize - num2);
                    writer.Write(writeBuffer, num2, minSize - num2);
                  }

                  num1 = index + 1;
                  if (!flag)
                    writer.Write(a);
                  else
                    writer.Write(writeBuffer, 0, 6);
                }
              }
            }

            int num3 = s.Length - num1;
            if (num3 > 0) {
              if (writeBuffer == null || writeBuffer.Length < num3)
                writeBuffer = BufferUtils.EnsureBufferSize(bufferPool, num3, writeBuffer);
              s.CopyTo(num1, writeBuffer, 0, num3);
              writer.Write(writeBuffer, 0, num3);
              break;
            }

            break;
          default:
            if (writeBuffer == null || writeBuffer.Length < num1)
              writeBuffer = BufferUtils.EnsureBufferSize(bufferPool, num1, writeBuffer);
            s.CopyTo(0, writeBuffer, 0, num1);
            writer.Write(writeBuffer, 0, num1);
            goto case 0;
        }
      }

      if (!appendDelimiters)
        return;
      writer.Write(delimiter);
    }

    public static string ToEscapedJavaScriptString(
      string value,
      char delimiter,
      bool appendDelimiters,
      StringEscapeHandling stringEscapeHandling) {
      bool[] charEscapeFlags = JavaScriptUtils.GetCharEscapeFlags(stringEscapeHandling, delimiter);
      using (StringWriter stringWriter = StringUtils.CreateStringWriter(value != null ? value.Length : 16)) {
        char[] writeBuffer = (char[]) null;
        JavaScriptUtils.WriteEscapedJavaScriptString((TextWriter) stringWriter, value, delimiter, appendDelimiters,
          charEscapeFlags, stringEscapeHandling, (IArrayPool<char>) null, ref writeBuffer);
        return stringWriter.ToString();
      }
    }

    private static int FirstCharToEscape(
      string s,
      bool[] charEscapeFlags,
      StringEscapeHandling stringEscapeHandling) {
      for (int index = 0; index != s.Length; ++index) {
        char ch = s[index];
        if ((int) ch < charEscapeFlags.Length) {
          if (charEscapeFlags[(int) ch])
            return index;
        } else if (stringEscapeHandling == StringEscapeHandling.EscapeNonAscii || ch == '\x0085' ||
                   (ch == '\x2028' || ch == '\x2029'))
          return index;
      }

      return -1;
    }
  }
}