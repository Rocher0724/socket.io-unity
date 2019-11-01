using System.Collections.Generic;

namespace Socket.WebSocket4Net.System.Collections {
  public class ConcurrentQueue<T> {
    private object m_SyncRoot = new object ();
    private Queue<T> m_Queue;

    public ConcurrentQueue () {
      this.m_Queue = new Queue<T> ();
    }

    public ConcurrentQueue (int capacity) {
      this.m_Queue = new Queue<T> (capacity);
    }

    public ConcurrentQueue (IEnumerable<T> collection) {
      this.m_Queue = new Queue<T> (collection);
    }

    public void Enqueue (T item) {
      lock (this.m_SyncRoot)
        this.m_Queue.Enqueue (item);
    }

    public bool TryDequeue (out T item) {
      lock (this.m_SyncRoot) {
        if (this.m_Queue.Count <= 0) {
          item = default (T);
          return false;
        }

        item = this.m_Queue.Dequeue ();
        return true;
      }
    }
  }
}