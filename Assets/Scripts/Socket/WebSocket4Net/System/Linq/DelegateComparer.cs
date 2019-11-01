using System;
using System.Collections.Generic;

namespace Socket.WebSocket4Net.System.Linq {
  internal class DelegateComparer<TSource, TKey> : IComparer<TSource> where TKey : IComparable<TKey> {
    private Func<TSource, TKey> m_Getter;

    public DelegateComparer(Func<TSource, TKey> getter) {
      this.m_Getter = getter;
    }

    public int Compare(TSource x, TSource y) {
      return this.m_Getter(x).CompareTo(this.m_Getter(y));
    }
  }
}