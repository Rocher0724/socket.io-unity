using System;
using System.Globalization;
using System.Reflection;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class ReflectionValueProvider : IValueProvider {
    private readonly MemberInfo _memberInfo;

    public ReflectionValueProvider(MemberInfo memberInfo) {
      ValidationUtils.ArgumentNotNull((object) memberInfo, nameof(memberInfo));
      this._memberInfo = memberInfo;
    }

    public void SetValue(object target, object value) {
      try {
        ReflectionUtils.SetMemberValue(this._memberInfo, target, value);
      } catch (Exception ex) {
        throw new JsonSerializationException(
          "Error setting value to '{0}' on '{1}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) this._memberInfo.Name, (object) target.GetType()), ex);
      }
    }

    public object GetValue(object target) {
      try {
        return ReflectionUtils.GetMemberValue(this._memberInfo, target);
      } catch (Exception ex) {
        throw new JsonSerializationException(
          "Error getting value from '{0}' on '{1}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) this._memberInfo.Name, (object) target.GetType()), ex);
      }
    }
  }
}