using System;

namespace Socket.Newtonsoft.Json.Utilities {
  internal struct TypeNameKey : IEquatable<TypeNameKey> {
    internal readonly string AssemblyName;
    internal readonly string TypeName;

    public TypeNameKey(string assemblyName, string typeName) {
      this.AssemblyName = assemblyName;
      this.TypeName = typeName;
    }

    public override int GetHashCode() {
      string assemblyName = this.AssemblyName;
      int num1 = assemblyName != null ? assemblyName.GetHashCode() : 0;
      string typeName = this.TypeName;
      int num2 = typeName != null ? typeName.GetHashCode() : 0;
      return num1 ^ num2;
    }

    public override bool Equals(object obj) {
      if (!(obj is TypeNameKey))
        return false;
      return this.Equals((TypeNameKey) obj);
    }

    public bool Equals(TypeNameKey other) {
      if (this.AssemblyName == other.AssemblyName)
        return this.TypeName == other.TypeName;
      return false;
    }
  }
}