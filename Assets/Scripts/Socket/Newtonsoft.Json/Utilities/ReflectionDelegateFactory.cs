using System;
using System.Globalization;
using System.Reflection;
using Socket.Newtonsoft.Json.Serialization;

namespace Socket.Newtonsoft.Json.Utilities {
  internal abstract class ReflectionDelegateFactory {
    public Func<T, object> CreateGet<T>(MemberInfo memberInfo) {
      PropertyInfo propertyInfo = memberInfo as PropertyInfo;
      if (propertyInfo != null)
        return this.CreateGet<T>(propertyInfo);
      FieldInfo fieldInfo = memberInfo as FieldInfo;
      if (fieldInfo != null)
        return this.CreateGet<T>(fieldInfo);
      throw new Exception("Could not create getter for {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
        (object) memberInfo));
    }

    public Action<T, object> CreateSet<T>(MemberInfo memberInfo) {
      PropertyInfo propertyInfo = memberInfo as PropertyInfo;
      if (propertyInfo != null)
        return this.CreateSet<T>(propertyInfo);
      FieldInfo fieldInfo = memberInfo as FieldInfo;
      if (fieldInfo != null)
        return this.CreateSet<T>(fieldInfo);
      throw new Exception("Could not create setter for {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
        (object) memberInfo));
    }

    public abstract MethodCall<T, object> CreateMethodCall<T>(MethodBase method);

    public abstract ObjectConstructor<object> CreateParameterizedConstructor(
      MethodBase method);

    public abstract Func<T> CreateDefaultConstructor<T>(Type type);

    public abstract Func<T, object> CreateGet<T>(PropertyInfo propertyInfo);

    public abstract Func<T, object> CreateGet<T>(FieldInfo fieldInfo);

    public abstract Action<T, object> CreateSet<T>(
      FieldInfo fieldInfo);

    public abstract Action<T, object> CreateSet<T>(
      PropertyInfo propertyInfo);
  }
}