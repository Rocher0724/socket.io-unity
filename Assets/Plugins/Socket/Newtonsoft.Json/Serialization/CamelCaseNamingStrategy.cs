using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class CamelCaseNamingStrategy : NamingStrategy {
    public CamelCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames) {
      this.ProcessDictionaryKeys = processDictionaryKeys;
      this.OverrideSpecifiedNames = overrideSpecifiedNames;
    }

    public CamelCaseNamingStrategy(
      bool processDictionaryKeys,
      bool overrideSpecifiedNames,
      bool processExtensionDataNames)
      : this(processDictionaryKeys, overrideSpecifiedNames) {
      this.ProcessExtensionDataNames = processExtensionDataNames;
    }

    public CamelCaseNamingStrategy() {
    }

    protected override string ResolvePropertyName(string name) {
      return StringUtils.ToCamelCase(name);
    }
  }
}