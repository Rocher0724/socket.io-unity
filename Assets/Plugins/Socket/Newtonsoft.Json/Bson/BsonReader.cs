using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Bson {
  [Obsolete(
    "BSON reading and writing has been moved to its own package. See https://www.nuget.org/packages/Newtonsoft.Json.Bson for more details.")]
  public class BsonReader : JsonReader {
    private static readonly byte[] SeqRange1 = new byte[2] {
      (byte) 0,
      (byte) 127
    };

    private static readonly byte[] SeqRange2 = new byte[2] {
      (byte) 194,
      (byte) 223
    };

    private static readonly byte[] SeqRange3 = new byte[2] {
      (byte) 224,
      (byte) 239
    };

    private static readonly byte[] SeqRange4 = new byte[2] {
      (byte) 240,
      (byte) 244
    };

    private const int MaxCharBytesSize = 128;
    private readonly BinaryReader _reader;
    private readonly List<BsonReader.ContainerContext> _stack;
    private byte[] _byteBuffer;
    private char[] _charBuffer;
    private BsonType _currentElementType;
    private BsonReader.BsonReaderState _bsonReaderState;
    private BsonReader.ContainerContext _currentContext;
    private bool _readRootValueAsArray;
    private bool _jsonNet35BinaryCompatibility;
    private DateTimeKind _dateTimeKindHandling;

    [Obsolete("JsonNet35BinaryCompatibility will be removed in a future version of Json.NET.")]
    public bool JsonNet35BinaryCompatibility {
      get { return this._jsonNet35BinaryCompatibility; }
      set { this._jsonNet35BinaryCompatibility = value; }
    }

    public bool ReadRootValueAsArray {
      get { return this._readRootValueAsArray; }
      set { this._readRootValueAsArray = value; }
    }

    public DateTimeKind DateTimeKindHandling {
      get { return this._dateTimeKindHandling; }
      set { this._dateTimeKindHandling = value; }
    }

    public BsonReader(Stream stream)
      : this(stream, false, DateTimeKind.Local) {
    }

    public BsonReader(BinaryReader reader)
      : this(reader, false, DateTimeKind.Local) {
    }

    public BsonReader(Stream stream, bool readRootValueAsArray, DateTimeKind dateTimeKindHandling) {
      ValidationUtils.ArgumentNotNull((object) stream, nameof(stream));
      this._reader = new BinaryReader(stream);
      this._stack = new List<BsonReader.ContainerContext>();
      this._readRootValueAsArray = readRootValueAsArray;
      this._dateTimeKindHandling = dateTimeKindHandling;
    }

    public BsonReader(
      BinaryReader reader,
      bool readRootValueAsArray,
      DateTimeKind dateTimeKindHandling) {
      ValidationUtils.ArgumentNotNull((object) reader, nameof(reader));
      this._reader = reader;
      this._stack = new List<BsonReader.ContainerContext>();
      this._readRootValueAsArray = readRootValueAsArray;
      this._dateTimeKindHandling = dateTimeKindHandling;
    }

    private string ReadElement() {
      this._currentElementType = this.ReadType();
      return this.ReadString();
    }

    public override bool Read() {
      try {
        bool flag;
        switch (this._bsonReaderState) {
          case BsonReader.BsonReaderState.Normal:
            flag = this.ReadNormal();
            break;
          case BsonReader.BsonReaderState.ReferenceStart:
          case BsonReader.BsonReaderState.ReferenceRef:
          case BsonReader.BsonReaderState.ReferenceId:
            flag = this.ReadReference();
            break;
          case BsonReader.BsonReaderState.CodeWScopeStart:
          case BsonReader.BsonReaderState.CodeWScopeCode:
          case BsonReader.BsonReaderState.CodeWScopeScope:
          case BsonReader.BsonReaderState.CodeWScopeScopeObject:
          case BsonReader.BsonReaderState.CodeWScopeScopeEnd:
            flag = this.ReadCodeWScope();
            break;
          default:
            throw JsonReaderException.Create((JsonReader) this,
              "Unexpected state: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) this._bsonReaderState));
        }

        if (flag)
          return true;
        this.SetToken(JsonToken.None);
        return false;
      } catch (EndOfStreamException ex) {
        this.SetToken(JsonToken.None);
        return false;
      }
    }

    public override void Close() {
      base.Close();
      if (!this.CloseInput)
        return;
      this._reader?.Close();
    }

    private bool ReadCodeWScope() {
      switch (this._bsonReaderState) {
        case BsonReader.BsonReaderState.CodeWScopeStart:
          this.SetToken(JsonToken.PropertyName, (object) "$code");
          this._bsonReaderState = BsonReader.BsonReaderState.CodeWScopeCode;
          return true;
        case BsonReader.BsonReaderState.CodeWScopeCode:
          this.ReadInt32();
          this.SetToken(JsonToken.String, (object) this.ReadLengthString());
          this._bsonReaderState = BsonReader.BsonReaderState.CodeWScopeScope;
          return true;
        case BsonReader.BsonReaderState.CodeWScopeScope:
          if (this.CurrentState == JsonReader.State.PostValue) {
            this.SetToken(JsonToken.PropertyName, (object) "$scope");
            return true;
          }

          this.SetToken(JsonToken.StartObject);
          this._bsonReaderState = BsonReader.BsonReaderState.CodeWScopeScopeObject;
          BsonReader.ContainerContext newContext = new BsonReader.ContainerContext(BsonType.Object);
          this.PushContext(newContext);
          newContext.Length = this.ReadInt32();
          return true;
        case BsonReader.BsonReaderState.CodeWScopeScopeObject:
          int num = this.ReadNormal() ? 1 : 0;
          if (num == 0)
            return num != 0;
          if (this.TokenType != JsonToken.EndObject)
            return num != 0;
          this._bsonReaderState = BsonReader.BsonReaderState.CodeWScopeScopeEnd;
          return num != 0;
        case BsonReader.BsonReaderState.CodeWScopeScopeEnd:
          this.SetToken(JsonToken.EndObject);
          this._bsonReaderState = BsonReader.BsonReaderState.Normal;
          return true;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private bool ReadReference() {
      switch (this.CurrentState) {
        case JsonReader.State.Property:
          if (this._bsonReaderState == BsonReader.BsonReaderState.ReferenceRef) {
            this.SetToken(JsonToken.String, (object) this.ReadLengthString());
            return true;
          }

          if (this._bsonReaderState != BsonReader.BsonReaderState.ReferenceId)
            throw JsonReaderException.Create((JsonReader) this,
              "Unexpected state when reading BSON reference: " + (object) this._bsonReaderState);
          this.SetToken(JsonToken.Bytes, (object) this.ReadBytes(12));
          return true;
        case JsonReader.State.ObjectStart:
          this.SetToken(JsonToken.PropertyName, (object) "$ref");
          this._bsonReaderState = BsonReader.BsonReaderState.ReferenceRef;
          return true;
        case JsonReader.State.PostValue:
          if (this._bsonReaderState == BsonReader.BsonReaderState.ReferenceRef) {
            this.SetToken(JsonToken.PropertyName, (object) "$id");
            this._bsonReaderState = BsonReader.BsonReaderState.ReferenceId;
            return true;
          }

          if (this._bsonReaderState != BsonReader.BsonReaderState.ReferenceId)
            throw JsonReaderException.Create((JsonReader) this,
              "Unexpected state when reading BSON reference: " + (object) this._bsonReaderState);
          this.SetToken(JsonToken.EndObject);
          this._bsonReaderState = BsonReader.BsonReaderState.Normal;
          return true;
        default:
          throw JsonReaderException.Create((JsonReader) this,
            "Unexpected state when reading BSON reference: " + (object) this.CurrentState);
      }
    }

    private bool ReadNormal() {
      switch (this.CurrentState) {
        case JsonReader.State.Start:
          JsonToken newToken = !this._readRootValueAsArray ? JsonToken.StartObject : JsonToken.StartArray;
          int num1 = !this._readRootValueAsArray ? 3 : 4;
          this.SetToken(newToken);
          BsonReader.ContainerContext newContext = new BsonReader.ContainerContext((BsonType) num1);
          this.PushContext(newContext);
          newContext.Length = this.ReadInt32();
          return true;
        case JsonReader.State.Complete:
        case JsonReader.State.Closed:
          return false;
        case JsonReader.State.Property:
          this.ReadType(this._currentElementType);
          return true;
        case JsonReader.State.ObjectStart:
        case JsonReader.State.ArrayStart:
        case JsonReader.State.PostValue:
          BsonReader.ContainerContext currentContext = this._currentContext;
          if (currentContext == null)
            return false;
          int num2 = currentContext.Length - 1;
          if (currentContext.Position < num2) {
            if (currentContext.Type == BsonType.Array) {
              this.ReadElement();
              this.ReadType(this._currentElementType);
              return true;
            }

            this.SetToken(JsonToken.PropertyName, (object) this.ReadElement());
            return true;
          }

          if (currentContext.Position != num2)
            throw JsonReaderException.Create((JsonReader) this, "Read past end of current container context.");
          if (this.ReadByte() != (byte) 0)
            throw JsonReaderException.Create((JsonReader) this, "Unexpected end of object byte value.");
          this.PopContext();
          if (this._currentContext != null)
            this.MovePosition(currentContext.Length);
          this.SetToken(currentContext.Type == BsonType.Object ? JsonToken.EndObject : JsonToken.EndArray);
          return true;
        case JsonReader.State.ConstructorStart:
        case JsonReader.State.Constructor:
        case JsonReader.State.Error:
        case JsonReader.State.Finished:
          return false;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void PopContext() {
      this._stack.RemoveAt(this._stack.Count - 1);
      if (this._stack.Count == 0)
        this._currentContext = (BsonReader.ContainerContext) null;
      else
        this._currentContext = this._stack[this._stack.Count - 1];
    }

    private void PushContext(BsonReader.ContainerContext newContext) {
      this._stack.Add(newContext);
      this._currentContext = newContext;
    }

    private byte ReadByte() {
      this.MovePosition(1);
      return this._reader.ReadByte();
    }

    private void ReadType(BsonType type) {
      switch (type) {
        case BsonType.Number:
          double num = this.ReadDouble();
          if (this._floatParseHandling == FloatParseHandling.Decimal) {
            this.SetToken(JsonToken.Float,
              (object) Convert.ToDecimal((object) num, (IFormatProvider) CultureInfo.InvariantCulture));
            break;
          }

          this.SetToken(JsonToken.Float, (object) num);
          break;
        case BsonType.String:
        case BsonType.Symbol:
          this.SetToken(JsonToken.String, (object) this.ReadLengthString());
          break;
        case BsonType.Object:
          this.SetToken(JsonToken.StartObject);
          BsonReader.ContainerContext newContext1 = new BsonReader.ContainerContext(BsonType.Object);
          this.PushContext(newContext1);
          newContext1.Length = this.ReadInt32();
          break;
        case BsonType.Array:
          this.SetToken(JsonToken.StartArray);
          BsonReader.ContainerContext newContext2 = new BsonReader.ContainerContext(BsonType.Array);
          this.PushContext(newContext2);
          newContext2.Length = this.ReadInt32();
          break;
        case BsonType.Binary:
          BsonBinaryType binaryType;
          byte[] b = this.ReadBinary(out binaryType);
          this.SetToken(JsonToken.Bytes, binaryType != BsonBinaryType.Uuid ? (object) b : (object) new Guid(b));
          break;
        case BsonType.Undefined:
          this.SetToken(JsonToken.Undefined);
          break;
        case BsonType.Oid:
          this.SetToken(JsonToken.Bytes, (object) this.ReadBytes(12));
          break;
        case BsonType.Boolean:
          this.SetToken(JsonToken.Boolean, (object) Convert.ToBoolean(this.ReadByte()));
          break;
        case BsonType.Date:
          DateTime dateTime1 = DateTimeUtils.ConvertJavaScriptTicksToDateTime(this.ReadInt64());
          DateTime dateTime2;
          switch (this.DateTimeKindHandling) {
            case DateTimeKind.Unspecified:
              dateTime2 = DateTime.SpecifyKind(dateTime1, DateTimeKind.Unspecified);
              break;
            case DateTimeKind.Local:
              dateTime2 = dateTime1.ToLocalTime();
              break;
            default:
              dateTime2 = dateTime1;
              break;
          }

          this.SetToken(JsonToken.Date, (object) dateTime2);
          break;
        case BsonType.Null:
          this.SetToken(JsonToken.Null);
          break;
        case BsonType.Regex:
          this.SetToken(JsonToken.String, (object) ("/" + this.ReadString() + "/" + this.ReadString()));
          break;
        case BsonType.Reference:
          this.SetToken(JsonToken.StartObject);
          this._bsonReaderState = BsonReader.BsonReaderState.ReferenceStart;
          break;
        case BsonType.Code:
          this.SetToken(JsonToken.String, (object) this.ReadLengthString());
          break;
        case BsonType.CodeWScope:
          this.SetToken(JsonToken.StartObject);
          this._bsonReaderState = BsonReader.BsonReaderState.CodeWScopeStart;
          break;
        case BsonType.Integer:
          this.SetToken(JsonToken.Integer, (object) (long) this.ReadInt32());
          break;
        case BsonType.TimeStamp:
        case BsonType.Long:
          this.SetToken(JsonToken.Integer, (object) this.ReadInt64());
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(type), "Unexpected BsonType value: " + (object) type);
      }
    }

    private byte[] ReadBinary(out BsonBinaryType binaryType) {
      int count = this.ReadInt32();
      binaryType = (BsonBinaryType) this.ReadByte();
      if (binaryType == BsonBinaryType.BinaryOld && !this._jsonNet35BinaryCompatibility)
        count = this.ReadInt32();
      return this.ReadBytes(count);
    }

    private string ReadString() {
      this.EnsureBuffers();
      StringBuilder stringBuilder = (StringBuilder) null;
      int num1 = 0;
      int length = 0;
      int byteCount;
      while (true) {
        int num2 = length;
        byte num3;
        while (num2 < 128 && (num3 = this._reader.ReadByte()) > (byte) 0)
          this._byteBuffer[num2++] = num3;
        byteCount = num2 - length;
        num1 += byteCount;
        if (num2 >= 128 || stringBuilder != null) {
          int lastFullCharStop = this.GetLastFullCharStop(num2 - 1);
          int chars = Encoding.UTF8.GetChars(this._byteBuffer, 0, lastFullCharStop + 1, this._charBuffer, 0);
          if (stringBuilder == null)
            stringBuilder = new StringBuilder(256);
          stringBuilder.Append(this._charBuffer, 0, chars);
          if (lastFullCharStop < byteCount - 1) {
            length = byteCount - lastFullCharStop - 1;
            Array.Copy((Array) this._byteBuffer, lastFullCharStop + 1, (Array) this._byteBuffer, 0, length);
          } else if (num2 >= 128)
            length = 0;
          else
            goto label_11;
        } else
          break;
      }

      int chars1 = Encoding.UTF8.GetChars(this._byteBuffer, 0, byteCount, this._charBuffer, 0);
      this.MovePosition(num1 + 1);
      return new string(this._charBuffer, 0, chars1);
      label_11:
      this.MovePosition(num1 + 1);
      return stringBuilder.ToString();
    }

    private string ReadLengthString() {
      int count = this.ReadInt32();
      this.MovePosition(count);
      string str = this.GetString(count - 1);
      int num = (int) this._reader.ReadByte();
      return str;
    }

    private string GetString(int length) {
      if (length == 0)
        return string.Empty;
      this.EnsureBuffers();
      StringBuilder stringBuilder = (StringBuilder) null;
      int num1 = 0;
      int num2 = 0;
      do {
        int count = length - num1 > 128 - num2 ? 128 - num2 : length - num1;
        int num3 = this._reader.Read(this._byteBuffer, num2, count);
        if (num3 == 0)
          throw new EndOfStreamException("Unable to read beyond the end of the stream.");
        num1 += num3;
        int byteCount = num3 + num2;
        if (byteCount == length)
          return new string(this._charBuffer, 0,
            Encoding.UTF8.GetChars(this._byteBuffer, 0, byteCount, this._charBuffer, 0));
        int lastFullCharStop = this.GetLastFullCharStop(byteCount - 1);
        if (stringBuilder == null)
          stringBuilder = new StringBuilder(length);
        int chars = Encoding.UTF8.GetChars(this._byteBuffer, 0, lastFullCharStop + 1, this._charBuffer, 0);
        stringBuilder.Append(this._charBuffer, 0, chars);
        if (lastFullCharStop < byteCount - 1) {
          num2 = byteCount - lastFullCharStop - 1;
          Array.Copy((Array) this._byteBuffer, lastFullCharStop + 1, (Array) this._byteBuffer, 0, num2);
        } else
          num2 = 0;
      } while (num1 < length);

      return stringBuilder.ToString();
    }

    private int GetLastFullCharStop(int start) {
      int index = start;
      int num = 0;
      for (; index >= 0; --index) {
        num = this.BytesInSequence(this._byteBuffer[index]);
        switch (num) {
          case 0:
            continue;
          case 1:
            goto label_5;
          default:
            --index;
            goto label_5;
        }
      }

      label_5:
      if (num == start - index)
        return start;
      return index;
    }

    private int BytesInSequence(byte b) {
      if ((int) b <= (int) BsonReader.SeqRange1[1])
        return 1;
      if ((int) b >= (int) BsonReader.SeqRange2[0] && (int) b <= (int) BsonReader.SeqRange2[1])
        return 2;
      if ((int) b >= (int) BsonReader.SeqRange3[0] && (int) b <= (int) BsonReader.SeqRange3[1])
        return 3;
      return (int) b >= (int) BsonReader.SeqRange4[0] && (int) b <= (int) BsonReader.SeqRange4[1] ? 4 : 0;
    }

    private void EnsureBuffers() {
      if (this._byteBuffer == null)
        this._byteBuffer = new byte[128];
      if (this._charBuffer != null)
        return;
      this._charBuffer = new char[Encoding.UTF8.GetMaxCharCount(128)];
    }

    private double ReadDouble() {
      this.MovePosition(8);
      return this._reader.ReadDouble();
    }

    private int ReadInt32() {
      this.MovePosition(4);
      return this._reader.ReadInt32();
    }

    private long ReadInt64() {
      this.MovePosition(8);
      return this._reader.ReadInt64();
    }

    private BsonType ReadType() {
      this.MovePosition(1);
      return (BsonType) this._reader.ReadSByte();
    }

    private void MovePosition(int count) {
      this._currentContext.Position += count;
    }

    private byte[] ReadBytes(int count) {
      this.MovePosition(count);
      return this._reader.ReadBytes(count);
    }

    private enum BsonReaderState {
      Normal,
      ReferenceStart,
      ReferenceRef,
      ReferenceId,
      CodeWScopeStart,
      CodeWScopeCode,
      CodeWScopeScope,
      CodeWScopeScopeObject,
      CodeWScopeScopeEnd,
    }

    private class ContainerContext {
      public readonly BsonType Type;
      public int Length;
      public int Position;

      public ContainerContext(BsonType type) {
        this.Type = type;
      }
    }
  }
}