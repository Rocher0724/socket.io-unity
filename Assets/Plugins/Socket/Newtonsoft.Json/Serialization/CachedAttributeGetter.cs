using System;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  internal static class CachedAttributeGetter<T> where T : Attribute {
    private static readonly ThreadSafeStore<object, T> TypeAttributeCache =
      new ThreadSafeStore<object, T>(new Func<object, T>(JsonTypeReflector.GetAttribute<T>));

    public static T GetAttribute(object type) {
      return CachedAttributeGetter<T>.TypeAttributeCache.Get(type);
    }
  }
}