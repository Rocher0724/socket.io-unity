using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class CamelCasePropertyNamesContractResolver : DefaultContractResolver {
    private static readonly object TypeContractCacheLock = new object();
    private static readonly PropertyNameTable NameTable = new PropertyNameTable();
    private static Dictionary<ResolverContractKey, JsonContract> _contractCache;

    public CamelCasePropertyNamesContractResolver() {
      CamelCaseNamingStrategy caseNamingStrategy = new CamelCaseNamingStrategy();
      caseNamingStrategy.ProcessDictionaryKeys = true;
      caseNamingStrategy.OverrideSpecifiedNames = true;
      this.NamingStrategy = (NamingStrategy) caseNamingStrategy;
    }

    public override JsonContract ResolveContract(Type type) {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      ResolverContractKey key = new ResolverContractKey(this.GetType(), type);
      Dictionary<ResolverContractKey, JsonContract> contractCache1 =
        CamelCasePropertyNamesContractResolver._contractCache;
      JsonContract contract;
      if (contractCache1 == null || !contractCache1.TryGetValue(key, out contract)) {
        contract = this.CreateContract(type);
        lock (CamelCasePropertyNamesContractResolver.TypeContractCacheLock) {
          Dictionary<ResolverContractKey, JsonContract> contractCache2 =
            CamelCasePropertyNamesContractResolver._contractCache;
          Dictionary<ResolverContractKey, JsonContract> dictionary = contractCache2 != null
            ? new Dictionary<ResolverContractKey, JsonContract>(
              (IDictionary<ResolverContractKey, JsonContract>) contractCache2)
            : new Dictionary<ResolverContractKey, JsonContract>();
          dictionary[key] = contract;
          CamelCasePropertyNamesContractResolver._contractCache = dictionary;
        }
      }

      return contract;
    }

    internal override PropertyNameTable GetNameTable() {
      return CamelCasePropertyNamesContractResolver.NameTable;
    }
  }
}