using System.Collections;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Utilities.LinqBridge {
  internal interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IEnumerable {
    bool Contains(TKey key);

    int Count { get; }

    IEnumerable<TElement> this[TKey key] { get; }
  }
}