using System.Collections;

namespace Socket.Newtonsoft.Json.Utilities {
  internal interface IWrappedCollection : IList, ICollection, IEnumerable
  {
    object UnderlyingCollection { get; }
  }
}
