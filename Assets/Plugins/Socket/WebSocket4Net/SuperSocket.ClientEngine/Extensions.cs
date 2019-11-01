using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Socket.WebSocket4Net.CompilerServices;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public static class Extensions {
    private static Random m_Random = new Random();

    [Extension]
    public static int IndexOf<T>(IList<T> source, T target, int pos, int length) where T : IEquatable<T> {
      for (int index = pos; index < pos + length; ++index) {
        if (source[index].Equals(target))
          return index;
      }

      return -1;
    }

    [Extension]
    public static int? SearchMark<T>(IList<T> source, T[] mark) where T : IEquatable<T> {
      return Extensions.SearchMark<T>(source, 0, source.Count, mark, 0);
    }

    [Extension]
    public static int? SearchMark<T>(IList<T> source, int offset, int length, T[] mark) where T : IEquatable<T> {
      return Extensions.SearchMark<T>(source, offset, length, mark, 0);
    }

    [Extension]
    public static int? SearchMark<T>(
      IList<T> source,
      int offset,
      int length,
      T[] mark,
      int matched)
      where T : IEquatable<T> {
      int pos = offset;
      int num1 = offset + length - 1;
      int index1 = matched;
      if (matched > 0) {
        for (int index2 = index1; index2 < mark.Length && source[pos++].Equals(mark[index2]); ++index2) {
          ++index1;
          if (pos > num1) {
            if (index1 == mark.Length)
              return new int?(offset);
            return new int?(-index1);
          }
        }

        if (index1 == mark.Length)
          return new int?(offset);
        pos = offset;
        index1 = 0;
      }

      int num2;
      while (true) {
        num2 = Extensions.IndexOf<T>(source, mark[index1], pos, length - pos + offset);
        if (num2 >= 0) {
          int num3 = index1 + 1;
          for (int index2 = num3; index2 < mark.Length; ++index2) {
            int index3 = num2 + index2;
            if (index3 > num1)
              return new int?(-num3);
            if (source[index3].Equals(mark[index2]))
              ++num3;
            else
              break;
          }

          if (num3 != mark.Length) {
            pos = num2 + 1;
            index1 = 0;
          } else
            goto label_20;
        } else
          break;
      }

      return new int?();
      label_20:
      return new int?(num2);
    }

    [Extension]
    public static int SearchMark<T>(
      IList<T> source,
      int offset,
      int length,
      SearchMarkState<T> searchState)
      where T : IEquatable<T> {
      int? nullable = Extensions.SearchMark<T>(source, offset, length, searchState.Mark, searchState.Matched);
      if (!nullable.HasValue) {
        searchState.Matched = 0;
        return -1;
      }

      if (nullable.Value < 0) {
        searchState.Matched = -nullable.Value;
        return -1;
      }

      searchState.Matched = 0;
      return nullable.Value;
    }

    [Extension]
    public static int StartsWith<T>(IList<T> source, T[] mark) where T : IEquatable<T> {
      return Extensions.StartsWith<T>(source, 0, source.Count, mark);
    }

    [Extension]
    public static int StartsWith<T>(IList<T> source, int offset, int length, T[] mark) where T : IEquatable<T> {
      int num1 = offset;
      int num2 = offset + length - 1;
      for (int index1 = 0; index1 < mark.Length; ++index1) {
        int index2 = num1 + index1;
        if (index2 > num2)
          return index1;
        if (!source[index2].Equals(mark[index1]))
          return -1;
      }

      return mark.Length;
    }

    [Extension]
    public static bool EndsWith<T>(IList<T> source, T[] mark) where T : IEquatable<T> {
      return Extensions.EndsWith<T>(source, 0, source.Count, mark);
    }

    [Extension]
    public static bool EndsWith<T>(IList<T> source, int offset, int length, T[] mark) where T : IEquatable<T> {
      if (mark.Length > length)
        return false;
      for (int index = 0; index < Math.Min(length, mark.Length); ++index) {
        if (!mark[index].Equals(source[offset + length - mark.Length + index]))
          return false;
      }

      return true;
    }

    [Extension]
    public static T[] CloneRange<T>(T[] source, int offset, int length) {
      T[] objArray = new T[length];
      Array.Copy((Array) source, offset, (Array) objArray, 0, length);
      return objArray;
    }

    [Extension]
    public static T[] RandomOrder<T>(T[] source) {
      int num = source.Length / 2;
      for (int index1 = 0; index1 < num; ++index1) {
        int index2 = Extensions.m_Random.Next(0, source.Length - 1);
        int index3 = Extensions.m_Random.Next(0, source.Length - 1);
        if (index2 != index3) {
          T obj = source[index3];
          source[index3] = source[index2];
          source[index2] = obj;
        }
      }

      return source;
    }

    // 이 extension 이 내부에 별거없는 클래스인데 무슨차이일까?
    [Extension]
    public static string GetValue(NameValueCollection collection, string key) {
      return Extensions.GetValue(collection, key, string.Empty);
    }

    [Extension]
    public static string GetValue(NameValueCollection collection, string key, string defaultValue) {
      if (string.IsNullOrEmpty(key))
        throw new ArgumentNullException(nameof(key));
      if (collection == null)
        return defaultValue;
      return collection[key] ?? defaultValue;
    }
  }
}