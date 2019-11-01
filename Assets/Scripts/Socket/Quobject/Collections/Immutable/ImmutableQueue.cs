using System;
using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public class ImmutableQueue<T> : IImmutableQueue<T>, IEnumerable<T>, IEnumerable
  {
    internal static readonly ImmutableQueue<T> Empty = new ImmutableQueue<T>(ImmutableStack<T>.Empty, ImmutableStack<T>.Empty);
    private readonly ImmutableStack<T> frontStack;
    private readonly ImmutableStack<T> backStack;

    internal ImmutableQueue()
    {
      this.frontStack = this.backStack = ImmutableStack<T>.Empty;
    }

    private ImmutableQueue(ImmutableStack<T> frontStack, ImmutableStack<T> backStack)
    {
      if (frontStack == null)
        throw new ArgumentNullException(nameof (frontStack));
      if (backStack == null)
        throw new ArgumentNullException(nameof (backStack));
      this.frontStack = frontStack;
      this.backStack = backStack;
    }

    public bool IsEmpty
    {
      get
      {
        return this.frontStack.IsEmpty && this.backStack.IsEmpty;
      }
    }

    public ImmutableQueue<T> Clear()
    {
      return ImmutableQueue<T>.Empty;
    }

    IImmutableQueue<T> IImmutableQueue<T>.Clear()
    {
      return (IImmutableQueue<T>) ImmutableQueue<T>.Empty;
    }

    public ImmutableQueue<T> Dequeue()
    {
      if (this.IsEmpty)
        throw new InvalidOperationException("Queue is empty.");
      ImmutableStack<T> frontStack = this.frontStack.Pop();
      if (!frontStack.IsEmpty)
        return new ImmutableQueue<T>(frontStack, this.backStack);
      return new ImmutableQueue<T>(ImmutableQueue<T>.Reverse((IImmutableStack<T>) this.backStack), ImmutableStack<T>.Empty);
    }

    public ImmutableQueue<T> Dequeue(out T value)
    {
      value = this.Peek();
      return this.Dequeue();
    }

    IImmutableQueue<T> IImmutableQueue<T>.Dequeue()
    {
      return (IImmutableQueue<T>) this.Dequeue();
    }

    private static ImmutableStack<T> Reverse(IImmutableStack<T> stack)
    {
      ImmutableStack<T> immutableStack1 = ImmutableStack<T>.Empty;
      for (IImmutableStack<T> immutableStack2 = stack; !immutableStack2.IsEmpty; immutableStack2 = immutableStack2.Pop())
        immutableStack1 = immutableStack1.Push(immutableStack2.Peek());
      return immutableStack1;
    }

    public ImmutableQueue<T> Enqueue(T value)
    {
      if (this.IsEmpty)
        return new ImmutableQueue<T>(ImmutableStack<T>.Empty.Push(value), ImmutableStack<T>.Empty);
      return new ImmutableQueue<T>(this.frontStack, this.backStack.Push(value));
    }

    IImmutableQueue<T> IImmutableQueue<T>.Enqueue(T value)
    {
      return (IImmutableQueue<T>) this.Enqueue(value);
    }

    public T Peek()
    {
      if (this.IsEmpty)
        throw new InvalidOperationException("Queue is empty.");
      return this.frontStack.Peek();
    }

    public IEnumerator<T> GetEnumerator()
    {
      return (IEnumerator<T>) new ImmutableQueue<T>.Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.GetEnumerator();
    }

    private struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
    {
      private readonly ImmutableQueue<T> start;
      private IImmutableStack<T> frontStack;
      private IImmutableStack<T> backStack;

      public Enumerator(ImmutableQueue<T> stack)
      {
        this.start = stack;
        this.frontStack = (IImmutableStack<T>) null;
        this.backStack = (IImmutableStack<T>) null;
      }

      bool IEnumerator.MoveNext()
      {
        if (this.frontStack == null)
        {
          this.frontStack = (IImmutableStack<T>) this.start.frontStack;
          this.backStack = (IImmutableStack<T>) ImmutableQueue<T>.Reverse((IImmutableStack<T>) this.start.backStack);
        }
        else if (!this.frontStack.IsEmpty)
          this.frontStack = this.frontStack.Pop();
        else if (!this.backStack.IsEmpty)
          this.backStack = this.backStack.Pop();
        return !this.frontStack.IsEmpty || !this.backStack.IsEmpty;
      }

      void IEnumerator.Reset()
      {
        this.frontStack = (IImmutableStack<T>) null;
        this.backStack = (IImmutableStack<T>) null;
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
          if (this.frontStack == null)
            return default (T);
          if (this.frontStack.IsEmpty && this.backStack.IsEmpty)
            throw new InvalidOperationException();
          return !this.frontStack.IsEmpty ? this.frontStack.Peek() : this.backStack.Peek();
        }
      }
    }
  }
}
