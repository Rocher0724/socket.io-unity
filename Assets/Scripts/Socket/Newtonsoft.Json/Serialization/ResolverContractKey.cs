using System;

namespace Socket.Newtonsoft.Json.Serialization {
  internal struct ResolverContractKey : IEquatable<ResolverContractKey> {
    private readonly Type _resolverType;
    private readonly Type _contractType;

    public ResolverContractKey(Type resolverType, Type contractType) {
      this._resolverType = resolverType;
      this._contractType = contractType;
    }

    public override int GetHashCode() {
      return this._resolverType.GetHashCode() ^ this._contractType.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (!(obj is ResolverContractKey))
        return false;
      return this.Equals((ResolverContractKey) obj);
    }

    public bool Equals(ResolverContractKey other) {
      if (this._resolverType == other._resolverType)
        return this._contractType == other._contractType;
      return false;
    }
  }
}