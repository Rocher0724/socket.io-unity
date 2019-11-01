using System.Collections;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonObject : BsonToken, IEnumerable<BsonProperty>, IEnumerable
  {
    private readonly List<BsonProperty> _children = new List<BsonProperty>();

    public void Add(string name, BsonToken token)
    {
      this._children.Add(new BsonProperty()
      {
        Name = new BsonString((object) name, false),
        Value = token
      });
      token.Parent = (BsonToken) this;
    }

    public override BsonType Type
    {
      get
      {
        return BsonType.Object;
      }
    }

    public IEnumerator<BsonProperty> GetEnumerator()
    {
      return (IEnumerator<BsonProperty>) this._children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.GetEnumerator();
    }
  }
}
