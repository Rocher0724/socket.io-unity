using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class ReflectionAttributeProvider : IAttributeProvider {
    private readonly object _attributeProvider;

    public ReflectionAttributeProvider(object attributeProvider) {
      ValidationUtils.ArgumentNotNull(attributeProvider, nameof(attributeProvider));
      this._attributeProvider = attributeProvider;
    }

    public IList<Attribute> GetAttributes(bool inherit) {
      return (IList<Attribute>) ReflectionUtils.GetAttributes(this._attributeProvider, (Type) null, inherit);
    }

    public IList<Attribute> GetAttributes(Type attributeType, bool inherit) {
      return (IList<Attribute>) ReflectionUtils.GetAttributes(this._attributeProvider, attributeType, inherit);
    }
  }
}