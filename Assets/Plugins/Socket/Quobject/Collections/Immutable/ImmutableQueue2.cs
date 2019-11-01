using System;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public static class ImmutableQueue {
    public static ImmutableQueue<T> Create<T>() {
      return ImmutableQueue<T>.Empty;
    }

    public static ImmutableQueue<T> Create<T>(T item) {
      return Create<T>().Enqueue(item);
    }

    public static ImmutableQueue<T> Create<T>(IEnumerable<T> items) {
      ImmutableQueue<T> immutableQueue = ImmutableQueue<T>.Empty;
      foreach (T obj in items)
        immutableQueue = immutableQueue.Enqueue(obj);
      return immutableQueue;
    }

    public static ImmutableQueue<T> Create<T>(params T[] items) {
      return Create<T>((IEnumerable<T>) items);
    }

    public static IImmutableQueue<T> Dequeue<T>(
      this IImmutableQueue<T> queue,
      out T value) {
      if (queue == null)
        throw new ArgumentNullException(nameof(queue));
      value = queue.Peek();
      return queue.Dequeue();
    }
  }
}