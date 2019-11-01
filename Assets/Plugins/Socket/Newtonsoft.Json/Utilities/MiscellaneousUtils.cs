using System;
using System.Globalization;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class MiscellaneousUtils {
    public static bool ValueEquals(object objA, object objB) {
      if (objA == objB)
        return true;
      if (objA == null || objB == null)
        return false;
      if (objA.GetType() == objB.GetType())
        return objA.Equals(objB);
      if (ConvertUtils.IsInteger(objA) && ConvertUtils.IsInteger(objB))
        return Convert.ToDecimal(objA, (IFormatProvider) CultureInfo.CurrentCulture)
          .Equals(Convert.ToDecimal(objB, (IFormatProvider) CultureInfo.CurrentCulture));
      if ((objA is double || objA is float || objA is Decimal) && (objB is double || objB is float || objB is Decimal))
        return MathUtils.ApproxEquals(Convert.ToDouble(objA, (IFormatProvider) CultureInfo.CurrentCulture),
          Convert.ToDouble(objB, (IFormatProvider) CultureInfo.CurrentCulture));
      return false;
    }

    public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(
      string paramName,
      object actualValue,
      string message) {
      string message1 = message + Environment.NewLine +
                        "Actual value was {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, actualValue);
      return new ArgumentOutOfRangeException(paramName, message1);
    }

    public static string ToString(object value) {
      if (value == null)
        return "{null}";
      if (!(value is string))
        return value.ToString();
      return "\"" + value.ToString() + "\"";
    }

    public static int ByteArrayCompare(byte[] a1, byte[] a2) {
      int num1 = a1.Length.CompareTo(a2.Length);
      if (num1 != 0)
        return num1;
      for (int index = 0; index < a1.Length; ++index) {
        int num2 = a1[index].CompareTo(a2[index]);
        if (num2 != 0)
          return num2;
      }

      return 0;
    }

    public static string GetPrefix(string qualifiedName) {
      string prefix;
      string localName;
      MiscellaneousUtils.GetQualifiedNameParts(qualifiedName, out prefix, out localName);
      return prefix;
    }

    public static string GetLocalName(string qualifiedName) {
      string prefix;
      string localName;
      MiscellaneousUtils.GetQualifiedNameParts(qualifiedName, out prefix, out localName);
      return localName;
    }

    public static void GetQualifiedNameParts(
      string qualifiedName,
      out string prefix,
      out string localName) {
      int length = qualifiedName.IndexOf(':');
      switch (length) {
        case -1:
        case 0:
          prefix = (string) null;
          localName = qualifiedName;
          break;
        default:
          if (qualifiedName.Length - 1 != length) {
            prefix = qualifiedName.Substring(0, length);
            localName = qualifiedName.Substring(length + 1);
            break;
          }

          goto case -1;
      }
    }

    internal static string FormatValueForPrint(object value) {
      if (value == null)
        return "{null}";
      if (value is string)
        return "\"" + value + "\"";
      return value.ToString();
    }
  }
}