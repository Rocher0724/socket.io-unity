using System;
using System.Collections.Generic;
using System.Threading;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public class ConcurrentBatchQueue<T> : IBatchQueue<T> {
    private static readonly T m_Null = default(T);
    private ConcurrentBatchQueue<T>.Entity m_Entity;
    private ConcurrentBatchQueue<T>.Entity m_BackEntity;
    private Func<T, bool> m_NullValidator;
    private bool m_Rebuilding;

    public ConcurrentBatchQueue()
      : this(16) {
    }

    public ConcurrentBatchQueue(int capacity)
      : this(new T[capacity]) {
    }

    public ConcurrentBatchQueue(int capacity, Func<T, bool> nullValidator)
      : this(new T[capacity], nullValidator) {
    }

    public ConcurrentBatchQueue(T[] array)
      : this(array, (Func<T, bool>) (t => (object) t == null)) {
    }

    public ConcurrentBatchQueue(T[] array, Func<T, bool> nullValidator) {
      this.m_Entity = new ConcurrentBatchQueue<T>.Entity();
      this.m_Entity.Array = array;
      this.m_BackEntity = new ConcurrentBatchQueue<T>.Entity();
      this.m_BackEntity.Array = new T[array.Length];
      this.m_NullValidator = nullValidator;
    }

    public bool Enqueue(T item) {
      bool full;
      do
        ;
      while (!this.TryEnqueue(item, out full) && !full);
      return !full;
    }

    private bool TryEnqueue(T item, out bool full) {
      full = false;
      this.EnsureNotRebuild();
      ConcurrentBatchQueue<T>.Entity entity = this.m_Entity;
      T[] array = entity.Array;
      int count = entity.Count;
      if (count >= array.Length) {
        full = true;
        return false;
      }

      if (entity != this.m_Entity || Interlocked.CompareExchange(ref entity.Count, count + 1, count) != count)
        return false;
      array[count] = item;
      return true;
    }

    public bool Enqueue(IList<T> items) {
      bool full;
      do
        ;
      while (!this.TryEnqueue(items, out full) && !full);
      return !full;
    }

    private bool TryEnqueue(IList<T> items, out bool full) {
      full = false;
      ConcurrentBatchQueue<T>.Entity entity = this.m_Entity;
      T[] array = entity.Array;
      int count1 = entity.Count;
      int count2 = items.Count;
      int num = count1 + count2;
      if (num > array.Length) {
        full = true;
        return false;
      }

      if (entity != this.m_Entity || Interlocked.CompareExchange(ref entity.Count, num, count1) != count1)
        return false;
      foreach (T obj in (IEnumerable<T>) items)
        array[count1++] = obj;
      return true;
    }

    private void EnsureNotRebuild() {
      if (!this.m_Rebuilding)
        return;
      do {
        Thread.SpinWait(1);
      } while (this.m_Rebuilding);
    }

    public bool TryDequeue(IList<T> outputItems) {
      ConcurrentBatchQueue<T>.Entity entity = this.m_Entity;
      if (entity.Count <= 0)
        return false;
      Interlocked.Exchange<ConcurrentBatchQueue<T>.Entity>(ref this.m_Entity, this.m_BackEntity);
      Thread.SpinWait(1);
      int count = entity.Count;
      T[] array = entity.Array;
      int index = 0;
      while (true) {
        for (T t = array[index]; this.m_NullValidator(t); t = array[index])
          Thread.SpinWait(1);
        outputItems.Add(array[index]);
        array[index] = ConcurrentBatchQueue<T>.m_Null;
        if (entity.Count > index + 1)
          ++index;
        else
          break;
      }

      this.m_BackEntity = entity;
      this.m_BackEntity.Count = 0;
      return true;
    }

    public bool IsEmpty {
      get { return this.m_Entity.Count <= 0; }
    }

    public int Count {
      get { return this.m_Entity.Count; }
    }

    private class Entity {
      public int Count;

      public T[] Array { get; set; }
    }
  }
}