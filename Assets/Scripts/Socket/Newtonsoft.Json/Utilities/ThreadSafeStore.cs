using System;
using System.Collections.Generic;
using System.Threading;

namespace Socket.Newtonsoft.Json.Utilities {
  internal class ThreadSafeStore<TKey, TValue> {
    private readonly object _lock = new object();
    private Dictionary<TKey, TValue> _store;
    private readonly Func<TKey, TValue> _creator;

    public ThreadSafeStore(Func<TKey, TValue> creator) {
      if (creator == null)
        throw new ArgumentNullException(nameof(creator));
      this._creator = creator;
      this._store = new Dictionary<TKey, TValue>();
    }

    public TValue Get(TKey key) {
      TValue obj;
      if (!this._store.TryGetValue(key, out obj))
        return this.AddValue(key);
      return obj;
    }

    private TValue AddValue(TKey key) {
      TValue obj1 = this._creator(key);
      lock (this._lock) {
        if (this._store == null) {
          this._store = new Dictionary<TKey, TValue>();
          this._store[key] = obj1;
        } else {
          TValue obj2;
          if (this._store.TryGetValue(key, out obj2))
            return obj2;
          Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>((IDictionary<TKey, TValue>) this._store);
          dictionary[key] = obj1;
          Thread.MemoryBarrier();
          this._store = dictionary;
        }

        return obj1;
      }
    }
  }
}