using System;

namespace Socket.Newtonsoft.Json {
  public enum ConstructorHandling
  {
    Default,
    AllowNonPublicDefaultConstructor,
  }
  public enum DateFormatHandling
  {
    IsoDateFormat,
    MicrosoftDateFormat,
  }
  public enum DateParseHandling
  {
    None,
    DateTime,
  }
  public enum DateTimeZoneHandling
  {
    Local,
    Utc,
    Unspecified,
    RoundtripKind,
  }
  [Flags]
  public enum DefaultValueHandling
  {
    Include = 0,
    Ignore = 1,
    Populate = 2,
    IgnoreAndPopulate = Populate | Ignore, // 0x00000003
  }
  public enum FloatFormatHandling
  {
    String,
    Symbol,
    DefaultValue,
  }
  public enum FloatParseHandling
  {
    Double,
    Decimal,
  }

  public enum Formatting {
    None,
    Indented,
  }
  public interface IArrayPool<T>
  {
    T[] Rent(int minimumLength);

    void Return(T[] array);
  }
  
  internal enum JsonContainerType
  {
    None,
    Object,
    Array,
    Constructor,
  }
  
  public enum JsonToken
  {
    None,
    StartObject,
    StartArray,
    StartConstructor,
    PropertyName,
    Comment,
    Raw,
    Integer,
    Float,
    String,
    Boolean,
    Null,
    Undefined,
    EndObject,
    EndArray,
    EndConstructor,
    Date,
    Bytes,
  }

  public enum MemberSerialization
  {
    OptOut,
    OptIn,
    Fields,
  }

  public enum MetadataPropertyHandling
  {
    Default,
    ReadAhead,
    Ignore,
  }
  
  public enum MissingMemberHandling
  {
    Ignore,
    Error,
  }
  
  public enum NullValueHandling
  {
    Include,
    Ignore,
  }
  
  public enum ObjectCreationHandling
  {
    Auto,
    Reuse,
    Replace,
  }
  
  [Flags]
  public enum PreserveReferencesHandling
  {
    None = 0,
    Objects = 1,
    Arrays = 2,
    All = Arrays | Objects, // 0x00000003
  }
  
  internal enum ReadType
  {
    Read,
    ReadAsInt32,
    ReadAsBytes,
    ReadAsString,
    ReadAsDecimal,
    ReadAsDateTime,
    ReadAsDouble,
    ReadAsBoolean,
  }
  
  public enum ReferenceLoopHandling
  {
    Error,
    Ignore,
    Serialize,
  }
    
  public enum Required
  {
    Default,
    AllowNull,
    Always,
    DisallowNull,
  }
  public enum StringEscapeHandling
  {
    Default,
    EscapeNonAscii,
    EscapeHtml,
  }
  public enum TypeNameAssemblyFormatHandling
  {
    Simple,
    Full,
  }
  [Flags]
  public enum TypeNameHandling
  {
    None = 0,
    Objects = 1,
    Arrays = 2,
    All = Arrays | Objects, // 0x00000003
    Auto = 4,
  }
  public enum WriteState
  {
    Error,
    Closed,
    Object,
    Array,
    Constructor,
    Property,
    Start,
  }

}