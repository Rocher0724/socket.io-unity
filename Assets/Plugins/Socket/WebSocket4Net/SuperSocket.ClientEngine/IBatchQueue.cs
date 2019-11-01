using System.Collections.Generic;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public interface IBatchQueue<T> {
    bool Enqueue(T item);

    bool Enqueue(IList<T> items);

    bool TryDequeue(IList<T> outputItems);

    bool IsEmpty { get; }

    int Count { get; }
  }
}