
using System;
using System.Collections;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Utilities.LinqBridge {
  internal sealed class OrderedEnumerable<T, K> : IOrderedEnumerable<T>, IEnumerable<T>, IEnumerable {
    private readonly IEnumerable<T> _source;
    private readonly List<Comparison<T>> _comparisons;

    public OrderedEnumerable(
      IEnumerable<T> source,
      Func<T, K> keySelector,
      IComparer<K> comparer,
      bool descending)
      : this(source, (List<Comparison<T>>) null, keySelector, comparer, descending) {
    }

    private OrderedEnumerable(
      IEnumerable<T> source,
      List<Comparison<T>> comparisons,
      Func<T, K> keySelector,
      IComparer<K> comparer,
      bool descending) {
      if (source == null)
        throw new ArgumentNullException(nameof(source));
      if (keySelector == null)
        throw new ArgumentNullException(nameof(keySelector));
      this._source = source;
      comparer = comparer ?? (IComparer<K>) Comparer<K>.Default;
      if (comparisons == null)
        comparisons = new List<Comparison<T>>(4);
      comparisons.Add((Comparison<T>) ((x, y) =>
        (descending ? -1 : 1) * comparer.Compare(keySelector(x), keySelector(y))));
      this._comparisons = comparisons;
    }

    public IOrderedEnumerable<T> CreateOrderedEnumerable<KK>(
      Func<T, KK> keySelector,
      IComparer<KK> comparer,
      bool descending) {
      return (IOrderedEnumerable<T>) new OrderedEnumerable<T, KK>(this._source, this._comparisons, keySelector,
        comparer, descending);
    }

    public IEnumerator<T> GetEnumerator() {
      List<Tuple<T, int>> list = this._source
        .Select<T, Tuple<T, int>>(new Func<T, int, Tuple<T, int>>(OrderedEnumerable<T, K>.TagPosition))
        .ToList<Tuple<T, int>>();
      list.Sort((Comparison<Tuple<T, int>>) ((x, y) => {
        List<Comparison<T>> comparisons = this._comparisons;
        for (int index = 0; index < comparisons.Count; ++index) {
          int num = comparisons[index](x.Item1, y.Item1);
          if (num != 0)
            return num;
        }

        return x.Item2.CompareTo(y.Item2);
      }));
      return list.Select<Tuple<T, int>, T>(new Func<Tuple<T, int>, T>(OrderedEnumerable<T, K>.GetFirst))
        .GetEnumerator();
    }

    private static Tuple<T, int> TagPosition(T e, int i) {
      return new Tuple<T, int>(e, i);
    }

    private static T GetFirst(Tuple<T, int> pv) {
      return pv.Item1;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (IEnumerator) this.GetEnumerator();
    }
  }
}