using System;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class StringReferenceExtensions {
    public static int IndexOf(this StringReference s, char c, int startIndex, int length) {
      int num = Array.IndexOf<char>(s.Chars, c, s.StartIndex + startIndex, length);
      if (num == -1)
        return -1;
      return num - s.StartIndex;
    }

    public static bool StartsWith(this StringReference s, string text) {
      if (text.Length > s.Length)
        return false;
      char[] chars = s.Chars;
      for (int index = 0; index < text.Length; ++index) {
        if ((int) text[index] != (int) chars[index + s.StartIndex])
          return false;
      }

      return true;
    }

    public static bool EndsWith(this StringReference s, string text) {
      if (text.Length > s.Length)
        return false;
      char[] chars = s.Chars;
      int num = s.StartIndex + s.Length - text.Length;
      for (int index = 0; index < text.Length; ++index) {
        if ((int) text[index] != (int) chars[index + num])
          return false;
      }

      return true;
    }
  }
}