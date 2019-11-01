using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Socket.Newtonsoft.Json.Serialization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json {
public class JsonSerializer
  {
    internal TypeNameHandling _typeNameHandling;
    internal TypeNameAssemblyFormatHandling _typeNameAssemblyFormatHandling;
    internal PreserveReferencesHandling _preserveReferencesHandling;
    internal ReferenceLoopHandling _referenceLoopHandling;
    internal MissingMemberHandling _missingMemberHandling;
    internal ObjectCreationHandling _objectCreationHandling;
    internal NullValueHandling _nullValueHandling;
    internal DefaultValueHandling _defaultValueHandling;
    internal ConstructorHandling _constructorHandling;
    internal MetadataPropertyHandling _metadataPropertyHandling;
    internal JsonConverterCollection _converters;
    internal IContractResolver _contractResolver;
    internal ITraceWriter _traceWriter;
    internal IEqualityComparer _equalityComparer;
    internal ISerializationBinder _serializationBinder;
    internal StreamingContext _context;
    private IReferenceResolver _referenceResolver;
    private Formatting? _formatting;
    private DateFormatHandling? _dateFormatHandling;
    private DateTimeZoneHandling? _dateTimeZoneHandling;
    private DateParseHandling? _dateParseHandling;
    private FloatFormatHandling? _floatFormatHandling;
    private FloatParseHandling? _floatParseHandling;
    private StringEscapeHandling? _stringEscapeHandling;
    private CultureInfo _culture;
    private int? _maxDepth;
    private bool _maxDepthSet;
    private bool? _checkAdditionalContent;
    private string _dateFormatString;
    private bool _dateFormatStringSet;

    public virtual event EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> Error;

    public virtual IReferenceResolver ReferenceResolver
    {
      get
      {
        return this.GetReferenceResolver();
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException(nameof (value), "Reference resolver cannot be null.");
        this._referenceResolver = value;
      }
    }

    [Obsolete("Binder is obsolete. Use SerializationBinder instead.")]
    public virtual System.Runtime.Serialization.SerializationBinder Binder
    {
      get
      {
        if (this._serializationBinder == null)
          return (System.Runtime.Serialization.SerializationBinder) null;
        SerializationBinderAdapter serializationBinder = this._serializationBinder as SerializationBinderAdapter;
        if (serializationBinder != null)
          return serializationBinder.SerializationBinder;
        throw new InvalidOperationException("Cannot get SerializationBinder because an ISerializationBinder was previously set.");
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException(nameof (value), "Serialization binder cannot be null.");
        this._serializationBinder = (ISerializationBinder) new SerializationBinderAdapter(value);
      }
    }

    public virtual ISerializationBinder SerializationBinder
    {
      get
      {
        return this._serializationBinder;
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException(nameof (value), "Serialization binder cannot be null.");
        this._serializationBinder = value;
      }
    }

    public virtual ITraceWriter TraceWriter
    {
      get
      {
        return this._traceWriter;
      }
      set
      {
        this._traceWriter = value;
      }
    }

    public virtual IEqualityComparer EqualityComparer
    {
      get
      {
        return this._equalityComparer;
      }
      set
      {
        this._equalityComparer = value;
      }
    }

    public virtual TypeNameHandling TypeNameHandling
    {
      get
      {
        return this._typeNameHandling;
      }
      set
      {
        switch (value)
        {
          case TypeNameHandling.None:
          case TypeNameHandling.Objects:
          case TypeNameHandling.Arrays:
          case TypeNameHandling.All:
          case TypeNameHandling.Auto:
            this._typeNameHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    [Obsolete("TypeNameAssemblyFormat is obsolete. Use TypeNameAssemblyFormatHandling instead.")]
    public virtual FormatterAssemblyStyle TypeNameAssemblyFormat
    {
      get
      {
        return (FormatterAssemblyStyle) this._typeNameAssemblyFormatHandling;
      }
      set
      {
        switch (value)
        {
          case FormatterAssemblyStyle.Simple:
          case FormatterAssemblyStyle.Full:
            this._typeNameAssemblyFormatHandling = (TypeNameAssemblyFormatHandling) value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling
    {
      get
      {
        return this._typeNameAssemblyFormatHandling;
      }
      set
      {
        switch (value)
        {
          case TypeNameAssemblyFormatHandling.Simple:
          case TypeNameAssemblyFormatHandling.Full:
            this._typeNameAssemblyFormatHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual PreserveReferencesHandling PreserveReferencesHandling
    {
      get
      {
        return this._preserveReferencesHandling;
      }
      set
      {
        switch (value)
        {
          case PreserveReferencesHandling.None:
          case PreserveReferencesHandling.Objects:
          case PreserveReferencesHandling.Arrays:
          case PreserveReferencesHandling.All:
            this._preserveReferencesHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual ReferenceLoopHandling ReferenceLoopHandling
    {
      get
      {
        return this._referenceLoopHandling;
      }
      set
      {
        switch (value)
        {
          case ReferenceLoopHandling.Error:
          case ReferenceLoopHandling.Ignore:
          case ReferenceLoopHandling.Serialize:
            this._referenceLoopHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual MissingMemberHandling MissingMemberHandling
    {
      get
      {
        return this._missingMemberHandling;
      }
      set
      {
        switch (value)
        {
          case MissingMemberHandling.Ignore:
          case MissingMemberHandling.Error:
            this._missingMemberHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual NullValueHandling NullValueHandling
    {
      get
      {
        return this._nullValueHandling;
      }
      set
      {
        switch (value)
        {
          case NullValueHandling.Include:
          case NullValueHandling.Ignore:
            this._nullValueHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual DefaultValueHandling DefaultValueHandling
    {
      get
      {
        return this._defaultValueHandling;
      }
      set
      {
        switch (value)
        {
          case DefaultValueHandling.Include:
          case DefaultValueHandling.Ignore:
          case DefaultValueHandling.Populate:
          case DefaultValueHandling.IgnoreAndPopulate:
            this._defaultValueHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual ObjectCreationHandling ObjectCreationHandling
    {
      get
      {
        return this._objectCreationHandling;
      }
      set
      {
        switch (value)
        {
          case ObjectCreationHandling.Auto:
          case ObjectCreationHandling.Reuse:
          case ObjectCreationHandling.Replace:
            this._objectCreationHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual ConstructorHandling ConstructorHandling
    {
      get
      {
        return this._constructorHandling;
      }
      set
      {
        switch (value)
        {
          case ConstructorHandling.Default:
          case ConstructorHandling.AllowNonPublicDefaultConstructor:
            this._constructorHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual MetadataPropertyHandling MetadataPropertyHandling
    {
      get
      {
        return this._metadataPropertyHandling;
      }
      set
      {
        switch (value)
        {
          case MetadataPropertyHandling.Default:
          case MetadataPropertyHandling.ReadAhead:
          case MetadataPropertyHandling.Ignore:
            this._metadataPropertyHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public virtual JsonConverterCollection Converters
    {
      get
      {
        if (this._converters == null)
          this._converters = new JsonConverterCollection();
        return this._converters;
      }
    }

    public virtual IContractResolver ContractResolver
    {
      get
      {
        return this._contractResolver;
      }
      set
      {
        this._contractResolver = value ?? DefaultContractResolver.Instance;
      }
    }

    public virtual StreamingContext Context
    {
      get
      {
        return this._context;
      }
      set
      {
        this._context = value;
      }
    }

    public virtual Formatting Formatting
    {
      get
      {
        return this._formatting ?? Formatting.None;
      }
      set
      {
        this._formatting = new Formatting?(value);
      }
    }

    public virtual DateFormatHandling DateFormatHandling
    {
      get
      {
        return this._dateFormatHandling ?? DateFormatHandling.IsoDateFormat;
      }
      set
      {
        this._dateFormatHandling = new DateFormatHandling?(value);
      }
    }

    public virtual DateTimeZoneHandling DateTimeZoneHandling
    {
      get
      {
        return this._dateTimeZoneHandling ?? DateTimeZoneHandling.RoundtripKind;
      }
      set
      {
        this._dateTimeZoneHandling = new DateTimeZoneHandling?(value);
      }
    }

    public virtual DateParseHandling DateParseHandling
    {
      get
      {
        return this._dateParseHandling ?? DateParseHandling.DateTime;
      }
      set
      {
        this._dateParseHandling = new DateParseHandling?(value);
      }
    }

    public virtual FloatParseHandling FloatParseHandling
    {
      get
      {
        return this._floatParseHandling ?? FloatParseHandling.Double;
      }
      set
      {
        this._floatParseHandling = new FloatParseHandling?(value);
      }
    }

    public virtual FloatFormatHandling FloatFormatHandling
    {
      get
      {
        return this._floatFormatHandling ?? FloatFormatHandling.String;
      }
      set
      {
        this._floatFormatHandling = new FloatFormatHandling?(value);
      }
    }

    public virtual StringEscapeHandling StringEscapeHandling
    {
      get
      {
        return this._stringEscapeHandling ?? StringEscapeHandling.Default;
      }
      set
      {
        this._stringEscapeHandling = new StringEscapeHandling?(value);
      }
    }

    public virtual string DateFormatString
    {
      get
      {
        return this._dateFormatString ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
      }
      set
      {
        this._dateFormatString = value;
        this._dateFormatStringSet = true;
      }
    }

    public virtual CultureInfo Culture
    {
      get
      {
        return this._culture ?? JsonSerializerSettings.DefaultCulture;
      }
      set
      {
        this._culture = value;
      }
    }

    public virtual int? MaxDepth
    {
      get
      {
        return this._maxDepth;
      }
      set
      {
        int? nullable = value;
        int num = 0;
        if ((nullable.GetValueOrDefault() <= num ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          throw new ArgumentException("Value must be positive.", nameof (value));
        this._maxDepth = value;
        this._maxDepthSet = true;
      }
    }

    public virtual bool CheckAdditionalContent
    {
      get
      {
        return this._checkAdditionalContent ?? false;
      }
      set
      {
        this._checkAdditionalContent = new bool?(value);
      }
    }

    internal bool IsCheckAdditionalContentSet()
    {
      return this._checkAdditionalContent.HasValue;
    }

    public JsonSerializer()
    {
      this._referenceLoopHandling = ReferenceLoopHandling.Error;
      this._missingMemberHandling = MissingMemberHandling.Ignore;
      this._nullValueHandling = NullValueHandling.Include;
      this._defaultValueHandling = DefaultValueHandling.Include;
      this._objectCreationHandling = ObjectCreationHandling.Auto;
      this._preserveReferencesHandling = PreserveReferencesHandling.None;
      this._constructorHandling = ConstructorHandling.Default;
      this._typeNameHandling = TypeNameHandling.None;
      this._metadataPropertyHandling = MetadataPropertyHandling.Default;
      this._context = JsonSerializerSettings.DefaultContext;
      this._serializationBinder = (ISerializationBinder) DefaultSerializationBinder.Instance;
      this._culture = JsonSerializerSettings.DefaultCulture;
      this._contractResolver = DefaultContractResolver.Instance;
    }

    public static JsonSerializer Create()
    {
      return new JsonSerializer();
    }

    public static JsonSerializer Create(JsonSerializerSettings settings)
    {
      JsonSerializer serializer = JsonSerializer.Create();
      if (settings != null)
        JsonSerializer.ApplySerializerSettings(serializer, settings);
      return serializer;
    }

    public static JsonSerializer CreateDefault()
    {
      Func<JsonSerializerSettings> defaultSettings = JsonConvert.DefaultSettings;
      return JsonSerializer.Create(defaultSettings != null ? defaultSettings() : (JsonSerializerSettings) null);
    }

    public static JsonSerializer CreateDefault(JsonSerializerSettings settings)
    {
      JsonSerializer serializer = JsonSerializer.CreateDefault();
      if (settings != null)
        JsonSerializer.ApplySerializerSettings(serializer, settings);
      return serializer;
    }

    private static void ApplySerializerSettings(
      JsonSerializer serializer,
      JsonSerializerSettings settings)
    {
      if (!CollectionUtils.IsNullOrEmpty<JsonConverter>((ICollection<JsonConverter>) settings.Converters))
      {
        for (int index = 0; index < settings.Converters.Count; ++index)
          serializer.Converters.Insert(index, settings.Converters[index]);
      }
      if (settings._typeNameHandling.HasValue)
        serializer.TypeNameHandling = settings.TypeNameHandling;
      if (settings._metadataPropertyHandling.HasValue)
        serializer.MetadataPropertyHandling = settings.MetadataPropertyHandling;
      if (settings._typeNameAssemblyFormatHandling.HasValue)
        serializer.TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling;
      if (settings._preserveReferencesHandling.HasValue)
        serializer.PreserveReferencesHandling = settings.PreserveReferencesHandling;
      if (settings._referenceLoopHandling.HasValue)
        serializer.ReferenceLoopHandling = settings.ReferenceLoopHandling;
      if (settings._missingMemberHandling.HasValue)
        serializer.MissingMemberHandling = settings.MissingMemberHandling;
      if (settings._objectCreationHandling.HasValue)
        serializer.ObjectCreationHandling = settings.ObjectCreationHandling;
      if (settings._nullValueHandling.HasValue)
        serializer.NullValueHandling = settings.NullValueHandling;
      if (settings._defaultValueHandling.HasValue)
        serializer.DefaultValueHandling = settings.DefaultValueHandling;
      if (settings._constructorHandling.HasValue)
        serializer.ConstructorHandling = settings.ConstructorHandling;
      if (settings._context.HasValue)
        serializer.Context = settings.Context;
      if (settings._checkAdditionalContent.HasValue)
        serializer._checkAdditionalContent = settings._checkAdditionalContent;
      if (settings.Error != null)
        serializer.Error += settings.Error;
      if (settings.ContractResolver != null)
        serializer.ContractResolver = settings.ContractResolver;
      if (settings.ReferenceResolverProvider != null)
        serializer.ReferenceResolver = settings.ReferenceResolverProvider();
      if (settings.TraceWriter != null)
        serializer.TraceWriter = settings.TraceWriter;
      if (settings.EqualityComparer != null)
        serializer.EqualityComparer = settings.EqualityComparer;
      if (settings.SerializationBinder != null)
        serializer.SerializationBinder = settings.SerializationBinder;
      if (settings._formatting.HasValue)
        serializer._formatting = settings._formatting;
      if (settings._dateFormatHandling.HasValue)
        serializer._dateFormatHandling = settings._dateFormatHandling;
      if (settings._dateTimeZoneHandling.HasValue)
        serializer._dateTimeZoneHandling = settings._dateTimeZoneHandling;
      if (settings._dateParseHandling.HasValue)
        serializer._dateParseHandling = settings._dateParseHandling;
      if (settings._dateFormatStringSet)
      {
        serializer._dateFormatString = settings._dateFormatString;
        serializer._dateFormatStringSet = settings._dateFormatStringSet;
      }
      if (settings._floatFormatHandling.HasValue)
        serializer._floatFormatHandling = settings._floatFormatHandling;
      if (settings._floatParseHandling.HasValue)
        serializer._floatParseHandling = settings._floatParseHandling;
      if (settings._stringEscapeHandling.HasValue)
        serializer._stringEscapeHandling = settings._stringEscapeHandling;
      if (settings._culture != null)
        serializer._culture = settings._culture;
      if (!settings._maxDepthSet)
        return;
      serializer._maxDepth = settings._maxDepth;
      serializer._maxDepthSet = settings._maxDepthSet;
    }

    public void Populate(TextReader reader, object target)
    {
      this.Populate((JsonReader) new JsonTextReader(reader), target);
    }

    public void Populate(JsonReader reader, object target)
    {
      this.PopulateInternal(reader, target);
    }

    internal virtual void PopulateInternal(JsonReader reader, object target)
    {
      ValidationUtils.ArgumentNotNull((object) reader, nameof (reader));
      ValidationUtils.ArgumentNotNull(target, nameof (target));
      CultureInfo previousCulture;
      DateTimeZoneHandling? previousDateTimeZoneHandling;
      DateParseHandling? previousDateParseHandling;
      FloatParseHandling? previousFloatParseHandling;
      int? previousMaxDepth;
      string previousDateFormatString;
      this.SetupReader(reader, out previousCulture, out previousDateTimeZoneHandling, out previousDateParseHandling, out previousFloatParseHandling, out previousMaxDepth, out previousDateFormatString);
      TraceJsonReader traceJsonReader = this.TraceWriter == null || this.TraceWriter.LevelFilter < TraceLevel.Verbose ? (TraceJsonReader) null : new TraceJsonReader(reader);
      new JsonSerializerInternalReader(this).Populate((JsonReader) traceJsonReader ?? reader, target);
      if (traceJsonReader != null)
        this.TraceWriter.Trace_(TraceLevel.Verbose, traceJsonReader.GetDeserializedJsonMessage(), (Exception) null);
      this.ResetReader(reader, previousCulture, previousDateTimeZoneHandling, previousDateParseHandling, previousFloatParseHandling, previousMaxDepth, previousDateFormatString);
    }

    public object Deserialize(JsonReader reader)
    {
      return this.Deserialize(reader, (Type) null);
    }

    public object Deserialize(TextReader reader, Type objectType)
    {
      return this.Deserialize((JsonReader) new JsonTextReader(reader), objectType);
    }

    public T Deserialize<T>(JsonReader reader)
    {
      return (T) this.Deserialize(reader, typeof (T));
    }

    public object Deserialize(JsonReader reader, Type objectType)
    {
      return this.DeserializeInternal(reader, objectType);
    }

    internal virtual object DeserializeInternal(JsonReader reader, Type objectType)
    {
      ValidationUtils.ArgumentNotNull((object) reader, nameof (reader));
      CultureInfo previousCulture;
      DateTimeZoneHandling? previousDateTimeZoneHandling;
      DateParseHandling? previousDateParseHandling;
      FloatParseHandling? previousFloatParseHandling;
      int? previousMaxDepth;
      string previousDateFormatString;
      this.SetupReader(reader, out previousCulture, out previousDateTimeZoneHandling, out previousDateParseHandling, out previousFloatParseHandling, out previousMaxDepth, out previousDateFormatString);
      TraceJsonReader traceJsonReader = this.TraceWriter == null || this.TraceWriter.LevelFilter < TraceLevel.Verbose ? (TraceJsonReader) null : new TraceJsonReader(reader);
      object obj = new JsonSerializerInternalReader(this).Deserialize((JsonReader) traceJsonReader ?? reader, objectType, this.CheckAdditionalContent);
      if (traceJsonReader != null)
        this.TraceWriter.Trace_(TraceLevel.Verbose, traceJsonReader.GetDeserializedJsonMessage(), (Exception) null);
      this.ResetReader(reader, previousCulture, previousDateTimeZoneHandling, previousDateParseHandling, previousFloatParseHandling, previousMaxDepth, previousDateFormatString);
      return obj;
    }

    private void SetupReader(
      JsonReader reader,
      out CultureInfo previousCulture,
      out DateTimeZoneHandling? previousDateTimeZoneHandling,
      out DateParseHandling? previousDateParseHandling,
      out FloatParseHandling? previousFloatParseHandling,
      out int? previousMaxDepth,
      out string previousDateFormatString)
    {
      if (this._culture != null && !this._culture.Equals((object) reader.Culture))
      {
        previousCulture = reader.Culture;
        reader.Culture = this._culture;
      }
      else
        previousCulture = (CultureInfo) null;
      if (this._dateTimeZoneHandling.HasValue)
      {
        int timeZoneHandling1 = (int) reader.DateTimeZoneHandling;
        DateTimeZoneHandling? timeZoneHandling2 = this._dateTimeZoneHandling;
        int valueOrDefault = (int) timeZoneHandling2.GetValueOrDefault();
        if ((timeZoneHandling1 == valueOrDefault ? (!timeZoneHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          previousDateTimeZoneHandling = new DateTimeZoneHandling?(reader.DateTimeZoneHandling);
          reader.DateTimeZoneHandling = this._dateTimeZoneHandling.GetValueOrDefault();
          goto label_7;
        }
      }
      previousDateTimeZoneHandling = new DateTimeZoneHandling?();
label_7:
      if (this._dateParseHandling.HasValue)
      {
        int dateParseHandling1 = (int) reader.DateParseHandling;
        DateParseHandling? dateParseHandling2 = this._dateParseHandling;
        int valueOrDefault = (int) dateParseHandling2.GetValueOrDefault();
        if ((dateParseHandling1 == valueOrDefault ? (!dateParseHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          previousDateParseHandling = new DateParseHandling?(reader.DateParseHandling);
          reader.DateParseHandling = this._dateParseHandling.GetValueOrDefault();
          goto label_11;
        }
      }
      previousDateParseHandling = new DateParseHandling?();
label_11:
      if (this._floatParseHandling.HasValue)
      {
        int floatParseHandling1 = (int) reader.FloatParseHandling;
        FloatParseHandling? floatParseHandling2 = this._floatParseHandling;
        int valueOrDefault = (int) floatParseHandling2.GetValueOrDefault();
        if ((floatParseHandling1 == valueOrDefault ? (!floatParseHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          previousFloatParseHandling = new FloatParseHandling?(reader.FloatParseHandling);
          reader.FloatParseHandling = this._floatParseHandling.GetValueOrDefault();
          goto label_15;
        }
      }
      previousFloatParseHandling = new FloatParseHandling?();
label_15:
      if (this._maxDepthSet)
      {
        int? maxDepth1 = reader.MaxDepth;
        int? maxDepth2 = this._maxDepth;
        if ((maxDepth1.GetValueOrDefault() == maxDepth2.GetValueOrDefault() ? (maxDepth1.HasValue != maxDepth2.HasValue ? 1 : 0) : 1) != 0)
        {
          previousMaxDepth = reader.MaxDepth;
          reader.MaxDepth = this._maxDepth;
          goto label_19;
        }
      }
      previousMaxDepth = new int?();
label_19:
      if (this._dateFormatStringSet && reader.DateFormatString != this._dateFormatString)
      {
        previousDateFormatString = reader.DateFormatString;
        reader.DateFormatString = this._dateFormatString;
      }
      else
        previousDateFormatString = (string) null;
      JsonTextReader jsonTextReader = reader as JsonTextReader;
      if (jsonTextReader == null)
        return;
      DefaultContractResolver contractResolver = this._contractResolver as DefaultContractResolver;
      if (contractResolver == null)
        return;
      jsonTextReader.NameTable = contractResolver.GetNameTable();
    }

    private void ResetReader(
      JsonReader reader,
      CultureInfo previousCulture,
      DateTimeZoneHandling? previousDateTimeZoneHandling,
      DateParseHandling? previousDateParseHandling,
      FloatParseHandling? previousFloatParseHandling,
      int? previousMaxDepth,
      string previousDateFormatString)
    {
      if (previousCulture != null)
        reader.Culture = previousCulture;
      if (previousDateTimeZoneHandling.HasValue)
        reader.DateTimeZoneHandling = previousDateTimeZoneHandling.GetValueOrDefault();
      if (previousDateParseHandling.HasValue)
        reader.DateParseHandling = previousDateParseHandling.GetValueOrDefault();
      if (previousFloatParseHandling.HasValue)
        reader.FloatParseHandling = previousFloatParseHandling.GetValueOrDefault();
      if (this._maxDepthSet)
        reader.MaxDepth = previousMaxDepth;
      if (this._dateFormatStringSet)
        reader.DateFormatString = previousDateFormatString;
      JsonTextReader jsonTextReader = reader as JsonTextReader;
      if (jsonTextReader == null)
        return;
      jsonTextReader.NameTable = (PropertyNameTable) null;
    }

    public void Serialize(TextWriter textWriter, object value)
    {
      this.Serialize((JsonWriter) new JsonTextWriter(textWriter), value);
    }

    public void Serialize(JsonWriter jsonWriter, object value, Type objectType)
    {
      this.SerializeInternal(jsonWriter, value, objectType);
    }

    public void Serialize(TextWriter textWriter, object value, Type objectType)
    {
      this.Serialize((JsonWriter) new JsonTextWriter(textWriter), value, objectType);
    }

    public void Serialize(JsonWriter jsonWriter, object value)
    {
      this.SerializeInternal(jsonWriter, value, (Type) null);
    }

    internal virtual void SerializeInternal(JsonWriter jsonWriter, object value, Type objectType)
    {
      ValidationUtils.ArgumentNotNull((object) jsonWriter, nameof (jsonWriter));
      Formatting? nullable1 = new Formatting?();
      if (this._formatting.HasValue)
      {
        int formatting1 = (int) jsonWriter.Formatting;
        Formatting? formatting2 = this._formatting;
        int valueOrDefault = (int) formatting2.GetValueOrDefault();
        if ((formatting1 == valueOrDefault ? (!formatting2.HasValue ? 1 : 0) : 1) != 0)
        {
          nullable1 = new Formatting?(jsonWriter.Formatting);
          jsonWriter.Formatting = this._formatting.GetValueOrDefault();
        }
      }
      DateFormatHandling? nullable2 = new DateFormatHandling?();
      if (this._dateFormatHandling.HasValue)
      {
        int dateFormatHandling1 = (int) jsonWriter.DateFormatHandling;
        DateFormatHandling? dateFormatHandling2 = this._dateFormatHandling;
        int valueOrDefault = (int) dateFormatHandling2.GetValueOrDefault();
        if ((dateFormatHandling1 == valueOrDefault ? (!dateFormatHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          nullable2 = new DateFormatHandling?(jsonWriter.DateFormatHandling);
          jsonWriter.DateFormatHandling = this._dateFormatHandling.GetValueOrDefault();
        }
      }
      DateTimeZoneHandling? nullable3 = new DateTimeZoneHandling?();
      if (this._dateTimeZoneHandling.HasValue)
      {
        int timeZoneHandling1 = (int) jsonWriter.DateTimeZoneHandling;
        DateTimeZoneHandling? timeZoneHandling2 = this._dateTimeZoneHandling;
        int valueOrDefault = (int) timeZoneHandling2.GetValueOrDefault();
        if ((timeZoneHandling1 == valueOrDefault ? (!timeZoneHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          nullable3 = new DateTimeZoneHandling?(jsonWriter.DateTimeZoneHandling);
          jsonWriter.DateTimeZoneHandling = this._dateTimeZoneHandling.GetValueOrDefault();
        }
      }
      FloatFormatHandling? nullable4 = new FloatFormatHandling?();
      if (this._floatFormatHandling.HasValue)
      {
        int floatFormatHandling1 = (int) jsonWriter.FloatFormatHandling;
        FloatFormatHandling? floatFormatHandling2 = this._floatFormatHandling;
        int valueOrDefault = (int) floatFormatHandling2.GetValueOrDefault();
        if ((floatFormatHandling1 == valueOrDefault ? (!floatFormatHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          nullable4 = new FloatFormatHandling?(jsonWriter.FloatFormatHandling);
          jsonWriter.FloatFormatHandling = this._floatFormatHandling.GetValueOrDefault();
        }
      }
      StringEscapeHandling? nullable5 = new StringEscapeHandling?();
      if (this._stringEscapeHandling.HasValue)
      {
        int stringEscapeHandling1 = (int) jsonWriter.StringEscapeHandling;
        StringEscapeHandling? stringEscapeHandling2 = this._stringEscapeHandling;
        int valueOrDefault = (int) stringEscapeHandling2.GetValueOrDefault();
        if ((stringEscapeHandling1 == valueOrDefault ? (!stringEscapeHandling2.HasValue ? 1 : 0) : 1) != 0)
        {
          nullable5 = new StringEscapeHandling?(jsonWriter.StringEscapeHandling);
          jsonWriter.StringEscapeHandling = this._stringEscapeHandling.GetValueOrDefault();
        }
      }
      CultureInfo cultureInfo = (CultureInfo) null;
      if (this._culture != null && !this._culture.Equals((object) jsonWriter.Culture))
      {
        cultureInfo = jsonWriter.Culture;
        jsonWriter.Culture = this._culture;
      }
      string str = (string) null;
      if (this._dateFormatStringSet && jsonWriter.DateFormatString != this._dateFormatString)
      {
        str = jsonWriter.DateFormatString;
        jsonWriter.DateFormatString = this._dateFormatString;
      }
      TraceJsonWriter traceJsonWriter = this.TraceWriter == null || this.TraceWriter.LevelFilter < TraceLevel.Verbose ? (TraceJsonWriter) null : new TraceJsonWriter(jsonWriter);
      new JsonSerializerInternalWriter(this).Serialize((JsonWriter) traceJsonWriter ?? jsonWriter, value, objectType);
      if (traceJsonWriter != null)
        this.TraceWriter.Trace_(TraceLevel.Verbose, traceJsonWriter.GetSerializedJsonMessage(), (Exception) null);
      if (nullable1.HasValue)
        jsonWriter.Formatting = nullable1.GetValueOrDefault();
      if (nullable2.HasValue)
        jsonWriter.DateFormatHandling = nullable2.GetValueOrDefault();
      if (nullable3.HasValue)
        jsonWriter.DateTimeZoneHandling = nullable3.GetValueOrDefault();
      if (nullable4.HasValue)
        jsonWriter.FloatFormatHandling = nullable4.GetValueOrDefault();
      if (nullable5.HasValue)
        jsonWriter.StringEscapeHandling = nullable5.GetValueOrDefault();
      if (this._dateFormatStringSet)
        jsonWriter.DateFormatString = str;
      if (cultureInfo == null)
        return;
      jsonWriter.Culture = cultureInfo;
    }

    internal IReferenceResolver GetReferenceResolver()
    {
      if (this._referenceResolver == null)
        this._referenceResolver = (IReferenceResolver) new DefaultReferenceResolver();
      return this._referenceResolver;
    }

    internal JsonConverter GetMatchingConverter(Type type)
    {
      return JsonSerializer.GetMatchingConverter((IList<JsonConverter>) this._converters, type);
    }

    internal static JsonConverter GetMatchingConverter(
      IList<JsonConverter> converters,
      Type objectType)
    {
      if (converters != null)
      {
        for (int index = 0; index < converters.Count; ++index)
        {
          JsonConverter converter = converters[index];
          if (converter.CanConvert(objectType))
            return converter;
        }
      }
      return (JsonConverter) null;
    }

    internal void OnError(Newtonsoft.Json.Serialization.ErrorEventArgs e)
    {
      EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> error = this.Error;
      if (error == null)
        return;
      error((object) this, e);
    }
  }
}
