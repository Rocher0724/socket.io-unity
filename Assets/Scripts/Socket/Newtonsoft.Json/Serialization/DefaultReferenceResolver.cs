using System;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  internal class DefaultReferenceResolver : IReferenceResolver {
    private int _referenceCount;

    private BidirectionalDictionary<string, object> GetMappings(
      object context) {
      JsonSerializerInternalBase serializerInternalBase = context as JsonSerializerInternalBase;
      if (serializerInternalBase == null) {
        JsonSerializerProxy jsonSerializerProxy = context as JsonSerializerProxy;
        if (jsonSerializerProxy == null)
          throw new JsonException("The DefaultReferenceResolver can only be used internally.");
        serializerInternalBase = jsonSerializerProxy.GetInternalSerializer();
      }

      return serializerInternalBase.DefaultReferenceMappings;
    }

    public object ResolveReference(object context, string reference) {
      object second;
      this.GetMappings(context).TryGetByFirst(reference, out second);
      return second;
    }

    public string GetReference(object context, object value) {
      BidirectionalDictionary<string, object> mappings = this.GetMappings(context);
      string first;
      if (!mappings.TryGetBySecond(value, out first)) {
        ++this._referenceCount;
        first = this._referenceCount.ToString((IFormatProvider) CultureInfo.InvariantCulture);
        mappings.Set(first, value);
      }

      return first;
    }

    public void AddReference(object context, string reference, object value) {
      this.GetMappings(context).Set(reference, value);
    }

    public bool IsReferenced(object context, object value) {
      string first;
      return this.GetMappings(context).TryGetBySecond(value, out first);
    }
  }
}