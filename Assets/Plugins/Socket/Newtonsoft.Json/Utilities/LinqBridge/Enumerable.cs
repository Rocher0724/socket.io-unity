using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Socket.Newtonsoft.Json.Utilities.LinqBridge {
  internal static class Enumerable {
    public static IEnumerable<TSource> AsEnumerable<TSource>(IEnumerable<TSource> source) {
      return source;
    }

    public static IEnumerable<TResult> Empty<TResult>() {
      return Enumerable.Sequence<TResult>.Empty;
    }

    public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source) {
      Enumerable.CheckNotNull<IEnumerable>(source, nameof(source));
      IEnumerable<TResult> results = source as IEnumerable<TResult>;
      if (results != null && (!(results is TResult[]) || results.GetType().GetElementType() == typeof(TResult)))
        return results;
      return Enumerable.CastYield<TResult>(source);
    }

    private static IEnumerable<TResult> CastYield<TResult>(IEnumerable source) {
      foreach (TResult result in source)
        yield return result;
    }

    public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source) {
      Enumerable.CheckNotNull<IEnumerable>(source, nameof(source));
      return Enumerable.OfTypeYield<TResult>(source);
    }

    private static IEnumerable<TResult> OfTypeYield<TResult>(IEnumerable source) {
      foreach (object obj in source) {
        if (obj is TResult)
          yield return (TResult) obj;
      }
    }

    public static IEnumerable<int> Range(int start, int count) {
      if (count < 0)
        throw new ArgumentOutOfRangeException(nameof(count), (object) count, (string) null);
      long end = (long) start + (long) count;
      if (end - 1L >= (long) int.MaxValue)
        throw new ArgumentOutOfRangeException(nameof(count), (object) count, (string) null);
      return Enumerable.RangeYield(start, end);
    }

    private static IEnumerable<int> RangeYield(int start, long end) {
      for (int i = start; (long) i < end; ++i)
        yield return i;
    }

    public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count) {
      if (count < 0)
        throw new ArgumentOutOfRangeException(nameof(count), (object) count, (string) null);
      return Enumerable.RepeatYield<TResult>(element, count);
    }

    private static IEnumerable<TResult> RepeatYield<TResult>(TResult element, int count) {
      for (int i = 0; i < count; ++i)
        yield return element;
    }

    public static IEnumerable<TSource> Where<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, bool>>(predicate, nameof(predicate));
      return Enumerable.WhereYield<TSource>(source, predicate);
    }

    private static IEnumerable<TSource> WhereYield<TSource>(
      IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      foreach (TSource a in source) {
        if (predicate(a))
          yield return a;
      }
    }

    public static IEnumerable<TSource> Where<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, int, bool> predicate) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, int, bool>>(predicate, nameof(predicate));
      return Enumerable.WhereYield<TSource>(source, predicate);
    }

    private static IEnumerable<TSource> WhereYield<TSource>(
      IEnumerable<TSource> source,
      Func<TSource, int, bool> predicate) {
      int i = 0;
      foreach (TSource source1 in source) {
        if (predicate(source1, i++))
          yield return source1;
      }
    }

    public static IEnumerable<TResult> Select<TSource, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TResult> selector) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TResult>>(selector, nameof(selector));
      return Enumerable.SelectYield<TSource, TResult>(source, selector);
    }

    private static IEnumerable<TResult> SelectYield<TSource, TResult>(
      IEnumerable<TSource> source,
      Func<TSource, TResult> selector) {
      foreach (TSource a in source)
        yield return selector(a);
    }

    public static IEnumerable<TResult> Select<TSource, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, int, TResult> selector) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, int, TResult>>(selector, nameof(selector));
      return Enumerable.SelectYield<TSource, TResult>(source, selector);
    }

    private static IEnumerable<TResult> SelectYield<TSource, TResult>(
      IEnumerable<TSource> source,
      Func<TSource, int, TResult> selector) {
      int i = 0;
      foreach (TSource source1 in source)
        yield return selector(source1, i++);
    }

    public static IEnumerable<TResult> SelectMany<TSource, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, IEnumerable<TResult>> selector) {
      Enumerable.CheckNotNull<Func<TSource, IEnumerable<TResult>>>(selector, nameof(selector));
      return source.SelectMany<TSource, TResult>(
        (Func<TSource, int, IEnumerable<TResult>>) ((item, i) => selector(item)));
    }

    public static IEnumerable<TResult> SelectMany<TSource, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, int, IEnumerable<TResult>> selector) {
      Enumerable.CheckNotNull<Func<TSource, int, IEnumerable<TResult>>>(selector, nameof(selector));
      return source.SelectMany<TSource, TResult, TResult>(selector,
        (Func<TSource, TResult, TResult>) ((item, subitem) => subitem));
    }

    public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, IEnumerable<TCollection>> collectionSelector,
      Func<TSource, TCollection, TResult> resultSelector) {
      Enumerable.CheckNotNull<Func<TSource, IEnumerable<TCollection>>>(collectionSelector, nameof(collectionSelector));
      return source.SelectMany<TSource, TCollection, TResult>(
        (Func<TSource, int, IEnumerable<TCollection>>) ((item, i) => collectionSelector(item)), resultSelector);
    }

    public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
      Func<TSource, TCollection, TResult> resultSelector) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, int, IEnumerable<TCollection>>>(collectionSelector,
        nameof(collectionSelector));
      Enumerable.CheckNotNull<Func<TSource, TCollection, TResult>>(resultSelector, nameof(resultSelector));
      return source.SelectManyYield<TSource, TCollection, TResult>(collectionSelector, resultSelector);
    }

    private static IEnumerable<TResult> SelectManyYield<TSource, TCollection, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
      Func<TSource, TCollection, TResult> resultSelector) {
      int i = 0;
      foreach (TSource source1 in source) {
        TSource item = source1;
        foreach (TCollection collection in collectionSelector(item, i++))
          yield return resultSelector(item, collection);
        item = default(TSource);
      }
    }

    public static IEnumerable<TSource> TakeWhile<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      Enumerable.CheckNotNull<Func<TSource, bool>>(predicate, nameof(predicate));
      return source.TakeWhile<TSource>((Func<TSource, int, bool>) ((item, i) => predicate(item)));
    }

    public static IEnumerable<TSource> TakeWhile<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, int, bool> predicate) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, int, bool>>(predicate, nameof(predicate));
      return source.TakeWhileYield<TSource>(predicate);
    }

    private static IEnumerable<TSource> TakeWhileYield<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, int, bool> predicate) {
      int i = 0;
      foreach (TSource source1 in source) {
        if (predicate(source1, i++))
          yield return source1;
        else
          break;
      }
    }

    private static TSource FirstImpl<TSource>(this IEnumerable<TSource> source, Func<TSource> empty) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      IList<TSource> sourceList = source as IList<TSource>;
      if (sourceList != null) {
        if (sourceList.Count <= 0)
          return empty();
        return sourceList[0];
      }

      using (IEnumerator<TSource> enumerator = source.GetEnumerator())
        return enumerator.MoveNext() ? enumerator.Current : empty();
    }

    public static TSource First<TSource>(this IEnumerable<TSource> source) {
      return source.FirstImpl<TSource>(Enumerable.Futures<TSource>.Undefined);
    }

    public static TSource First<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).First<TSource>();
    }

    public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source) {
      return source.FirstImpl<TSource>(Enumerable.Futures<TSource>.Default);
    }

    public static TSource FirstOrDefault<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).FirstOrDefault<TSource>();
    }

    private static TSource LastImpl<TSource>(this IEnumerable<TSource> source, Func<TSource> empty) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      IList<TSource> sourceList = source as IList<TSource>;
      if (sourceList != null) {
        if (sourceList.Count <= 0)
          return empty();
        return sourceList[sourceList.Count - 1];
      }

      using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
        if (!enumerator.MoveNext())
          return empty();
        TSource current = enumerator.Current;
        while (enumerator.MoveNext())
          current = enumerator.Current;
        return current;
      }
    }

    public static TSource Last<TSource>(this IEnumerable<TSource> source) {
      return source.LastImpl<TSource>(Enumerable.Futures<TSource>.Undefined);
    }

    public static TSource Last<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).Last<TSource>();
    }

    public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source) {
      return source.LastImpl<TSource>(Enumerable.Futures<TSource>.Default);
    }

    public static TSource LastOrDefault<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).LastOrDefault<TSource>();
    }

    private static TSource SingleImpl<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource> empty) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
        if (!enumerator.MoveNext())
          return empty();
        TSource current = enumerator.Current;
        if (!enumerator.MoveNext())
          return current;
        throw new InvalidOperationException();
      }
    }

    public static TSource Single<TSource>(this IEnumerable<TSource> source) {
      return source.SingleImpl<TSource>(Enumerable.Futures<TSource>.Undefined);
    }

    public static TSource Single<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).Single<TSource>();
    }

    public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source) {
      return source.SingleImpl<TSource>(Enumerable.Futures<TSource>.Default);
    }

    public static TSource SingleOrDefault<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).SingleOrDefault<TSource>();
    }

    public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      if (index < 0)
        throw new ArgumentOutOfRangeException(nameof(index), (object) index, (string) null);
      IList<TSource> sourceList = source as IList<TSource>;
      if (sourceList != null)
        return sourceList[index];
      try {
        return source.SkipWhile<TSource>((Func<TSource, int, bool>) ((item, i) => i < index)).First<TSource>();
      } catch (InvalidOperationException ex) {
        throw new ArgumentOutOfRangeException(nameof(index), (object) index, (string) null);
      }
    }

    public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      if (index < 0)
        return default(TSource);
      IList<TSource> sourceList = source as IList<TSource>;
      if (sourceList == null)
        return source.SkipWhile<TSource>((Func<TSource, int, bool>) ((item, i) => i < index)).FirstOrDefault<TSource>();
      if (index >= sourceList.Count)
        return default(TSource);
      return sourceList[index];
    }

    public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      return Enumerable.ReverseYield<TSource>(source);
    }

    private static IEnumerable<TSource> ReverseYield<TSource>(IEnumerable<TSource> source) {
      foreach (TSource source1 in new Stack<TSource>(source))
        yield return source1;
    }

    public static IEnumerable<TSource> Take<TSource>(
      this IEnumerable<TSource> source,
      int count) {
      return source.Where<TSource>((Func<TSource, int, bool>) ((item, i) => i < count));
    }

    public static IEnumerable<TSource> Skip<TSource>(
      this IEnumerable<TSource> source,
      int count) {
      return source.Where<TSource>((Func<TSource, int, bool>) ((item, i) => i >= count));
    }

    public static IEnumerable<TSource> SkipWhile<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      Enumerable.CheckNotNull<Func<TSource, bool>>(predicate, nameof(predicate));
      return source.SkipWhile<TSource>((Func<TSource, int, bool>) ((item, i) => predicate(item)));
    }

    public static IEnumerable<TSource> SkipWhile<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, int, bool> predicate) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, int, bool>>(predicate, nameof(predicate));
      return Enumerable.SkipWhileYield<TSource>(source, predicate);
    }

    private static IEnumerable<TSource> SkipWhileYield<TSource>(
      IEnumerable<TSource> source,
      Func<TSource, int, bool> predicate) {
      using (IEnumerator<TSource> e = source.GetEnumerator()) {
        int num = 0;
        while (true) {
          if (e.MoveNext()) {
            if (predicate(e.Current, num))
              ++num;
            else
              goto label_6;
          } else
            break;
        }

        yield break;
        label_6:
        do {
          yield return e.Current;
        } while (e.MoveNext());
      }
    }

    public static int Count<TSource>(this IEnumerable<TSource> source) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      ICollection collection = source as ICollection;
      if (collection != null)
        return collection.Count;
      using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
        int num = 0;
        while (enumerator.MoveNext())
          ++num;
        return num;
      }
    }

    public static int Count<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).Count<TSource>();
    }

    public static long LongCount<TSource>(this IEnumerable<TSource> source) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Array array = source as Array;
      if (array == null)
        return source.Aggregate<TSource, long>(0L, (Func<long, TSource, long>) ((count, item) => count + 1L));
      return array.LongLength;
    }

    public static long LongCount<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, bool> predicate) {
      return source.Where<TSource>(predicate).LongCount<TSource>();
    }

    public static IEnumerable<TSource> Concat<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(first, nameof(first));
      Enumerable.CheckNotNull<IEnumerable<TSource>>(second, nameof(second));
      return Enumerable.ConcatYield<TSource>(first, second);
    }

    private static IEnumerable<TSource> ConcatYield<TSource>(
      IEnumerable<TSource> first,
      IEnumerable<TSource> second) {
      foreach (TSource source in first)
        yield return source;
      foreach (TSource source in second)
        yield return source;
    }

    public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      return new List<TSource>(source);
    }

    public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source) {
      IList<TSource> sourceList = source as IList<TSource>;
      if (sourceList == null)
        return source.ToList<TSource>().ToArray();
      TSource[] array = new TSource[sourceList.Count];
      sourceList.CopyTo(array, 0);
      return array;
    }

    public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source) {
      return source.Distinct<TSource>((IEqualityComparer<TSource>) null);
    }

    public static IEnumerable<TSource> Distinct<TSource>(
      this IEnumerable<TSource> source,
      IEqualityComparer<TSource> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      return Enumerable.DistinctYield<TSource>(source, comparer);
    }

    private static IEnumerable<TSource> DistinctYield<TSource>(
      IEnumerable<TSource> source,
      IEqualityComparer<TSource> comparer) {
      Dictionary<TSource, object> set = new Dictionary<TSource, object>(comparer);
      bool gotNull = false;
      foreach (TSource key in source) {
        if ((object) (TSource) key == null) {
          if (!gotNull)
            gotNull = true;
          else
            continue;
        } else if (!set.ContainsKey(key))
          set.Add(key, (object) null);
        else
          continue;

        yield return key;
      }
    }

    public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.ToLookup<TSource, TKey, TSource>(keySelector, (Func<TSource, TSource>) (e => e),
        (IEqualityComparer<TKey>) null);
    }

    public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IEqualityComparer<TKey> comparer) {
      return source.ToLookup<TSource, TKey, TSource>(keySelector, (Func<TSource, TSource>) (e => e), comparer);
    }

    public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector) {
      return source.ToLookup<TSource, TKey, TElement>(keySelector, elementSelector, (IEqualityComparer<TKey>) null);
    }

    public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TKey>>(keySelector, nameof(keySelector));
      Enumerable.CheckNotNull<Func<TSource, TElement>>(elementSelector, nameof(elementSelector));
      Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
      foreach (TSource a in source) {
        TKey key = keySelector(a);
        Enumerable.Grouping<TKey, TElement> grouping = (Enumerable.Grouping<TKey, TElement>) lookup.Find(key);
        if (grouping == null) {
          grouping = new Enumerable.Grouping<TKey, TElement>(key);
          lookup.Add((IGrouping<TKey, TElement>) grouping);
        }

        grouping.Add(elementSelector(a));
      }

      return (ILookup<TKey, TElement>) lookup;
    }

    public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.GroupBy<TSource, TKey>(keySelector, (IEqualityComparer<TKey>) null);
    }

    public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IEqualityComparer<TKey> comparer) {
      return source.GroupBy<TSource, TKey, TSource>(keySelector, (Func<TSource, TSource>) (e => e), comparer);
    }

    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector) {
      return source.GroupBy<TSource, TKey, TElement>(keySelector, elementSelector, (IEqualityComparer<TKey>) null);
    }

    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TKey>>(keySelector, nameof(keySelector));
      Enumerable.CheckNotNull<Func<TSource, TElement>>(elementSelector, nameof(elementSelector));
      return (IEnumerable<IGrouping<TKey, TElement>>) source.ToLookup<TSource, TKey, TElement>(keySelector,
        elementSelector, comparer);
    }

    public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TKey, IEnumerable<TSource>, TResult> resultSelector) {
      return source.GroupBy<TSource, TKey, TResult>(keySelector, resultSelector, (IEqualityComparer<TKey>) null);
    }

    public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TKey>>(keySelector, nameof(keySelector));
      Enumerable.CheckNotNull<Func<TKey, IEnumerable<TSource>, TResult>>(resultSelector, nameof(resultSelector));
      return source.ToLookup<TSource, TKey>(keySelector, comparer).Select<IGrouping<TKey, TSource>, TResult>(
        (Func<IGrouping<TKey, TSource>, TResult>) (g => resultSelector(g.Key, (IEnumerable<TSource>) g)));
    }

    public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector,
      Func<TKey, IEnumerable<TElement>, TResult> resultSelector) {
      return source.GroupBy<TSource, TKey, TElement, TResult>(keySelector, elementSelector, resultSelector,
        (IEqualityComparer<TKey>) null);
    }

    public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector,
      Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TKey>>(keySelector, nameof(keySelector));
      Enumerable.CheckNotNull<Func<TSource, TElement>>(elementSelector, nameof(elementSelector));
      Enumerable.CheckNotNull<Func<TKey, IEnumerable<TElement>, TResult>>(resultSelector, nameof(resultSelector));
      return source.ToLookup<TSource, TKey, TElement>(keySelector, elementSelector, comparer)
        .Select<IGrouping<TKey, TElement>, TResult>(
          (Func<IGrouping<TKey, TElement>, TResult>) (g => resultSelector(g.Key, (IEnumerable<TElement>) g)));
    }

    public static TSource Aggregate<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, TSource, TSource> func) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TSource, TSource>>(func, nameof(func));
      using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
        if (!enumerator.MoveNext())
          throw new InvalidOperationException();
        return enumerator.Renumerable<TSource>().Skip<TSource>(1).Aggregate<TSource, TSource>(enumerator.Current, func);
      }
    }

    public static TAccumulate Aggregate<TSource, TAccumulate>(
      this IEnumerable<TSource> source,
      TAccumulate seed,
      Func<TAccumulate, TSource, TAccumulate> func) {
      return source.Aggregate<TSource, TAccumulate, TAccumulate>(seed, func, (Func<TAccumulate, TAccumulate>) (r => r));
    }

    public static TResult Aggregate<TSource, TAccumulate, TResult>(
      this IEnumerable<TSource> source,
      TAccumulate seed,
      Func<TAccumulate, TSource, TAccumulate> func,
      Func<TAccumulate, TResult> resultSelector) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TAccumulate, TSource, TAccumulate>>(func, nameof(func));
      Enumerable.CheckNotNull<Func<TAccumulate, TResult>>(resultSelector, nameof(resultSelector));
      TAccumulate a = seed;
      foreach (TSource source1 in source)
        a = func(a, source1);
      return resultSelector(a);
    }

    public static IEnumerable<TSource> Union<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second) {
      return first.Union<TSource>(second, (IEqualityComparer<TSource>) null);
    }

    public static IEnumerable<TSource> Union<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second,
      IEqualityComparer<TSource> comparer) {
      return first.Concat<TSource>(second).Distinct<TSource>(comparer);
    }

    public static IEnumerable<TSource> DefaultIfEmpty<TSource>(
      this IEnumerable<TSource> source) {
      return source.DefaultIfEmpty<TSource>(default(TSource));
    }

    public static IEnumerable<TSource> DefaultIfEmpty<TSource>(
      this IEnumerable<TSource> source,
      TSource defaultValue) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      return Enumerable.DefaultIfEmptyYield<TSource>(source, defaultValue);
    }

    private static IEnumerable<TSource> DefaultIfEmptyYield<TSource>(
      IEnumerable<TSource> source,
      TSource defaultValue) {
      using (IEnumerator<TSource> e = source.GetEnumerator()) {
        if (!e.MoveNext()) {
          yield return defaultValue;
        } else {
          do {
            yield return e.Current;
          } while (e.MoveNext());
        }
      }
    }

    public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, bool>>(predicate, nameof(predicate));
      foreach (TSource a in source) {
        if (!predicate(a))
          return false;
      }

      return true;
    }

    public static bool Any<TSource>(this IEnumerable<TSource> source) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      using (IEnumerator<TSource> enumerator = source.GetEnumerator())
        return enumerator.MoveNext();
    }

    public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
      foreach (TSource a in source) {
        if (predicate(a))
          return true;
      }

      return false;
    }

    public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value) {
      return source.Contains<TSource>(value, (IEqualityComparer<TSource>) null);
    }

    public static bool Contains<TSource>(
      this IEnumerable<TSource> source,
      TSource value,
      IEqualityComparer<TSource> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      if (comparer == null) {
        ICollection<TSource> sources = source as ICollection<TSource>;
        if (sources != null)
          return sources.Contains(value);
      }

      comparer = comparer ?? (IEqualityComparer<TSource>) EqualityComparer<TSource>.Default;
      return source.Any<TSource>((Func<TSource, bool>) (item => comparer.Equals(item, value)));
    }

    public static bool SequenceEqual<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second) {
      return first.SequenceEqual<TSource>(second, (IEqualityComparer<TSource>) null);
    }

    public static bool SequenceEqual<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second,
      IEqualityComparer<TSource> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(first, nameof(first));
      Enumerable.CheckNotNull<IEnumerable<TSource>>(second, nameof(second));
      comparer = comparer ?? (IEqualityComparer<TSource>) EqualityComparer<TSource>.Default;
      using (IEnumerator<TSource> enumerator1 = first.GetEnumerator()) {
        using (IEnumerator<TSource> enumerator2 = second.GetEnumerator()) {
          while (enumerator1.MoveNext()) {
            if (!enumerator2.MoveNext() || !comparer.Equals(enumerator1.Current, enumerator2.Current))
              return false;
          }

          return !enumerator2.MoveNext();
        }
      }
    }

    private static TSource MinMaxImpl<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, TSource, bool> lesser) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      return source.Aggregate<TSource>((Func<TSource, TSource, TSource>) ((a, item) => {
        if (!lesser(a, item))
          return item;
        return a;
      }));
    }

    private static TSource? MinMaxImpl<TSource>(
      this IEnumerable<TSource?> source,
      TSource? seed,
      Func<TSource?, TSource?, bool> lesser)
      where TSource : struct {
      Enumerable.CheckNotNull<IEnumerable<TSource?>>(source, nameof(source));
      return source.Aggregate<TSource?, TSource?>(seed, (Func<TSource?, TSource?, TSource?>) ((a, item) => {
        if (!lesser(a, item))
          return item;
        return a;
      }));
    }

    public static TSource Min<TSource>(this IEnumerable<TSource> source) {
      Comparer<TSource> comparer = Comparer<TSource>.Default;
      return source.MinMaxImpl<TSource>((Func<TSource, TSource, bool>) ((x, y) => comparer.Compare(x, y) < 0));
    }

    public static TResult Min<TSource, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TResult> selector) {
      return source.Select<TSource, TResult>(selector).Min<TResult>();
    }

    public static TSource Max<TSource>(this IEnumerable<TSource> source) {
      Comparer<TSource> comparer = Comparer<TSource>.Default;
      return source.MinMaxImpl<TSource>((Func<TSource, TSource, bool>) ((x, y) => comparer.Compare(x, y) > 0));
    }

    public static TResult Max<TSource, TResult>(
      this IEnumerable<TSource> source,
      Func<TSource, TResult> selector) {
      return source.Select<TSource, TResult>(selector).Max<TResult>();
    }

    private static IEnumerable<T> Renumerable<T>(this IEnumerator<T> e) {
      do {
        yield return e.Current;
      } while (e.MoveNext());
    }

    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.OrderBy<TSource, TKey>(keySelector, (IComparer<TKey>) null);
    }

    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TKey>>(keySelector, nameof(keySelector));
      return (IOrderedEnumerable<TSource>) new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false);
    }

    public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.OrderByDescending<TSource, TKey>(keySelector, (IComparer<TKey>) null);
    }

    public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(keySelector));
      return (IOrderedEnumerable<TSource>) new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true);
    }

    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
      this IOrderedEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.ThenBy<TSource, TKey>(keySelector, (IComparer<TKey>) null);
    }

    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
      this IOrderedEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IOrderedEnumerable<TSource>>(source, nameof(source));
      return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, false);
    }

    public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
      this IOrderedEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.ThenByDescending<TSource, TKey>(keySelector, (IComparer<TKey>) null);
    }

    public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
      this IOrderedEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IOrderedEnumerable<TSource>>(source, nameof(source));
      return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, true);
    }

    private static IEnumerable<TSource> IntersectExceptImpl<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second,
      IEqualityComparer<TSource> comparer,
      bool flag) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(first, nameof(first));
      Enumerable.CheckNotNull<IEnumerable<TSource>>(second, nameof(second));
      List<TSource> source = new List<TSource>();
      Dictionary<TSource, bool> flags = new Dictionary<TSource, bool>(comparer);
      foreach (TSource key in first.Where<TSource>((Func<TSource, bool>) (k => !flags.ContainsKey(k)))) {
        flags.Add(key, !flag);
        source.Add(key);
      }

      foreach (TSource index in second.Where<TSource>(new Func<TSource, bool>(flags.ContainsKey)))
        flags[index] = flag;
      return source.Where<TSource>((Func<TSource, bool>) (item => flags[item]));
    }

    public static IEnumerable<TSource> Intersect<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second) {
      return first.Intersect<TSource>(second, (IEqualityComparer<TSource>) null);
    }

    public static IEnumerable<TSource> Intersect<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second,
      IEqualityComparer<TSource> comparer) {
      return first.IntersectExceptImpl<TSource>(second, comparer, true);
    }

    public static IEnumerable<TSource> Except<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second) {
      return first.Except<TSource>(second, (IEqualityComparer<TSource>) null);
    }

    public static IEnumerable<TSource> Except<TSource>(
      this IEnumerable<TSource> first,
      IEnumerable<TSource> second,
      IEqualityComparer<TSource> comparer) {
      return first.IntersectExceptImpl<TSource>(second, comparer, false);
    }

    public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector) {
      return source.ToDictionary<TSource, TKey>(keySelector, (IEqualityComparer<TKey>) null);
    }

    public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      IEqualityComparer<TKey> comparer) {
      return source.ToDictionary<TSource, TKey, TSource>(keySelector, (Func<TSource, TSource>) (e => e));
    }

    public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector) {
      return source.ToDictionary<TSource, TKey, TElement>(keySelector, elementSelector, (IEqualityComparer<TKey>) null);
    }

    public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TElement> elementSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, TKey>>(keySelector, nameof(keySelector));
      Enumerable.CheckNotNull<Func<TSource, TElement>>(elementSelector, nameof(elementSelector));
      Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(comparer);
      foreach (TSource a in source)
        dictionary.Add(keySelector(a), elementSelector(a));
      return dictionary;
    }

    public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
      this IEnumerable<TOuter> outer,
      IEnumerable<TInner> inner,
      Func<TOuter, TKey> outerKeySelector,
      Func<TInner, TKey> innerKeySelector,
      Func<TOuter, TInner, TResult> resultSelector) {
      return outer.Join<TOuter, TInner, TKey, TResult>(inner, outerKeySelector, innerKeySelector, resultSelector,
        (IEqualityComparer<TKey>) null);
    }

    public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
      this IEnumerable<TOuter> outer,
      IEnumerable<TInner> inner,
      Func<TOuter, TKey> outerKeySelector,
      Func<TInner, TKey> innerKeySelector,
      Func<TOuter, TInner, TResult> resultSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TOuter>>(outer, nameof(outer));
      Enumerable.CheckNotNull<IEnumerable<TInner>>(inner, nameof(inner));
      Enumerable.CheckNotNull<Func<TOuter, TKey>>(outerKeySelector, nameof(outerKeySelector));
      Enumerable.CheckNotNull<Func<TInner, TKey>>(innerKeySelector, nameof(innerKeySelector));
      Enumerable.CheckNotNull<Func<TOuter, TInner, TResult>>(resultSelector, nameof(resultSelector));
      ILookup<TKey, TInner> lookup = inner.ToLookup<TInner, TKey>(innerKeySelector, comparer);
      return outer.SelectMany<TOuter, TInner, TResult>(
        (Func<TOuter, IEnumerable<TInner>>) (o => lookup[outerKeySelector(o)]),
        (Func<TOuter, TInner, TResult>) ((o, i) => resultSelector(o, i)));
    }

    public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
      this IEnumerable<TOuter> outer,
      IEnumerable<TInner> inner,
      Func<TOuter, TKey> outerKeySelector,
      Func<TInner, TKey> innerKeySelector,
      Func<TOuter, IEnumerable<TInner>, TResult> resultSelector) {
      return outer.GroupJoin<TOuter, TInner, TKey, TResult>(inner, outerKeySelector, innerKeySelector, resultSelector,
        (IEqualityComparer<TKey>) null);
    }

    public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
      this IEnumerable<TOuter> outer,
      IEnumerable<TInner> inner,
      Func<TOuter, TKey> outerKeySelector,
      Func<TInner, TKey> innerKeySelector,
      Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
      IEqualityComparer<TKey> comparer) {
      Enumerable.CheckNotNull<IEnumerable<TOuter>>(outer, nameof(outer));
      Enumerable.CheckNotNull<IEnumerable<TInner>>(inner, nameof(inner));
      Enumerable.CheckNotNull<Func<TOuter, TKey>>(outerKeySelector, nameof(outerKeySelector));
      Enumerable.CheckNotNull<Func<TInner, TKey>>(innerKeySelector, nameof(innerKeySelector));
      Enumerable.CheckNotNull<Func<TOuter, IEnumerable<TInner>, TResult>>(resultSelector, nameof(resultSelector));
      ILookup<TKey, TInner> lookup = inner.ToLookup<TInner, TKey>(innerKeySelector, comparer);
      return outer.Select<TOuter, TResult>(
        (Func<TOuter, TResult>) (o => resultSelector(o, lookup[outerKeySelector(o)])));
    }

    [DebuggerStepThrough]
    private static void CheckNotNull<T>(T value, string name) where T : class {
      if ((object) value == null)
        throw new ArgumentNullException(name);
    }

    public static int Sum(this IEnumerable<int> source) {
      Enumerable.CheckNotNull<IEnumerable<int>>(source, nameof(source));
      int num1 = 0;
      foreach (int num2 in source)
        checked {
          num1 += num2;
        }

      return num1;
    }

    public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) {
      return source.Select<TSource, int>(selector).Sum();
    }

    public static double Average(this IEnumerable<int> source) {
      Enumerable.CheckNotNull<IEnumerable<int>>(source, nameof(source));
      long num1 = 0;
      long num2 = 0;
      foreach (int num3 in source) {
        checked {
          num1 += (long) num3;
        }

        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        throw new InvalidOperationException();
      return (double) num1 / (double) num2;
    }

    public static double Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, int> selector) {
      return source.Select<TSource, int>(selector).Average();
    }

    public static int? Sum(this IEnumerable<int?> source) {
      Enumerable.CheckNotNull<IEnumerable<int?>>(source, nameof(source));
      int num = 0;
      foreach (int? nullable in source)
        checked {
          num += nullable ?? 0;
        }

      return new int?(num);
    }

    public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) {
      return source.Select<TSource, int?>(selector).Sum();
    }

    public static double? Average(this IEnumerable<int?> source) {
      Enumerable.CheckNotNull<IEnumerable<int?>>(source, nameof(source));
      long num1 = 0;
      long num2 = 0;
      foreach (int? nullable in source.Where<int?>((Func<int?, bool>) (n => n.HasValue))) {
        checked {
          num1 += (long) nullable.Value;
        }

        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        return new double?();
      return new double?((double) num1 / (double) num2);
    }

    public static double? Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, int?> selector) {
      return source.Select<TSource, int?>(selector).Average();
    }

    public static int? Min(this IEnumerable<int?> source) {
      Enumerable.CheckNotNull<IEnumerable<int?>>(source, nameof(source));
      return source.Where<int?>((Func<int?, bool>) (x => x.HasValue)).MinMaxImpl<int>(new int?(),
        (Func<int?, int?, bool>) ((min, x) => {
          int? nullable1 = min;
          int? nullable2 = x;
          if (nullable1.GetValueOrDefault() >= nullable2.GetValueOrDefault())
            return false;
          return nullable1.HasValue & nullable2.HasValue;
        }));
    }

    public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) {
      return source.Select<TSource, int?>(selector).Min();
    }

    public static int? Max(this IEnumerable<int?> source) {
      Enumerable.CheckNotNull<IEnumerable<int?>>(source, nameof(source));
      return source.Where<int?>((Func<int?, bool>) (x => x.HasValue)).MinMaxImpl<int>(new int?(),
        (Func<int?, int?, bool>) ((max, x) => {
          if (!x.HasValue)
            return true;
          if (max.HasValue)
            return x.Value < max.Value;
          return false;
        }));
    }

    public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) {
      return source.Select<TSource, int?>(selector).Max();
    }

    public static long Sum(this IEnumerable<long> source) {
      Enumerable.CheckNotNull<IEnumerable<long>>(source, nameof(source));
      long num1 = 0;
      foreach (long num2 in source)
        checked {
          num1 += num2;
        }

      return num1;
    }

    public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) {
      return source.Select<TSource, long>(selector).Sum();
    }

    public static double Average(this IEnumerable<long> source) {
      Enumerable.CheckNotNull<IEnumerable<long>>(source, nameof(source));
      long num1 = 0;
      long num2 = 0;
      foreach (long num3 in source) {
        checked {
          num1 += num3;
        }

        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        throw new InvalidOperationException();
      return (double) num1 / (double) num2;
    }

    public static double Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, long> selector) {
      return source.Select<TSource, long>(selector).Average();
    }

    public static long? Sum(this IEnumerable<long?> source) {
      Enumerable.CheckNotNull<IEnumerable<long?>>(source, nameof(source));
      long num = 0;
      foreach (long? nullable in source)
        checked {
          num += nullable ?? 0L;
        }

      return new long?(num);
    }

    public static long? Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, long?> selector) {
      return source.Select<TSource, long?>(selector).Sum();
    }

    public static double? Average(this IEnumerable<long?> source) {
      Enumerable.CheckNotNull<IEnumerable<long?>>(source, nameof(source));
      long num1 = 0;
      long num2 = 0;
      foreach (long? nullable in source.Where<long?>((Func<long?, bool>) (n => n.HasValue))) {
        checked {
          num1 += nullable.Value;
        }

        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        return new double?();
      return new double?((double) num1 / (double) num2);
    }

    public static double? Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, long?> selector) {
      return source.Select<TSource, long?>(selector).Average();
    }

    public static long? Min(this IEnumerable<long?> source) {
      Enumerable.CheckNotNull<IEnumerable<long?>>(source, nameof(source));
      return source.Where<long?>((Func<long?, bool>) (x => x.HasValue)).MinMaxImpl<long>(new long?(),
        (Func<long?, long?, bool>) ((min, x) => {
          long? nullable1 = min;
          long? nullable2 = x;
          if (nullable1.GetValueOrDefault() >= nullable2.GetValueOrDefault())
            return false;
          return nullable1.HasValue & nullable2.HasValue;
        }));
    }

    public static long? Min<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, long?> selector) {
      return source.Select<TSource, long?>(selector).Min();
    }

    public static long? Max(this IEnumerable<long?> source) {
      Enumerable.CheckNotNull<IEnumerable<long?>>(source, nameof(source));
      return source.Where<long?>((Func<long?, bool>) (x => x.HasValue)).MinMaxImpl<long>(new long?(),
        (Func<long?, long?, bool>) ((max, x) => {
          if (!x.HasValue)
            return true;
          if (max.HasValue)
            return x.Value < max.Value;
          return false;
        }));
    }

    public static long? Max<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, long?> selector) {
      return source.Select<TSource, long?>(selector).Max();
    }

    public static float Sum(this IEnumerable<float> source) {
      Enumerable.CheckNotNull<IEnumerable<float>>(source, nameof(source));
      float num1 = 0.0f;
      foreach (float num2 in source)
        num1 += num2;
      return num1;
    }

    public static float Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, float> selector) {
      return source.Select<TSource, float>(selector).Sum();
    }

    public static float Average(this IEnumerable<float> source) {
      Enumerable.CheckNotNull<IEnumerable<float>>(source, nameof(source));
      float num1 = 0.0f;
      long num2 = 0;
      foreach (float num3 in source) {
        num1 += num3;
        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        throw new InvalidOperationException();
      return num1 / (float) num2;
    }

    public static float Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, float> selector) {
      return source.Select<TSource, float>(selector).Average();
    }

    public static float? Sum(this IEnumerable<float?> source) {
      Enumerable.CheckNotNull<IEnumerable<float?>>(source, nameof(source));
      float num1 = 0.0f;
      foreach (float? nullable1 in source) {
        double num2 = (double) num1;
        float? nullable2 = nullable1;
        double num3 = nullable2.HasValue ? (double) nullable2.GetValueOrDefault() : 0.0;
        num1 = (float) (num2 + num3);
      }

      return new float?(num1);
    }

    public static float? Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, float?> selector) {
      return source.Select<TSource, float?>(selector).Sum();
    }

    public static float? Average(this IEnumerable<float?> source) {
      Enumerable.CheckNotNull<IEnumerable<float?>>(source, nameof(source));
      float num1 = 0.0f;
      long num2 = 0;
      foreach (float? nullable in source.Where<float?>((Func<float?, bool>) (n => n.HasValue))) {
        num1 += nullable.Value;
        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        return new float?();
      return new float?(num1 / (float) num2);
    }

    public static float? Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, float?> selector) {
      return source.Select<TSource, float?>(selector).Average();
    }

    public static float? Min(this IEnumerable<float?> source) {
      Enumerable.CheckNotNull<IEnumerable<float?>>(source, nameof(source));
      return source.Where<float?>((Func<float?, bool>) (x => x.HasValue)).MinMaxImpl<float>(new float?(),
        (Func<float?, float?, bool>) ((min, x) => {
          float? nullable1 = min;
          float? nullable2 = x;
          if ((double) nullable1.GetValueOrDefault() >= (double) nullable2.GetValueOrDefault())
            return false;
          return nullable1.HasValue & nullable2.HasValue;
        }));
    }

    public static float? Min<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, float?> selector) {
      return source.Select<TSource, float?>(selector).Min();
    }

    public static float? Max(this IEnumerable<float?> source) {
      Enumerable.CheckNotNull<IEnumerable<float?>>(source, nameof(source));
      return source.Where<float?>((Func<float?, bool>) (x => x.HasValue)).MinMaxImpl<float>(new float?(),
        (Func<float?, float?, bool>) ((max, x) => {
          if (!x.HasValue)
            return true;
          if (max.HasValue)
            return (double) x.Value < (double) max.Value;
          return false;
        }));
    }

    public static float? Max<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, float?> selector) {
      return source.Select<TSource, float?>(selector).Max();
    }

    public static double Sum(this IEnumerable<double> source) {
      Enumerable.CheckNotNull<IEnumerable<double>>(source, nameof(source));
      double num1 = 0.0;
      foreach (double num2 in source)
        num1 += num2;
      return num1;
    }

    public static double Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, double> selector) {
      return source.Select<TSource, double>(selector).Sum();
    }

    public static double Average(this IEnumerable<double> source) {
      Enumerable.CheckNotNull<IEnumerable<double>>(source, nameof(source));
      double num1 = 0.0;
      long num2 = 0;
      foreach (double num3 in source) {
        num1 += num3;
        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        throw new InvalidOperationException();
      return num1 / (double) num2;
    }

    public static double Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, double> selector) {
      return source.Select<TSource, double>(selector).Average();
    }

    public static double? Sum(this IEnumerable<double?> source) {
      Enumerable.CheckNotNull<IEnumerable<double?>>(source, nameof(source));
      double num = 0.0;
      foreach (double? nullable in source)
        num += nullable ?? 0.0;
      return new double?(num);
    }

    public static double? Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, double?> selector) {
      return source.Select<TSource, double?>(selector).Sum();
    }

    public static double? Average(this IEnumerable<double?> source) {
      Enumerable.CheckNotNull<IEnumerable<double?>>(source, nameof(source));
      double num1 = 0.0;
      long num2 = 0;
      foreach (double? nullable in source.Where<double?>((Func<double?, bool>) (n => n.HasValue))) {
        num1 += nullable.Value;
        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        return new double?();
      return new double?(num1 / (double) num2);
    }

    public static double? Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, double?> selector) {
      return source.Select<TSource, double?>(selector).Average();
    }

    public static double? Min(this IEnumerable<double?> source) {
      Enumerable.CheckNotNull<IEnumerable<double?>>(source, nameof(source));
      return source.Where<double?>((Func<double?, bool>) (x => x.HasValue)).MinMaxImpl<double>(new double?(),
        (Func<double?, double?, bool>) ((min, x) => {
          double? nullable1 = min;
          double? nullable2 = x;
          if (nullable1.GetValueOrDefault() >= nullable2.GetValueOrDefault())
            return false;
          return nullable1.HasValue & nullable2.HasValue;
        }));
    }

    public static double? Min<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, double?> selector) {
      return source.Select<TSource, double?>(selector).Min();
    }

    public static double? Max(this IEnumerable<double?> source) {
      Enumerable.CheckNotNull<IEnumerable<double?>>(source, nameof(source));
      return source.Where<double?>((Func<double?, bool>) (x => x.HasValue)).MinMaxImpl<double>(new double?(),
        (Func<double?, double?, bool>) ((max, x) => {
          if (!x.HasValue)
            return true;
          if (max.HasValue)
            return x.Value < max.Value;
          return false;
        }));
    }

    public static double? Max<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, double?> selector) {
      return source.Select<TSource, double?>(selector).Max();
    }

    public static Decimal Sum(this IEnumerable<Decimal> source) {
      Enumerable.CheckNotNull<IEnumerable<Decimal>>(source, nameof(source));
      Decimal num1 = new Decimal();
      foreach (Decimal num2 in source)
        num1 += num2;
      return num1;
    }

    public static Decimal Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, Decimal> selector) {
      Enumerable.CheckNotNull<IEnumerable<TSource>>(source, nameof(source));
      Enumerable.CheckNotNull<Func<TSource, Decimal>>(selector, nameof(selector));
      Decimal num = new Decimal();
      foreach (TSource a in source)
        num += selector(a);
      return num;
    }

    public static Decimal Average(this IEnumerable<Decimal> source) {
      Enumerable.CheckNotNull<IEnumerable<Decimal>>(source, nameof(source));
      Decimal num1 = new Decimal();
      long num2 = 0;
      foreach (Decimal num3 in source) {
        num1 += num3;
        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        throw new InvalidOperationException();
      return num1 / (Decimal) num2;
    }

    public static Decimal Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, Decimal> selector) {
      return source.Select<TSource, Decimal>(selector).Average();
    }

    public static Decimal? Sum(this IEnumerable<Decimal?> source) {
      Enumerable.CheckNotNull<IEnumerable<Decimal?>>(source, nameof(source));
      Decimal num = new Decimal();
      foreach (Decimal? nullable in source)
        num += nullable ?? Decimal.Zero;
      return new Decimal?(num);
    }

    public static Decimal? Sum<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, Decimal?> selector) {
      return source.Select<TSource, Decimal?>(selector).Sum();
    }

    public static Decimal? Average(this IEnumerable<Decimal?> source) {
      Enumerable.CheckNotNull<IEnumerable<Decimal?>>(source, nameof(source));
      Decimal num1 = new Decimal();
      long num2 = 0;
      foreach (Decimal? nullable in source.Where<Decimal?>((Func<Decimal?, bool>) (n => n.HasValue))) {
        num1 += nullable.Value;
        checked {
          ++num2;
        }
      }

      if (num2 == 0L)
        return new Decimal?();
      return new Decimal?(num1 / (Decimal) num2);
    }

    public static Decimal? Average<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, Decimal?> selector) {
      return source.Select<TSource, Decimal?>(selector).Average();
    }

    public static Decimal? Min(this IEnumerable<Decimal?> source) {
      Enumerable.CheckNotNull<IEnumerable<Decimal?>>(source, nameof(source));
      return source.Where<Decimal?>((Func<Decimal?, bool>) (x => x.HasValue)).MinMaxImpl<Decimal>(new Decimal?(),
        (Func<Decimal?, Decimal?, bool>) ((min, x) => {
          Decimal? nullable1 = min;
          Decimal? nullable2 = x;
          if (!(nullable1.GetValueOrDefault() < nullable2.GetValueOrDefault()))
            return false;
          return nullable1.HasValue & nullable2.HasValue;
        }));
    }

    public static Decimal? Min<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, Decimal?> selector) {
      return source.Select<TSource, Decimal?>(selector).Min();
    }

    public static Decimal? Max(this IEnumerable<Decimal?> source) {
      Enumerable.CheckNotNull<IEnumerable<Decimal?>>(source, nameof(source));
      return source.Where<Decimal?>((Func<Decimal?, bool>) (x => x.HasValue)).MinMaxImpl<Decimal>(new Decimal?(),
        (Func<Decimal?, Decimal?, bool>) ((max, x) => {
          if (!x.HasValue)
            return true;
          if (max.HasValue)
            return x.Value < max.Value;
          return false;
        }));
    }

    public static Decimal? Max<TSource>(
      this IEnumerable<TSource> source,
      Func<TSource, Decimal?> selector) {
      return source.Select<TSource, Decimal?>(selector).Max();
    }

    private static class Futures<T> {
      public static readonly Func<T> Default = (Func<T>) (() => default(T));
      public static readonly Func<T> Undefined = (Func<T>) (() => { throw new InvalidOperationException(); });
    }

    private static class Sequence<T> {
      public static readonly IEnumerable<T> Empty = (IEnumerable<T>) new T[0];
    }

    private sealed class Grouping<K, V> : List<V>, IGrouping<K, V>, IEnumerable<V>, IEnumerable {
      internal Grouping(K key) {
        this.Key = key;
      }

      public K Key { get; private set; }
    }
  }
}