using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public abstract class JsonContract {
    internal bool IsNullable;
    internal bool IsConvertable;
    internal bool IsEnum;
    internal Type NonNullableUnderlyingType;
    internal ReadType InternalReadType;
    internal JsonContractType ContractType;
    internal bool IsReadOnlyOrFixedSize;
    internal bool IsSealed;
    internal bool IsInstantiable;
    private List<SerializationCallback> _onDeserializedCallbacks;
    private IList<SerializationCallback> _onDeserializingCallbacks;
    private IList<SerializationCallback> _onSerializedCallbacks;
    private IList<SerializationCallback> _onSerializingCallbacks;
    private IList<SerializationErrorCallback> _onErrorCallbacks;
    private Type _createdType;

    public Type UnderlyingType { get; }

    public Type CreatedType {
      get { return this._createdType; }
      set {
        this._createdType = value;
        this.IsSealed = this._createdType.IsSealed();
        this.IsInstantiable = !this._createdType.IsInterface() && !this._createdType.IsAbstract();
      }
    }

    public bool? IsReference { get; set; }

    public JsonConverter Converter { get; set; }

    internal JsonConverter InternalConverter { get; set; }

    public IList<SerializationCallback> OnDeserializedCallbacks {
      get {
        if (this._onDeserializedCallbacks == null)
          this._onDeserializedCallbacks = new List<SerializationCallback>();
        return (IList<SerializationCallback>) this._onDeserializedCallbacks;
      }
    }

    public IList<SerializationCallback> OnDeserializingCallbacks {
      get {
        if (this._onDeserializingCallbacks == null)
          this._onDeserializingCallbacks = (IList<SerializationCallback>) new List<SerializationCallback>();
        return this._onDeserializingCallbacks;
      }
    }

    public IList<SerializationCallback> OnSerializedCallbacks {
      get {
        if (this._onSerializedCallbacks == null)
          this._onSerializedCallbacks = (IList<SerializationCallback>) new List<SerializationCallback>();
        return this._onSerializedCallbacks;
      }
    }

    public IList<SerializationCallback> OnSerializingCallbacks {
      get {
        if (this._onSerializingCallbacks == null)
          this._onSerializingCallbacks = (IList<SerializationCallback>) new List<SerializationCallback>();
        return this._onSerializingCallbacks;
      }
    }

    public IList<SerializationErrorCallback> OnErrorCallbacks {
      get {
        if (this._onErrorCallbacks == null)
          this._onErrorCallbacks = (IList<SerializationErrorCallback>) new List<SerializationErrorCallback>();
        return this._onErrorCallbacks;
      }
    }

    public Func<object> DefaultCreator { get; set; }

    public bool DefaultCreatorNonPublic { get; set; }

    internal JsonContract(Type underlyingType) {
      ValidationUtils.ArgumentNotNull((object) underlyingType, nameof(underlyingType));
      this.UnderlyingType = underlyingType;
      this.IsNullable = ReflectionUtils.IsNullable(underlyingType);
      this.NonNullableUnderlyingType = !this.IsNullable || !ReflectionUtils.IsNullableType(underlyingType)
        ? underlyingType
        : Nullable.GetUnderlyingType(underlyingType);
      this.CreatedType = this.NonNullableUnderlyingType;
      this.IsConvertable = ConvertUtils.IsConvertible(this.NonNullableUnderlyingType);
      this.IsEnum = this.NonNullableUnderlyingType.IsEnum();
      this.InternalReadType = ReadType.Read;
    }

    internal void InvokeOnSerializing(object o, StreamingContext context) {
      if (this._onSerializingCallbacks == null)
        return;
      foreach (SerializationCallback serializingCallback in (IEnumerable<SerializationCallback>) this
        ._onSerializingCallbacks)
        serializingCallback(o, context);
    }

    internal void InvokeOnSerialized(object o, StreamingContext context) {
      if (this._onSerializedCallbacks == null)
        return;
      foreach (SerializationCallback serializedCallback in (IEnumerable<SerializationCallback>) this
        ._onSerializedCallbacks)
        serializedCallback(o, context);
    }

    internal void InvokeOnDeserializing(object o, StreamingContext context) {
      if (this._onDeserializingCallbacks == null)
        return;
      foreach (SerializationCallback deserializingCallback in (IEnumerable<SerializationCallback>) this
        ._onDeserializingCallbacks)
        deserializingCallback(o, context);
    }

    internal void InvokeOnDeserialized(object o, StreamingContext context) {
      if (this._onDeserializedCallbacks == null)
        return;
      foreach (SerializationCallback deserializedCallback in this._onDeserializedCallbacks)
        deserializedCallback(o, context);
    }

    internal void InvokeOnError(object o, StreamingContext context, ErrorContext errorContext) {
      if (this._onErrorCallbacks == null)
        return;
      foreach (SerializationErrorCallback onErrorCallback in (IEnumerable<SerializationErrorCallback>) this
        ._onErrorCallbacks)
        onErrorCallback(o, context, errorContext);
    }

    internal static SerializationCallback CreateSerializationCallback(
      MethodInfo callbackMethodInfo) {
      return (SerializationCallback) ((o, context) => callbackMethodInfo.Invoke(o, new object[1] {
        (object) context
      }));
    }

    internal static SerializationErrorCallback CreateSerializationErrorCallback(
      MethodInfo callbackMethodInfo) {
      return (SerializationErrorCallback) ((o, context, econtext) => callbackMethodInfo.Invoke(o, new object[2] {
        (object) context,
        (object) econtext
      }));
    }
  }
}