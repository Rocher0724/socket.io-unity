using System.Collections;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Utilities.LinqBridge {
  internal interface IGrouping<TKey, TElement> : IEnumerable<TElement>, IEnumerable {
    TKey Key { get; }
  }
}