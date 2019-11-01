using System;

namespace Socket.Newtonsoft.Json.Utilities {
  internal class PropertyNameTable {
    private int _mask = 31;
    private static readonly int HashCodeRandomizer = Environment.TickCount;
    private int _count;
    private PropertyNameTable.Entry[] _entries;

    public PropertyNameTable() {
      this._entries = new PropertyNameTable.Entry[this._mask + 1];
    }

    public string Get(char[] key, int start, int length) {
      if (length == 0)
        return string.Empty;
      int num1 = length + PropertyNameTable.HashCodeRandomizer;
      int num2 = num1 + (num1 << 7 ^ (int) key[start]);
      int num3 = start + length;
      for (int index = start + 1; index < num3; ++index)
        num2 += num2 << 7 ^ (int) key[index];
      int num4 = num2 - (num2 >> 17);
      int num5 = num4 - (num4 >> 11);
      int num6 = num5 - (num5 >> 5);
      for (PropertyNameTable.Entry entry = this._entries[num6 & this._mask]; entry != null; entry = entry.Next) {
        if (entry.HashCode == num6 && PropertyNameTable.TextEquals(entry.Value, key, start, length))
          return entry.Value;
      }

      return (string) null;
    }

    public string Add(string key) {
      if (key == null)
        throw new ArgumentNullException(nameof(key));
      int length = key.Length;
      if (length == 0)
        return string.Empty;
      int num1 = length + PropertyNameTable.HashCodeRandomizer;
      for (int index = 0; index < key.Length; ++index)
        num1 += num1 << 7 ^ (int) key[index];
      int num2 = num1 - (num1 >> 17);
      int num3 = num2 - (num2 >> 11);
      int hashCode = num3 - (num3 >> 5);
      for (PropertyNameTable.Entry entry = this._entries[hashCode & this._mask]; entry != null; entry = entry.Next) {
        if (entry.HashCode == hashCode && entry.Value.Equals(key))
          return entry.Value;
      }

      return this.AddEntry(key, hashCode);
    }

    private string AddEntry(string str, int hashCode) {
      int index = hashCode & this._mask;
      PropertyNameTable.Entry entry = new PropertyNameTable.Entry(str, hashCode, this._entries[index]);
      this._entries[index] = entry;
      if (this._count++ == this._mask)
        this.Grow();
      return entry.Value;
    }

    private void Grow() {
      PropertyNameTable.Entry[] entries = this._entries;
      int num = this._mask * 2 + 1;
      PropertyNameTable.Entry[] entryArray = new PropertyNameTable.Entry[num + 1];
      PropertyNameTable.Entry next;
      for (int index1 = 0; index1 < entries.Length; ++index1) {
        for (PropertyNameTable.Entry entry = entries[index1]; entry != null; entry = next) {
          int index2 = entry.HashCode & num;
          next = entry.Next;
          entry.Next = entryArray[index2];
          entryArray[index2] = entry;
        }
      }

      this._entries = entryArray;
      this._mask = num;
    }

    private static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length) {
      if (str1.Length != str2Length)
        return false;
      for (int index = 0; index < str1.Length; ++index) {
        if ((int) str1[index] != (int) str2[str2Start + index])
          return false;
      }

      return true;
    }

    private class Entry {
      internal readonly string Value;
      internal readonly int HashCode;
      internal PropertyNameTable.Entry Next;

      internal Entry(string value, int hashCode, PropertyNameTable.Entry next) {
        this.Value = value;
        this.HashCode = hashCode;
        this.Next = next;
      }
    }
  }
}