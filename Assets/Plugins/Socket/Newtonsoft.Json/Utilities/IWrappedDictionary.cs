using System.Collections;

namespace Socket.Newtonsoft.Json.Utilities {
  internal interface IWrappedDictionary : IDictionary, ICollection, IEnumerable
  {
    object UnderlyingDictionary { get; }
  }
}
