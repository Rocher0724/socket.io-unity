using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Socket.Newtonsoft.Json.Serialization;

namespace Socket.Newtonsoft.Json {
  public class JsonSerializerSettings {
    internal static readonly StreamingContext DefaultContext = new StreamingContext();
    internal static readonly CultureInfo DefaultCulture = CultureInfo.InvariantCulture;
    internal const ReferenceLoopHandling DefaultReferenceLoopHandling = ReferenceLoopHandling.Error;
    internal const MissingMemberHandling DefaultMissingMemberHandling = MissingMemberHandling.Ignore;
    internal const NullValueHandling DefaultNullValueHandling = NullValueHandling.Include;
    internal const DefaultValueHandling DefaultDefaultValueHandling = DefaultValueHandling.Include;
    internal const ObjectCreationHandling DefaultObjectCreationHandling = ObjectCreationHandling.Auto;
    internal const PreserveReferencesHandling DefaultPreserveReferencesHandling = PreserveReferencesHandling.None;
    internal const ConstructorHandling DefaultConstructorHandling = ConstructorHandling.Default;
    internal const TypeNameHandling DefaultTypeNameHandling = TypeNameHandling.None;
    internal const MetadataPropertyHandling DefaultMetadataPropertyHandling = MetadataPropertyHandling.Default;
    internal const Formatting DefaultFormatting = Formatting.None;
    internal const DateFormatHandling DefaultDateFormatHandling = DateFormatHandling.IsoDateFormat;
    internal const DateTimeZoneHandling DefaultDateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
    internal const DateParseHandling DefaultDateParseHandling = DateParseHandling.DateTime;
    internal const FloatParseHandling DefaultFloatParseHandling = FloatParseHandling.Double;
    internal const FloatFormatHandling DefaultFloatFormatHandling = FloatFormatHandling.String;
    internal const StringEscapeHandling DefaultStringEscapeHandling = StringEscapeHandling.Default;

    internal const TypeNameAssemblyFormatHandling DefaultTypeNameAssemblyFormatHandling =
      TypeNameAssemblyFormatHandling.Simple;

    internal const bool DefaultCheckAdditionalContent = false;
    internal const string DefaultDateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
    internal Formatting? _formatting;
    internal DateFormatHandling? _dateFormatHandling;
    internal DateTimeZoneHandling? _dateTimeZoneHandling;
    internal DateParseHandling? _dateParseHandling;
    internal FloatFormatHandling? _floatFormatHandling;
    internal FloatParseHandling? _floatParseHandling;
    internal StringEscapeHandling? _stringEscapeHandling;
    internal CultureInfo _culture;
    internal bool? _checkAdditionalContent;
    internal int? _maxDepth;
    internal bool _maxDepthSet;
    internal string _dateFormatString;
    internal bool _dateFormatStringSet;
    internal TypeNameAssemblyFormatHandling? _typeNameAssemblyFormatHandling;
    internal DefaultValueHandling? _defaultValueHandling;
    internal PreserveReferencesHandling? _preserveReferencesHandling;
    internal NullValueHandling? _nullValueHandling;
    internal ObjectCreationHandling? _objectCreationHandling;
    internal MissingMemberHandling? _missingMemberHandling;
    internal ReferenceLoopHandling? _referenceLoopHandling;
    internal StreamingContext? _context;
    internal ConstructorHandling? _constructorHandling;
    internal TypeNameHandling? _typeNameHandling;
    internal MetadataPropertyHandling? _metadataPropertyHandling;

    public ReferenceLoopHandling ReferenceLoopHandling {
      get { return this._referenceLoopHandling ?? ReferenceLoopHandling.Error; }
      set { this._referenceLoopHandling = new ReferenceLoopHandling?(value); }
    }

    public MissingMemberHandling MissingMemberHandling {
      get { return this._missingMemberHandling ?? MissingMemberHandling.Ignore; }
      set { this._missingMemberHandling = new MissingMemberHandling?(value); }
    }

    public ObjectCreationHandling ObjectCreationHandling {
      get { return this._objectCreationHandling ?? ObjectCreationHandling.Auto; }
      set { this._objectCreationHandling = new ObjectCreationHandling?(value); }
    }

    public NullValueHandling NullValueHandling {
      get { return this._nullValueHandling ?? NullValueHandling.Include; }
      set { this._nullValueHandling = new NullValueHandling?(value); }
    }

    public DefaultValueHandling DefaultValueHandling {
      get { return this._defaultValueHandling ?? DefaultValueHandling.Include; }
      set { this._defaultValueHandling = new DefaultValueHandling?(value); }
    }

    public IList<JsonConverter> Converters { get; set; }

    public PreserveReferencesHandling PreserveReferencesHandling {
      get { return this._preserveReferencesHandling ?? PreserveReferencesHandling.None; }
      set { this._preserveReferencesHandling = new PreserveReferencesHandling?(value); }
    }

    public TypeNameHandling TypeNameHandling {
      get { return this._typeNameHandling ?? TypeNameHandling.None; }
      set { this._typeNameHandling = new TypeNameHandling?(value); }
    }

    public MetadataPropertyHandling MetadataPropertyHandling {
      get { return this._metadataPropertyHandling ?? MetadataPropertyHandling.Default; }
      set { this._metadataPropertyHandling = new MetadataPropertyHandling?(value); }
    }

