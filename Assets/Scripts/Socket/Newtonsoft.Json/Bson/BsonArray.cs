using System.Collections;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonArray : BsonToken, IEnumerable<BsonToken>, IEnumerable {
    private readonly List<BsonToken> _children = new List<BsonToken>();

    public void Add(BsonToken token) {
      this._children.Add(token);
      token.Parent = (BsonToken) this;
    }

    public override BsonType Type {
      get { return BsonType.Array; }
    }

    public IEnumerator<BsonToken> GetEnumerator() {
      return (IEnumerator<BsonToken>) this._children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (IEnumerator) this.GetEnumerator();
    }
  }
}
