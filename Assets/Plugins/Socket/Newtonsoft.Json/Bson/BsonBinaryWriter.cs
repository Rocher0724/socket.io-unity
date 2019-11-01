using System;
using System.Globalization;
using System.IO;
using System.Text;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Bson {
internal class BsonBinaryWriter
  {
    private static readonly Encoding Encoding = (Encoding) new UTF8Encoding(false);
    private readonly BinaryWriter _writer;
    private byte[] _largeByteBuffer;

    public DateTimeKind DateTimeKindHandling { get; set; }

    public BsonBinaryWriter(BinaryWriter writer)
    {
      this.DateTimeKindHandling = DateTimeKind.Utc;
      this._writer = writer;
    }

    public void Flush()
    {
      this._writer.Flush();
    }

    public void Close()
    {
      this._writer.Close();
    }

    public void WriteToken(BsonToken t)
    {
      this.CalculateSize(t);
      this.WriteTokenInternal(t);
    }

    private void WriteTokenInternal(BsonToken t)
    {
      switch (t.Type)
      {
        case BsonType.Number:
          this._writer.Write(Convert.ToDouble(((BsonValue) t).Value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        case BsonType.String:
          BsonString bsonString = (BsonString) t;
          this.WriteString((string) bsonString.Value, bsonString.ByteCount, new int?(bsonString.CalculatedSize - 4));
          break;
        case BsonType.Object:
          BsonObject bsonObject = (BsonObject) t;
          this._writer.Write(bsonObject.CalculatedSize);
          foreach (BsonProperty bsonProperty in bsonObject)
          {
            this._writer.Write((sbyte) bsonProperty.Value.Type);
            this.WriteString((string) bsonProperty.Name.Value, bsonProperty.Name.ByteCount, new int?());
            this.WriteTokenInternal(bsonProperty.Value);
          }
          this._writer.Write((byte) 0);
          break;
        case BsonType.Array:
          BsonArray bsonArray = (BsonArray) t;
          this._writer.Write(bsonArray.CalculatedSize);
          ulong i = 0;
          foreach (BsonToken t1 in bsonArray)
          {
            this._writer.Write((sbyte) t1.Type);
            this.WriteString(i.ToString((IFormatProvider) CultureInfo.InvariantCulture), MathUtils.IntLength(i), new int?());
            this.WriteTokenInternal(t1);
            ++i;
          }
          this._writer.Write((byte) 0);
          break;
        case BsonType.Binary:
          BsonBinary bsonBinary = (BsonBinary) t;
          byte[] buffer = (byte[]) bsonBinary.Value;
          this._writer.Write(buffer.Length);
          this._writer.Write((byte) bsonBinary.BinaryType);
          this._writer.Write(buffer);
          break;
        case BsonType.Undefined:
          break;
        case BsonType.Oid:
          this._writer.Write((byte[]) ((BsonValue) t).Value);
          break;
        case BsonType.Boolean:
          this._writer.Write(t == BsonBoolean.True);
          break;
        case BsonType.Date:
          BsonValue bsonValue = (BsonValue) t;
          long num = 0;
          if (bsonValue.Value is DateTime)
          {
            DateTime dateTime = (DateTime) bsonValue.Value;
            if (this.DateTimeKindHandling == DateTimeKind.Utc)
              dateTime = dateTime.ToUniversalTime();
            else if (this.DateTimeKindHandling == DateTimeKind.Local)
              dateTime = dateTime.ToLocalTime();
            num = DateTimeUtils.ConvertDateTimeToJavaScriptTicks(dateTime, false);
          }
          this._writer.Write(num);
          break;
        case BsonType.Null:
          break;
        case BsonType.Regex:
          BsonRegex bsonRegex = (BsonRegex) t;
          this.WriteString((string) bsonRegex.Pattern.Value, bsonRegex.Pattern.ByteCount, new int?());
          this.WriteString((string) bsonRegex.Options.Value, bsonRegex.Options.ByteCount, new int?());
          break;
        case BsonType.Integer:
          this._writer.Write(Convert.ToInt32(((BsonValue) t).Value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        case BsonType.Long:
          this._writer.Write(Convert.ToInt64(((BsonValue) t).Value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof (t), "Unexpected token when writing BSON: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) t.Type));
      }
    }

    private void WriteString(string s, int byteCount, int? calculatedlengthPrefix)
    {
      if (calculatedlengthPrefix.HasValue)
        this._writer.Write(calculatedlengthPrefix.GetValueOrDefault());
      this.WriteUtf8Bytes(s, byteCount);
      this._writer.Write((byte) 0);
    }

    public void WriteUtf8Bytes(string s, int byteCount)
    {
      if (s == null)
        return;
      if (byteCount <= 256)
      {
        if (this._largeByteBuffer == null)
          this._largeByteBuffer = new byte[256];
        BsonBinaryWriter.Encoding.GetBytes(s, 0, s.Length, this._largeByteBuffer, 0);
        this._writer.Write(this._largeByteBuffer, 0, byteCount);
      }
      else
        this._writer.Write(BsonBinaryWriter.Encoding.GetBytes(s));
    }

    private int CalculateSize(int stringByteCount)
    {
      return stringByteCount + 1;
    }

    private int CalculateSizeWithLength(int stringByteCount, bool includeSize)
    {
      return (includeSize ? 5 : 1) + stringByteCount;
    }

    private int CalculateSize(BsonToken t)
    {
      switch (t.Type)
      {
        case BsonType.Number:
          return 8;
        case BsonType.String:
          BsonString bsonString = (BsonString) t;
          string s = (string) bsonString.Value;
          bsonString.ByteCount = s != null ? BsonBinaryWriter.Encoding.GetByteCount(s) : 0;
          bsonString.CalculatedSize = this.CalculateSizeWithLength(bsonString.ByteCount, bsonString.IncludeLength);
          return bsonString.CalculatedSize;
        case BsonType.Object:
          BsonObject bsonObject = (BsonObject) t;
          int num1 = 4;
          foreach (BsonProperty bsonProperty in bsonObject)
          {
            int num2 = 1 + this.CalculateSize((BsonToken) bsonProperty.Name) + this.CalculateSize(bsonProperty.Value);
            num1 += num2;
          }
          int num3 = num1 + 1;
          bsonObject.CalculatedSize = num3;
          return num3;
        case BsonType.Array:
          BsonArray bsonArray = (BsonArray) t;
          int num4 = 4;
          ulong i = 0;
          foreach (BsonToken t1 in bsonArray)
          {
            ++num4;
            num4 += this.CalculateSize(MathUtils.IntLength(i));
            num4 += this.CalculateSize(t1);
            ++i;
          }
          int num5 = num4 + 1;
          bsonArray.CalculatedSize = num5;
          return bsonArray.CalculatedSize;
        case BsonType.Binary:
          BsonBinary bsonBinary = (BsonBinary) t;
          bsonBinary.CalculatedSize = 5 + ((byte[]) bsonBinary.Value).Length;
          return bsonBinary.CalculatedSize;
        case BsonType.Undefined:
        case BsonType.Null:
          return 0;
        case BsonType.Oid:
          return 12;
        case BsonType.Boolean:
          return 1;
        case BsonType.Date:
          return 8;
        case BsonType.Regex:
          BsonRegex bsonRegex = (BsonRegex) t;
          int num6 = 0 + this.CalculateSize((BsonToken) bsonRegex.Pattern) + this.CalculateSize((BsonToken) bsonRegex.Options);
          bsonRegex.CalculatedSize = num6;
          return bsonRegex.CalculatedSize;
        case BsonType.Integer:
          return 4;
        case BsonType.Long:
          return 8;
        default:
          throw new ArgumentOutOfRangeException(nameof (t), "Unexpected token when writing BSON: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) t.Type));
      }
    }
  }
}
