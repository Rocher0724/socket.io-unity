using System;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public static class ImmutableStack {
    public static ImmutableStack<T> Create<T>() {
      return ImmutableStack<T>.Empty;
    }

    public static ImmutableStack<T> Create<T>(T item) {
      return ImmutableStack.Create<T>().Push(item);
    }

    public static ImmutableStack<T> Create<T>(IEnumerable<T> items) {
      ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
      foreach (T obj in items)
        immutableStack = immutableStack.Push(obj);
      return immutableStack;
    }

    public static ImmutableStack<T> Create<T>(params T[] items) {
      return ImmutableStack.Create<T>((IEnumerable<T>) items);
    }

    public static IImmutableStack<T> Pop<T>(
      this IImmutableStack<T> stack,
      out T value) {
      if (stack == null)
        throw new ArgumentNullException(nameof(stack));
      value = stack.Peek();
      return stack.Pop();
    }
  }
}