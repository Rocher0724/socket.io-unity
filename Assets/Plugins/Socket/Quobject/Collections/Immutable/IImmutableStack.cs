using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public interface IImmutableStack<T> : IEnumerable<T>, IEnumerable {
    bool IsEmpty { get; }

    IImmutableStack<T> Clear();

    T Peek();

    IImmutableStack<T> Pop();

    IImmutableStack<T> Push(T value);
  }
}