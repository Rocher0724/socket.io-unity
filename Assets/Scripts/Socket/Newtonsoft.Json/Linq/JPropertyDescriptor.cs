using System;
using System.ComponentModel;

namespace Socket.Newtonsoft.Json.Linq {
  public class JPropertyDescriptor : PropertyDescriptor {
    public JPropertyDescriptor(string name)
      : base(name, (Attribute[]) null) {
    }

    private static JObject CastInstance(object instance) {
      return (JObject) instance;
    }

    public override bool CanResetValue(object component) {
      return false;
    }

    public override object GetValue(object component) {
      return (object) JPropertyDescriptor.CastInstance(component)[this.Name];
    }

    public override void ResetValue(object component) {
    }

    public override void SetValue(object component, object value) {
      JToken jtoken = value as JToken ?? (JToken) new JValue(value);
      JPropertyDescriptor.CastInstance(component)[this.Name] = jtoken;
    }

    public override bool ShouldSerializeValue(object component) {
      return false;
    }

    public override Type ComponentType {
      get { return typeof(JObject); }
    }

    public override bool IsReadOnly {
      get { return false; }
    }

    public override Type PropertyType {
      get { return typeof(object); }
    }

    protected override int NameHashCode {
      get { return base.NameHashCode; }
    }
  }
}