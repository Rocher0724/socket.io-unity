using System.Collections;
using System.Collections.Generic;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine {
  public class PosList<T> : List<T>, IPosList<T>, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable {
    public int Position { get; set; }
  }
}