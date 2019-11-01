using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Socket.Newtonsoft.Json.Serialization;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class ConvertUtils {
    private static readonly Dictionary<Type, PrimitiveTypeCode> TypeCodeMap = new Dictionary<Type, PrimitiveTypeCode>() {
      {
        typeof(char),
        PrimitiveTypeCode.Char
      }, {
        typeof(char?),
        PrimitiveTypeCode.CharNullable
      }, {
        typeof(bool),
        PrimitiveTypeCode.Boolean
      }, {
        typeof(bool?),
        PrimitiveTypeCode.BooleanNullable
      }, {
        typeof(sbyte),
        PrimitiveTypeCode.SByte
      }, {
        typeof(sbyte?),
        PrimitiveTypeCode.SByteNullable
      }, {
        typeof(short),
        PrimitiveTypeCode.Int16
      }, {
        typeof(short?),
        PrimitiveTypeCode.Int16Nullable
      }, {
        typeof(ushort),
        PrimitiveTypeCode.UInt16
      }, {
        typeof(ushort?),
        PrimitiveTypeCode.UInt16Nullable
      }, {
        typeof(int),
        PrimitiveTypeCode.Int32
      }, {
        typeof(int?),
        PrimitiveTypeCode.Int32Nullable
      }, {
        typeof(byte),
        PrimitiveTypeCode.Byte
      }, {
        typeof(byte?),
        PrimitiveTypeCode.ByteNullable
      }, {
        typeof(uint),
        PrimitiveTypeCode.UInt32
      }, {
        typeof(uint?),
        PrimitiveTypeCode.UInt32Nullable
      }, {
        typeof(long),
        PrimitiveTypeCode.Int64
      }, {
        typeof(long?),
        PrimitiveTypeCode.Int64Nullable
      }, {
        typeof(ulong),
        PrimitiveTypeCode.UInt64
      }, {
        typeof(ulong?),
        PrimitiveTypeCode.UInt64Nullable
      }, {
        typeof(float),
        PrimitiveTypeCode.Single
      }, {
        typeof(float?),
        PrimitiveTypeCode.SingleNullable
      }, {
        typeof(double),
        PrimitiveTypeCode.Double
      }, {
        typeof(double?),
        PrimitiveTypeCode.DoubleNullable
      }, {
        typeof(DateTime),
        PrimitiveTypeCode.DateTime
      }, {
        typeof(DateTime?),
        PrimitiveTypeCode.DateTimeNullable
      }, {
        typeof(Decimal),
        PrimitiveTypeCode.Decimal
      }, {
        typeof(Decimal?),
        PrimitiveTypeCode.DecimalNullable
      }, {
        typeof(Guid),
        PrimitiveTypeCode.Guid
      }, {
        typeof(Guid?),
        PrimitiveTypeCode.GuidNullable
      }, {
        typeof(TimeSpan),
        PrimitiveTypeCode.TimeSpan
      }, {
        typeof(TimeSpan?),
        PrimitiveTypeCode.TimeSpanNullable
      }, {
        typeof(Uri),
        PrimitiveTypeCode.Uri
      }, {
        typeof(string),
        PrimitiveTypeCode.String
      }, {
        typeof(byte[]),
        PrimitiveTypeCode.Bytes
      }, {
        typeof(DBNull),
        PrimitiveTypeCode.DBNull
      }
    };

    private static readonly TypeInformation[] PrimitiveTypeCodes = new TypeInformation[19] {
      new TypeInformation() {
        Type = typeof(object),
        TypeCode = PrimitiveTypeCode.Empty
      },
      new TypeInformation() {
        Type = typeof(object),
        TypeCode = PrimitiveTypeCode.Object
      },
      new TypeInformation() {
        Type = typeof(object),
        TypeCode = PrimitiveTypeCode.DBNull
      },
      new TypeInformation() {
        Type = typeof(bool),
        TypeCode = PrimitiveTypeCode.Boolean
      },
      new TypeInformation() {
        Type = typeof(char),
        TypeCode = PrimitiveTypeCode.Char
      },
      new TypeInformation() {
        Type = typeof(sbyte),
        TypeCode = PrimitiveTypeCode.SByte
      },
      new TypeInformation() {
        Type = typeof(byte),
        TypeCode = PrimitiveTypeCode.Byte
      },
      new TypeInformation() {
        Type = typeof(short),
        TypeCode = PrimitiveTypeCode.Int16
      },
      new TypeInformation() {
        Type = typeof(ushort),
        TypeCode = PrimitiveTypeCode.UInt16
      },
      new TypeInformation() {
        Type = typeof(int),
        TypeCode = PrimitiveTypeCode.Int32
      },
      new TypeInformation() {
        Type = typeof(uint),
        TypeCode = PrimitiveTypeCode.UInt32
      },
      new TypeInformation() {
        Type = typeof(long),
        TypeCode = PrimitiveTypeCode.Int64
      },
      new TypeInformation() {
        Type = typeof(ulong),
        TypeCode = PrimitiveTypeCode.UInt64
      },
      new TypeInformation() {
        Type = typeof(float),
        TypeCode = PrimitiveTypeCode.Single
      },
      new TypeInformation() {
        Type = typeof(double),
        TypeCode = PrimitiveTypeCode.Double
      },
      new TypeInformation() {
        Type = typeof(Decimal),
        TypeCode = PrimitiveTypeCode.Decimal
      },
      new TypeInformation() {
        Type = typeof(DateTime),
        TypeCode = PrimitiveTypeCode.DateTime
      },
      new TypeInformation() {
        Type = typeof(object),
        TypeCode = PrimitiveTypeCode.Empty
      },
      new TypeInformation() {
        Type = typeof(string),
        TypeCode = PrimitiveTypeCode.String
      }
    };

    private static readonly ThreadSafeStore<ConvertUtils.TypeConvertKey, Func<object, object>> CastConverters =
      new ThreadSafeStore<ConvertUtils.TypeConvertKey, Func<object, object>>(
        new Func<ConvertUtils.TypeConvertKey, Func<object, object>>(ConvertUtils.CreateCastConverter));

    private static Decimal[] _decimalFactors;

    public static PrimitiveTypeCode GetTypeCode(Type t) {
      bool isEnum;
      return ConvertUtils.GetTypeCode(t, out isEnum);
    }

    public static PrimitiveTypeCode GetTypeCode(Type t, out bool isEnum) {
      PrimitiveTypeCode primitiveTypeCode;
      if (ConvertUtils.TypeCodeMap.TryGetValue(t, out primitiveTypeCode)) {
        isEnum = false;
        return primitiveTypeCode;
      }

      if (t.IsEnum()) {
        isEnum = true;
        return ConvertUtils.GetTypeCode(Enum.GetUnderlyingType(t));
      }

      if (ReflectionUtils.IsNullableType(t)) {
        Type underlyingType = Nullable.GetUnderlyingType(t);
        if (underlyingType.IsEnum()) {
          Type t1 = typeof(Nullable<>).MakeGenericType(Enum.GetUnderlyingType(underlyingType));
          isEnum = true;
          return ConvertUtils.GetTypeCode(t1);
        }
      }

      isEnum = false;
      return PrimitiveTypeCode.Object;
    }

    public static TypeInformation GetTypeInformation(IConvertible convertable) {
      return ConvertUtils.PrimitiveTypeCodes[(int) convertable.GetTypeCode()];
    }

    public static bool IsConvertible(Type t) {
      return typeof(IConvertible).IsAssignableFrom(t);
    }

    public static TimeSpan ParseTimeSpan(string input) {
      return TimeSpan.Parse(input);
    }

    private static Func<object, object> CreateCastConverter(ConvertUtils.TypeConvertKey t) {
      MethodInfo method = t.TargetType.GetMethod("op_Implicit", new Type[1] {
        t.InitialType
      });
      if (method == null)
        method = t.TargetType.GetMethod("op_Explicit", new Type[1] {
          t.InitialType
        });
      if (method == null)
        return (Func<object, object>) null;
      MethodCall<object, object> call =
        JsonTypeReflector.ReflectionDelegateFactory.CreateMethodCall<object>((MethodBase) method);
      return (Func<object, object>) (o => call((object) null, new object[1] {
        o
      }));
    }

    // 원래이름은 Convert 였다. 근데 클래스 Convert랑 겹쳐서 사용이 안돼서 바꿈 
    public static object Convert2(object initialValue, CultureInfo culture, Type targetType) {
      object obj;
      switch (ConvertUtils.TryConvertInternal(initialValue, culture, targetType, out obj)) {
        case ConvertUtils.ConvertResult.Success:
          return obj;
        case ConvertUtils.ConvertResult.CannotConvertNull:
          throw new Exception("Can not convert null {0} into non-nullable {1}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) initialValue.GetType(), (object) targetType));
        case ConvertUtils.ConvertResult.NotInstantiableType:
          throw new ArgumentException(
            "Target type {0} is not a value type or a non-abstract class.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) targetType), nameof(targetType));
        case ConvertUtils.ConvertResult.NoValidConversion:
          throw new InvalidOperationException("Can not convert from {0} to {1}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) initialValue.GetType(), (object) targetType));
        default:
          throw new InvalidOperationException("Unexpected conversion result.");
      }
    }

    private static bool TryConvert(
      object initialValue,
      CultureInfo culture,
      Type targetType,
      out object value) {
      try {
        if (ConvertUtils.TryConvertInternal(initialValue, culture, targetType, out value) ==
            ConvertUtils.ConvertResult.Success)
          return true;
        value = (object) null;
        return false;
      } catch {
        value = (object) null;
        return false;
      }
    }

    private static ConvertUtils.ConvertResult TryConvertInternal(
      object initialValue,
      CultureInfo culture,
      Type targetType,
      out object value) {
      if (initialValue == null)
        throw new ArgumentNullException(nameof(initialValue));
      if (ReflectionUtils.IsNullableType(targetType))
        targetType = Nullable.GetUnderlyingType(targetType);
      Type type = initialValue.GetType();
      if (targetType == type) {
        value = initialValue;
        return ConvertUtils.ConvertResult.Success;
      }

      if (ConvertUtils.IsConvertible(initialValue.GetType()) && ConvertUtils.IsConvertible(targetType)) {
        if (targetType.IsEnum()) {
          if (initialValue is string) {
            value = Enum.Parse(targetType, initialValue.ToString(), true);
            return ConvertUtils.ConvertResult.Success;
          }

          if (ConvertUtils.IsInteger(initialValue)) {
            value = Enum.ToObject(targetType, initialValue);
            return ConvertUtils.ConvertResult.Success;
          }
        }

        value = Convert.ChangeType(initialValue, targetType, (IFormatProvider) culture);
        return ConvertUtils.ConvertResult.Success;
      }

      byte[] b = initialValue as byte[];
      if (b != null && targetType == typeof(Guid)) {
        value = (object) new Guid(b);
        return ConvertUtils.ConvertResult.Success;
      }

      if (initialValue is Guid && targetType == typeof(byte[])) {
        value = (object) ((Guid) initialValue).ToByteArray();
        return ConvertUtils.ConvertResult.Success;
      }

      string str = initialValue as string;
      if (str != null) {
        if (targetType == typeof(Guid)) {
          value = (object) new Guid(str);
          return ConvertUtils.ConvertResult.Success;
        }

        if (targetType == typeof(Uri)) {
          value = (object) new Uri(str, UriKind.RelativeOrAbsolute);
          return ConvertUtils.ConvertResult.Success;
        }

        if (targetType == typeof(TimeSpan)) {
          value = (object) ConvertUtils.ParseTimeSpan(str);
          return ConvertUtils.ConvertResult.Success;
        }

        if (targetType == typeof(byte[])) {
          value = (object) Convert.FromBase64String(str);
          return ConvertUtils.ConvertResult.Success;
        }

        if (targetType == typeof(Version)) {
          Version result;
          if (ConvertUtils.VersionTryParse(str, out result)) {
            value = (object) result;
            return ConvertUtils.ConvertResult.Success;
          }

          value = (object) null;
          return ConvertUtils.ConvertResult.NoValidConversion;
        }

        if (typeof(Type).IsAssignableFrom(targetType)) {
          value = (object) Type.GetType(str, true);
          return ConvertUtils.ConvertResult.Success;
        }
      }

      TypeConverter converter1 = TypeDescriptor.GetConverter(type);
      if (converter1 != null && converter1.CanConvertTo(targetType)) {
        value = converter1.ConvertTo((ITypeDescriptorContext) null, culture, initialValue, targetType);
        return ConvertUtils.ConvertResult.Success;
      }

      TypeConverter converter2 = TypeDescriptor.GetConverter(targetType);
      if (converter2 != null && converter2.CanConvertFrom(type)) {
        value = converter2.ConvertFrom((ITypeDescriptorContext) null, culture, initialValue);
        return ConvertUtils.ConvertResult.Success;
      }

      if (initialValue == DBNull.Value) {
        if (ReflectionUtils.IsNullable(targetType)) {
          value = ConvertUtils.EnsureTypeAssignable((object) null, type, targetType);
          return ConvertUtils.ConvertResult.Success;
        }

        value = (object) null;
        return ConvertUtils.ConvertResult.CannotConvertNull;
      }

      INullable nullableValue = initialValue as INullable;
      if (nullableValue != null) {
        value = ConvertUtils.EnsureTypeAssignable(ConvertUtils.ToValue(nullableValue), type, targetType);
        return ConvertUtils.ConvertResult.Success;
      }

      if (targetType.IsInterface() || targetType.IsGenericTypeDefinition() || targetType.IsAbstract()) {
        value = (object) null;
        return ConvertUtils.ConvertResult.NotInstantiableType;
      }

      value = (object) null;
      return ConvertUtils.ConvertResult.NoValidConversion;
    }

    public static object ConvertOrCast(object initialValue, CultureInfo culture, Type targetType) {
      if (targetType == typeof(object))
        return initialValue;
      if (initialValue == null && ReflectionUtils.IsNullable(targetType))
        return (object) null;
      object obj;
      if (ConvertUtils.TryConvert(initialValue, culture, targetType, out obj))
        return obj;
      return ConvertUtils.EnsureTypeAssignable(initialValue, ReflectionUtils.GetObjectType(initialValue), targetType);
    }

    private static object EnsureTypeAssignable(object value, Type initialType, Type targetType) {
      Type type = value?.GetType();
      if (value != null) {
        if (targetType.IsAssignableFrom(type))
          return value;
        Func<object, object> func = ConvertUtils.CastConverters.Get(new ConvertUtils.TypeConvertKey(type, targetType));
        if (func != null)
          return func(value);
      } else if (ReflectionUtils.IsNullable(targetType))
        return (object) null;

      throw new ArgumentException("Could not cast or convert from {0} to {1}.".FormatWith(
        (IFormatProvider) CultureInfo.InvariantCulture, (object) (initialType?.ToString() ?? "{null}"),
        (object) targetType));
    }

    public static object ToValue(INullable nullableValue) {
      if (nullableValue == null)
        return (object) null;
      if (nullableValue is SqlInt32)
        return ConvertUtils.ToValue(nullableValue);
      if (nullableValue is SqlInt64)
        return ConvertUtils.ToValue(nullableValue);
      if (nullableValue is SqlBoolean)
        return ConvertUtils.ToValue(nullableValue);
      if (nullableValue is SqlString)
        return ConvertUtils.ToValue(nullableValue);
      if (nullableValue is SqlDateTime)
        return ConvertUtils.ToValue(nullableValue);
      throw new ArgumentException(
        "Unsupported INullable type: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) nullableValue.GetType()));
    }

    public static bool VersionTryParse(string input, out Version result) {
      try {
        result = new Version(input);
        return true;
      } catch {
        result = (Version) null;
        return false;
      }
    }

    public static bool IsInteger(object value) {
      switch (ConvertUtils.GetTypeCode(value.GetType())) {
        case PrimitiveTypeCode.SByte:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Byte:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.UInt64:
          return true;
        default:
          return false;
      }
    }

    public static ParseResult Int32TryParse(
      char[] chars,
      int start,
      int length,
      out int value) {
      value = 0;
      if (length == 0)
        return ParseResult.Invalid;
      bool flag = chars[start] == '-';
      if (flag) {
        if (length == 1)
          return ParseResult.Invalid;
        ++start;
        --length;
      }

      int num1 = start + length;
      if (length > 10 || length == 10 && (int) chars[start] - 48 > 2) {
        for (int index = start; index < num1; ++index) {
          switch (chars[index]) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
              continue;
            default:
              return ParseResult.Invalid;
          }
        }

        return ParseResult.Overflow;
      }

      for (int index1 = start; index1 < num1; ++index1) {
        int num2 = (int) chars[index1] - 48;
        switch (num2) {
          case 0:
          case 1:
          case 2:
          case 3:
          case 4:
          case 5:
          case 6:
          case 7:
          case 8:
          case 9:
            int num3 = 10 * value - num2;
            if (num3 > value) {
              for (int index2 = index1 + 1; index2 < num1; ++index2) {
                switch (chars[index2]) {
                  case '0':
                  case '1':
                  case '2':
                  case '3':
                  case '4':
                  case '5':
                  case '6':
                  case '7':
                  case '8':
                  case '9':
                    continue;
                  default:
                    return ParseResult.Invalid;
                }
              }

              return ParseResult.Overflow;
            }

            value = num3;
            continue;
          default:
            return ParseResult.Invalid;
        }
      }

      if (!flag) {
        if (value == int.MinValue)
          return ParseResult.Overflow;
        value = -value;
      }

      return ParseResult.Success;
    }

    public static ParseResult Int64TryParse(
      char[] chars,
      int start,
      int length,
      out long value) {
      value = 0L;
      if (length == 0)
        return ParseResult.Invalid;
      bool flag = chars[start] == '-';
      if (flag) {
        if (length == 1)
          return ParseResult.Invalid;
        ++start;
        --length;
      }

      int num1 = start + length;
      if (length > 19) {
        for (int index = start; index < num1; ++index) {
          switch (chars[index]) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
              continue;
            default:
              return ParseResult.Invalid;
          }
        }

        return ParseResult.Overflow;
      }

      for (int index1 = start; index1 < num1; ++index1) {
        int num2 = (int) chars[index1] - 48;
        switch (num2) {
          case 0:
          case 1:
          case 2:
          case 3:
          case 4:
          case 5:
          case 6:
          case 7:
          case 8:
          case 9:
            long num3 = 10L * value - (long) num2;
            if (num3 > value) {
              for (int index2 = index1 + 1; index2 < num1; ++index2) {
                switch (chars[index2]) {
                  case '0':
                  case '1':
                  case '2':
                  case '3':
                  case '4':
                  case '5':
                  case '6':
                  case '7':
                  case '8':
                  case '9':
                    continue;
                  default:
                    return ParseResult.Invalid;
                }
              }

              return ParseResult.Overflow;
            }

            value = num3;
            continue;
          default:
            return ParseResult.Invalid;
        }
      }

      if (!flag) {
        if (value == long.MinValue)
          return ParseResult.Overflow;
        value = -value;
      }

      return ParseResult.Success;
    }

    public static ParseResult DecimalTryParse(
      char[] chars,
      int start,
      int length,
      out Decimal value) {
      value = new Decimal();
      if (length == 0)
        return ParseResult.Invalid;
      bool flag1 = chars[start] == '-';
      if (flag1) {
        if (length == 1)
          return ParseResult.Invalid;
        ++start;
        --length;
      }

      int index = start;
      int num1 = start + length;
      int num2 = num1;
      int num3 = num1;
      int num4 = 0;
      ulong num5 = 0;
      ulong num6 = 0;
      int num7 = 0;
      int num8 = 0;
      bool? nullable1 = new bool?();
      bool? nullable2 = new bool?();
      for (; index < num1; ++index) {
        char ch1 = chars[index];
        if (ch1 != '.') {
          if (ch1 != 'E' && ch1 != 'e')
            goto label_29;
          label_12:
          if (index == start || index == num2)
            return ParseResult.Invalid;
          ++index;
          if (index == num1)
            return ParseResult.Invalid;
          if (num2 < num1)
            num3 = index - 1;
          char ch2 = chars[index];
          bool flag2 = false;
          switch (ch2) {
            case '+':
              ++index;
              break;
            case '-':
              flag2 = true;
              ++index;
              break;
          }

          for (; index < num1; ++index) {
            char ch3 = chars[index];
            if (ch3 < '0' || ch3 > '9')
              return ParseResult.Invalid;
            int num9 = 10 * num4 + ((int) ch3 - 48);
            if (num4 < num9)
              num4 = num9;
          }

          if (flag2) {
            num4 = -num4;
            continue;
          }

          continue;
          label_29:
          if (ch1 < '0' || ch1 > '9')
            return ParseResult.Invalid;
          if (index == start && ch1 == '0') {
            ++index;
            if (index != num1) {
              switch (chars[index]) {
                case '.':
                  goto label_9;
                case 'E':
                case 'e':
                  goto label_12;
                default:
                  return ParseResult.Invalid;
              }
            }
          }

          if (num7 < 29) {
            if (num7 == 28) {
              bool? nullable3 = nullable2;
              int num9;
              if (!nullable3.HasValue) {
                nullable2 = new bool?(num5 > 7922816251426433759UL || num5 == 7922816251426433759UL &&
                                      (num6 > 354395033UL || num6 == 354395033UL && ch1 > '5'));
                num9 = nullable2.GetValueOrDefault() ? 1 : 0;
              } else
                num9 = nullable3.GetValueOrDefault() ? 1 : 0;

              if (num9 != 0)
                goto label_45;
            }

            if (num7 < 19)
              num5 = num5 * 10UL + (ulong) ((int) ch1 - 48);
            else
              num6 = num6 * 10UL + (ulong) ((int) ch1 - 48);
            ++num7;
            continue;
          }

          label_45:
          if (!nullable1.HasValue)
            nullable1 = new bool?(ch1 >= '5');
          ++num8;
          continue;
        }

        label_9:
        if (index == start || index + 1 == num1 || num2 != num1)
          return ParseResult.Invalid;
        num2 = index + 1;
      }

      int num10 = num4 + num8 - (num3 - num2);
      value = num7 > 19 ? (Decimal) num5 * ConvertUtils.DecimalFactors[num7 - 20] + (Decimal) num6 : (Decimal) num5;
      if (num10 > 0) {
        int num9 = num7 + num10;
        if (num9 > 29)
          return ParseResult.Overflow;
        if (num9 == 29) {
          if (num10 > 1) {
            value *= ConvertUtils.DecimalFactors[num10 - 2];
            if (value > new Decimal(-1717986919, -1717986919, 429496729, false, (byte) 0))
              return ParseResult.Overflow;
          }

          value *= new Decimal(10);
        } else
          value *= ConvertUtils.DecimalFactors[num10 - 1];
      } else {
        bool? nullable3 = nullable1;
        bool flag2 = true;
        if ((nullable3.GetValueOrDefault() == flag2 ? (nullable3.HasValue ? 1 : 0) : 0) != 0 && num10 >= -28)
          ++value;
        if (num10 < 0) {
          if (num7 + num10 + 28 <= 0) {
            value = new Decimal();
            return ParseResult.Success;
          }

          if (num10 >= -28) {
            value /= ConvertUtils.DecimalFactors[-num10 - 1];
          } else {
            Decimal[] decimalFactors = ConvertUtils.DecimalFactors;
            value /= decimalFactors[27];
            value /= decimalFactors[-num10 - 29];
          }
        }
      }

      if (flag1)
        value = -value;
      return ParseResult.Success;
    }

    private static Decimal[] DecimalFactors {
      get {
        Decimal[] numArray = ConvertUtils._decimalFactors;
        if (numArray == null) {
          numArray = new Decimal[28];
          Decimal one = Decimal.One;
          for (int index = 0; index < numArray.Length; ++index)
            numArray[index] = (one *= new Decimal(10));
          ConvertUtils._decimalFactors = numArray;
        }

        return numArray;
      }
    }

    public static bool TryConvertGuid(string s, out Guid g) {
      if (s == null)
        throw new ArgumentNullException(nameof(s));
      if (new Regex("^[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}$").Match(s).Success) {
        g = new Guid(s);
        return true;
      }

      g = Guid.Empty;
      return false;
    }

    public static bool TryHexTextToInt(char[] text, int start, int end, out int value) {
      value = 0;
      for (int index = start; index < end; ++index) {
        char ch = text[index];
        int num;
        if (ch <= '9' && ch >= '0')
          num = (int) ch - 48;
        else if (ch <= 'F' && ch >= 'A')
          num = (int) ch - 55;
        else if (ch <= 'f' && ch >= 'a') {
          num = (int) ch - 87;
        } else {
          value = 0;
          return false;
        }

        value += num << (end - 1 - index) * 4;
      }

      return true;
    }

    internal struct TypeConvertKey : IEquatable<ConvertUtils.TypeConvertKey> {
      private readonly Type _initialType;
      private readonly Type _targetType;

      public Type InitialType {
        get { return this._initialType; }
      }

      public Type TargetType {
        get { return this._targetType; }
      }

      public TypeConvertKey(Type initialType, Type targetType) {
        this._initialType = initialType;
        this._targetType = targetType;
      }

      public override int GetHashCode() {
        return this._initialType.GetHashCode() ^ this._targetType.GetHashCode();
      }

      public override bool Equals(object obj) {
        if (!(obj is ConvertUtils.TypeConvertKey))
          return false;
        return this.Equals((ConvertUtils.TypeConvertKey) obj);
      }

      public bool Equals(ConvertUtils.TypeConvertKey other) {
        if (this._initialType == other._initialType)
          return this._targetType == other._targetType;
        return false;
      }
    }

    internal enum ConvertResult {
      Success,
      CannotConvertNull,
      NotInstantiableType,
      NoValidConversion,
    }
  }
}