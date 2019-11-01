using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public interface IImmutableQueue<T> : IEnumerable<T>, IEnumerable
  {
    bool IsEmpty { get; }

    IImmutableQueue<T> Clear();

    IImmutableQueue<T> Dequeue();

    IImmutableQueue<T> Enqueue(T value);

    T Peek();
  }
}
