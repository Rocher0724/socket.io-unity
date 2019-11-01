using System;
using System.Collections.Generic;
using Socket.WebSocket4Net.System.Linq;

namespace Socket.WebSocket4Net.System.Linq {
  public static class LINQ {
    public static int Count<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate) {
      int num = 0;
      foreach (TSource source1 in source) {
        if (predicate(source1))
          ++num;
      }

      return num;
    }

    public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource target) {
      foreach (TSource source1 in source) {
        if (source1.Equals((object) target))
          return true;
      }

      return false;
    }

    public static TSource FirstOrDefault<TSource>(
      this IEnumerable<TSource> source,
      Predicate<TSource> predicate) {
      foreach (TSource source1 in source) {
        if (predicate(source1))
          return source1;
      }

      return default(TSource);
    }

    public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> getter) {
      int num = 0;
      foreach (TSource t in source)
        num += getter(t);
      return num;
    }

    public static IEnumerable<TSource> OrderByDescending<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> getter)
      where TKey : IComparable<TKey> {
      List<TSource> sourceList = new List<TSource>();
      foreach (TSource source1 in source)
        sourceList.Add(source1);
      sourceList.Sort((IComparer<TSource>) new DelegateComparer<TSource, TKey>(getter));
      return (IEnumerable<TSource>) sourceList;
    }

    public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source) {
      if (source is TSource[])
        return source as TSource[];
      List<TSource> sourceList = new List<TSource>();
      foreach (TSource source1 in source)
        sourceList.Add(source1);
      return sourceList.ToArray();
    }
  }
}