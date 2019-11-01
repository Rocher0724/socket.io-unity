using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class JsonPrimitiveContract : JsonContract {
    private static readonly Dictionary<Type, ReadType> ReadTypeMap = new Dictionary<Type, ReadType>() {
      [typeof(byte[])] = ReadType.ReadAsBytes,
      [typeof(byte)] = ReadType.ReadAsInt32,
      [typeof(short)] = ReadType.ReadAsInt32,
      [typeof(int)] = ReadType.ReadAsInt32,
      [typeof(Decimal)] = ReadType.ReadAsDecimal,
      [typeof(bool)] = ReadType.ReadAsBoolean,
      [typeof(string)] = ReadType.ReadAsString,
      [typeof(DateTime)] = ReadType.ReadAsDateTime,
      [typeof(float)] = ReadType.ReadAsDouble,
      [typeof(double)] = ReadType.ReadAsDouble
    };

    internal PrimitiveTypeCode TypeCode { get; set; }

    public JsonPrimitiveContract(Type underlyingType)
      : base(underlyingType) {
      this.ContractType = JsonContractType.Primitive;
      this.TypeCode = ConvertUtils.GetTypeCode(underlyingType);
      this.IsReadOnlyOrFixedSize = true;
      ReadType readType;
      if (!JsonPrimitiveContract.ReadTypeMap.TryGetValue(this.NonNullableUnderlyingType, out readType))
        return;
      this.InternalReadType = readType;
    }
  }
}