using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
public class JsonObjectContract : JsonContainerContract
  {
    internal bool ExtensionDataIsJToken;
    private bool? _hasRequiredOrDefaultValueProperties;
    private ObjectConstructor<object> _overrideCreator;
    private ObjectConstructor<object> _parameterizedCreator;
    private JsonPropertyCollection _creatorParameters;
    private Type _extensionDataValueType;

    public MemberSerialization MemberSerialization { get; set; }

    public Required? ItemRequired { get; set; }

    public JsonPropertyCollection Properties { get; }

    public JsonPropertyCollection CreatorParameters
    {
      get
      {
        if (this._creatorParameters == null)
          this._creatorParameters = new JsonPropertyCollection(this.UnderlyingType);
        return this._creatorParameters;
      }
    }

    public ObjectConstructor<object> OverrideCreator
    {
      get
      {
        return this._overrideCreator;
      }
      set
      {
        this._overrideCreator = value;
      }
    }

    internal ObjectConstructor<object> ParameterizedCreator
    {
      get
      {
        return this._parameterizedCreator;
      }
      set
      {
        this._parameterizedCreator = value;
      }
    }

    public ExtensionDataSetter ExtensionDataSetter { get; set; }

    public ExtensionDataGetter ExtensionDataGetter { get; set; }

    public Type ExtensionDataValueType
    {
      get
      {
        return this._extensionDataValueType;
      }
      set
      {
        this._extensionDataValueType = value;
        this.ExtensionDataIsJToken = value != null && typeof (JToken).IsAssignableFrom(value);
      }
    }

    public Func<string, string> ExtensionDataNameResolver { get; set; }

    internal bool HasRequiredOrDefaultValueProperties
    {
      get
      {
        if (!this._hasRequiredOrDefaultValueProperties.HasValue)
        {
          this._hasRequiredOrDefaultValueProperties = new bool?(false);
          if (this.ItemRequired.GetValueOrDefault(Required.Default) != Required.Default)
          {
            this._hasRequiredOrDefaultValueProperties = new bool?(true);
          }
          else
          {
            foreach (JsonProperty property in (Collection<JsonProperty>) this.Properties)
            {
              if (property.Required == Required.Default)
              {
                DefaultValueHandling? defaultValueHandling1 = property.DefaultValueHandling;
                DefaultValueHandling? nullable = defaultValueHandling1.HasValue ? new DefaultValueHandling?(defaultValueHandling1.GetValueOrDefault() & DefaultValueHandling.Populate) : new DefaultValueHandling?();
                DefaultValueHandling defaultValueHandling2 = DefaultValueHandling.Populate;
                if ((nullable.GetValueOrDefault() == defaultValueHandling2 ? (nullable.HasValue ? 1 : 0) : 0) == 0)
                  continue;
              }
              this._hasRequiredOrDefaultValueProperties = new bool?(true);
              break;
            }
          }
        }
        return this._hasRequiredOrDefaultValueProperties.GetValueOrDefault();
      }
    }

    public JsonObjectContract(Type underlyingType)
      : base(underlyingType)
    {
      this.ContractType = JsonContractType.Object;
      this.Properties = new JsonPropertyCollection(this.UnderlyingType);
    }

    internal object GetUninitializedObject()
    {
      if (!JsonTypeReflector.FullyTrusted)
        throw new JsonException("Insufficient permissions. Creating an uninitialized '{0}' type requires full trust.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) this.NonNullableUnderlyingType));
      return FormatterServices.GetUninitializedObject(this.NonNullableUnderlyingType);
    }
  }
}