    [Obsolete("TypeNameAssemblyFormat is obsolete. Use TypeNameAssemblyFormatHandling instead.")]
    public FormatterAssemblyStyle TypeNameAssemblyFormat {
      get { return (FormatterAssemblyStyle) this.TypeNameAssemblyFormatHandling; }
      set { this.TypeNameAssemblyFormatHandling = (TypeNameAssemblyFormatHandling) value; }
    }

    public TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling {
      get { return this._typeNameAssemblyFormatHandling ?? TypeNameAssemblyFormatHandling.Simple; }
      set { this._typeNameAssemblyFormatHandling = new TypeNameAssemblyFormatHandling?(value); }
    }

    public ConstructorHandling ConstructorHandling {
      get { return this._constructorHandling ?? ConstructorHandling.Default; }
      set { this._constructorHandling = new ConstructorHandling?(value); }
    }

    public IContractResolver ContractResolver { get; set; }

    public IEqualityComparer EqualityComparer { get; set; }

    [Obsolete(
      "ReferenceResolver property is obsolete. Use the ReferenceResolverProvider property to set the IReferenceResolver: settings.ReferenceResolverProvider = () => resolver")]
    public IReferenceResolver ReferenceResolver {
      get {
        Func<IReferenceResolver> resolverProvider = this.ReferenceResolverProvider;
        if (resolverProvider == null)
          return (IReferenceResolver) null;
        return resolverProvider();
      }
      set {
        this.ReferenceResolverProvider =
          value != null ? (Func<IReferenceResolver>) (() => value) : (Func<IReferenceResolver>) null;
      }
    }

    public Func<IReferenceResolver> ReferenceResolverProvider { get; set; }

    public ITraceWriter TraceWriter { get; set; }

    [Obsolete("Binder is obsolete. Use SerializationBinder instead.")]
    public System.Runtime.Serialization.SerializationBinder Binder {
      get {
        if (this.SerializationBinder == null)
          return (System.Runtime.Serialization.SerializationBinder) null;
        SerializationBinderAdapter serializationBinder = this.SerializationBinder as SerializationBinderAdapter;
        if (serializationBinder != null)
          return serializationBinder.SerializationBinder;
        throw new InvalidOperationException(
          "Cannot get SerializationBinder because an ISerializationBinder was previously set.");
      }
      set {
        this.SerializationBinder = value == null
          ? (ISerializationBinder) null
          : (ISerializationBinder) new SerializationBinderAdapter(value);
      }
    }

    public ISerializationBinder SerializationBinder { get; set; }

    public EventHandler<ErrorEventArgs> Error { get; set; }

    public StreamingContext Context {
      get { return this._context ?? JsonSerializerSettings.DefaultContext; }
      set { this._context = new StreamingContext?(value); }
    }

    public string DateFormatString {
      get { return this._dateFormatString ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"; }
      set {
        this._dateFormatString = value;
        this._dateFormatStringSet = true;
      }
    }

    public int? MaxDepth {
      get { return this._maxDepth; }
      set {
        int? nullable = value;
        int num = 0;
        if ((nullable.GetValueOrDefault() <= num ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          throw new ArgumentException("Value must be positive.", nameof(value));
        this._maxDepth = value;
        this._maxDepthSet = true;
      }
    }

    public Formatting Formatting {
      get { return this._formatting ?? Formatting.None; }
      set { this._formatting = new Formatting?(value); }
    }

    public DateFormatHandling DateFormatHandling {
      get { return this._dateFormatHandling ?? DateFormatHandling.IsoDateFormat; }
      set { this._dateFormatHandling = new DateFormatHandling?(value); }
    }

    public DateTimeZoneHandling DateTimeZoneHandling {
      get { return this._dateTimeZoneHandling ?? DateTimeZoneHandling.RoundtripKind; }
      set { this._dateTimeZoneHandling = new DateTimeZoneHandling?(value); }
    }

    public DateParseHandling DateParseHandling {
      get { return this._dateParseHandling ?? DateParseHandling.DateTime; }
      set { this._dateParseHandling = new DateParseHandling?(value); }
    }

    public FloatFormatHandling FloatFormatHandling {
      get { return this._floatFormatHandling ?? FloatFormatHandling.String; }
      set { this._floatFormatHandling = new FloatFormatHandling?(value); }
    }

    public FloatParseHandling FloatParseHandling {
      get { return this._floatParseHandling ?? FloatParseHandling.Double; }
      set { this._floatParseHandling = new FloatParseHandling?(value); }
    }

    public StringEscapeHandling StringEscapeHandling {
      get { return this._stringEscapeHandling ?? StringEscapeHandling.Default; }
      set { this._stringEscapeHandling = new StringEscapeHandling?(value); }
    }

    public CultureInfo Culture {
      get { return this._culture ?? JsonSerializerSettings.DefaultCulture; }
      set { this._culture = value; }
    }

    public bool CheckAdditionalContent {
      get { return this._checkAdditionalContent ?? false; }
      set { this._checkAdditionalContent = new bool?(value); }
    }

    public JsonSerializerSettings() {
      this.Converters = (IList<JsonConverter>) new List<JsonConverter>();
    }
  }
}