using System;
using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public class ImmutableStack<T> : IImmutableStack<T>, IEnumerable<T>, IEnumerable
  {
    internal static readonly ImmutableStack<T> Empty = new ImmutableStack<T>();
    private readonly T head;
    private readonly ImmutableStack<T> tail;

    internal ImmutableStack()
    {
    }

    private ImmutableStack(T head, ImmutableStack<T> tail)
    {
      this.head = head;
      this.tail = tail;
    }

    public bool IsEmpty
    {
      get
      {
        return this.tail == null;
      }
    }

    public ImmutableStack<T> Clear()
    {
      return ImmutableStack<T>.Empty;
    }

    IImmutableStack<T> IImmutableStack<T>.Clear()
    {
      return (IImmutableStack<T>) ImmutableStack<T>.Empty;
    }

    public T Peek()
    {
      if (this.IsEmpty)
        throw new InvalidOperationException("Stack is empty.");
      return this.head;
    }

    public ImmutableStack<T> Pop()
    {
      if (this.IsEmpty)
        throw new InvalidOperationException("Stack is empty.");
      return this.tail;
    }

    public ImmutableStack<T> Pop(out T value)
    {
      value = this.Peek();
      return this.Pop();
    }

    IImmutableStack<T> IImmutableStack<T>.Pop()
    {
      return (IImmutableStack<T>) this.Pop();
    }

    public ImmutableStack<T> Push(T value)
    {
      return new ImmutableStack<T>(value, this);
    }

    IImmutableStack<T> IImmutableStack<T>.Push(T value)
    {
      return (IImmutableStack<T>) this.Push(value);
    }

    public IEnumerator<T> GetEnumerator()
    {
      return (IEnumerator<T>) new ImmutableStack<T>.Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.GetEnumerator();
    }

    private struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
    {
      private readonly ImmutableStack<T> start;
      private IImmutableStack<T> current;

      public Enumerator(ImmutableStack<T> stack)
      {
        this.start = stack;
        this.current = (IImmutableStack<T>) null;
      }

      bool IEnumerator.MoveNext()
      {
        if (this.current == null)
          this.current = (IImmutableStack<T>) this.start;
        else if (!this.current.IsEmpty)
          this.current = this.current.Pop();
        return !this.current.IsEmpty;
      }

      void IEnumerator.Reset()
      {
        this.current = (IImmutableStack<T>) null;
      }

      object IEnumerator.Current
      {
        get
        {
          return (object) this.Current;
        }
      }

      void IDisposable.Dispose()
      {
      }

      public T Current
      {
        get
        {
          return this.current != null ? this.current.Peek() : default (T);
        }
      }
    }
  }
}
