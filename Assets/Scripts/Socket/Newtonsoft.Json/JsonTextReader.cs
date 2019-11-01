using System;
using System.Globalization;
using System.IO;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json {
  public class JsonTextReader : JsonReader, IJsonLineInfo {
    private const char UnicodeReplacementChar = '�';
    private const int MaximumJavascriptIntegerCharacterLength = 380;
    private readonly TextReader _reader;
    private char[] _chars;
    private int _charsUsed;
    private int _charPos;
    private int _lineStartPos;
    private int _lineNumber;
    private bool _isEndOfFile;
    private StringBuffer _stringBuffer;
    private StringReference _stringReference;
    private IArrayPool<char> _arrayPool;
    internal PropertyNameTable NameTable;

    public JsonTextReader(TextReader reader) {
      if (reader == null)
        throw new ArgumentNullException(nameof(reader));
      this._reader = reader;
      this._lineNumber = 1;
    }

    public IArrayPool<char> ArrayPool {
      get { return this._arrayPool; }
      set {
        if (value == null)
          throw new ArgumentNullException(nameof(value));
        this._arrayPool = value;
      }
    }

    private void EnsureBufferNotEmpty() {
      if (!this._stringBuffer.IsEmpty)
        return;
      this._stringBuffer = new StringBuffer(this._arrayPool, 1024);
    }

    private void SetNewLine(bool hasNextChar) {
      if (hasNextChar && this._chars[this._charPos] == '\n')
        ++this._charPos;
      this.OnNewLine(this._charPos);
    }

    private void OnNewLine(int pos) {
      ++this._lineNumber;
      this._lineStartPos = pos;
    }

    private void ParseString(char quote, ReadType readType) {
      ++this._charPos;
      this.ShiftBufferIfNeeded();
      this.ReadStringIntoBuffer(quote);
      this.ParseReadString(quote, readType);
    }

    private void ParseReadString(char quote, ReadType readType) {
      this.SetPostValueState(true);
      switch (readType) {
        case ReadType.ReadAsInt32:
          break;
        case ReadType.ReadAsBytes:
          Guid g;
          this.SetToken(JsonToken.Bytes,
            this._stringReference.Length != 0
              ? (this._stringReference.Length != 36 ||
                 !ConvertUtils.TryConvertGuid(this._stringReference.ToString(), out g)
                ? (object) Convert.FromBase64CharArray(this._stringReference.Chars, this._stringReference.StartIndex,
                  this._stringReference.Length)
                : (object) g.ToByteArray())
              : (object) CollectionUtils.ArrayEmpty<byte>(), false);
          break;
        case ReadType.ReadAsString:
          this.SetToken(JsonToken.String, (object) this._stringReference.ToString(), false);
          this._quoteChar = quote;
          break;
        case ReadType.ReadAsDecimal:
          break;
        case ReadType.ReadAsBoolean:
          break;
        default:
          DateTime dt;
          if (this._dateParseHandling != DateParseHandling.None &&
              ((readType != ReadType.ReadAsDateTime ? this._dateParseHandling : DateParseHandling.DateTime) ==
               DateParseHandling.DateTime && DateTimeUtils.TryParseDateTime(this._stringReference,
                 this.DateTimeZoneHandling, this.DateFormatString, this.Culture, out dt))) {
            this.SetToken(JsonToken.Date, (object) dt, false);
            break;
          }

          this.SetToken(JsonToken.String, (object) this._stringReference.ToString(), false);
          this._quoteChar = quote;
          break;
      }
    }

    private static void BlockCopyChars(
      char[] src,
      int srcOffset,
      char[] dst,
      int dstOffset,
      int count) {
      Buffer.BlockCopy((Array) src, srcOffset * 2, (Array) dst, dstOffset * 2, count * 2);
    }

    private void ShiftBufferIfNeeded() {
      int length = this._chars.Length;
      if ((double) (length - this._charPos) > (double) length * 0.1)
        return;
      int count = this._charsUsed - this._charPos;
      if (count > 0)
        JsonTextReader.BlockCopyChars(this._chars, this._charPos, this._chars, 0, count);
      this._lineStartPos -= this._charPos;
      this._charPos = 0;
      this._charsUsed = count;
      this._chars[this._charsUsed] = char.MinValue;
    }

    private int ReadData(bool append) {
      return this.ReadData(append, 0);
    }

    private void PrepareBufferForReadData(bool append, int charsRequired) {
      if (this._charsUsed + charsRequired < this._chars.Length - 1)
        return;
      if (append) {
        char[] dst = BufferUtils.RentBuffer(this._arrayPool,
          Math.Max(this._chars.Length * 2, this._charsUsed + charsRequired + 1));
        JsonTextReader.BlockCopyChars(this._chars, 0, dst, 0, this._chars.Length);
        BufferUtils.ReturnBuffer(this._arrayPool, this._chars);
        this._chars = dst;
      } else {
        int count = this._charsUsed - this._charPos;
        if (count + charsRequired + 1 >= this._chars.Length) {
          char[] dst = BufferUtils.RentBuffer(this._arrayPool, count + charsRequired + 1);
          if (count > 0)
            JsonTextReader.BlockCopyChars(this._chars, this._charPos, dst, 0, count);
          BufferUtils.ReturnBuffer(this._arrayPool, this._chars);
          this._chars = dst;
        } else if (count > 0)
          JsonTextReader.BlockCopyChars(this._chars, this._charPos, this._chars, 0, count);

        this._lineStartPos -= this._charPos;
        this._charPos = 0;
        this._charsUsed = count;
      }
    }

    private int ReadData(bool append, int charsRequired) {
      if (this._isEndOfFile)
        return 0;
      this.PrepareBufferForReadData(append, charsRequired);
      int num = this._reader.Read(this._chars, this._charsUsed, this._chars.Length - this._charsUsed - 1);
      this._charsUsed += num;
      if (num == 0)
        this._isEndOfFile = true;
      this._chars[this._charsUsed] = char.MinValue;
      return num;
    }

    private bool EnsureChars(int relativePosition, bool append) {
      if (this._charPos + relativePosition >= this._charsUsed)
        return this.ReadChars(relativePosition, append);
      return true;
    }

    private bool ReadChars(int relativePosition, bool append) {
      if (this._isEndOfFile)
        return false;
      int num1 = this._charPos + relativePosition - this._charsUsed + 1;
      int num2 = 0;
      do {
        int num3 = this.ReadData(append, num1 - num2);
        if (num3 != 0)
          num2 += num3;
        else
          break;
      } while (num2 < num1);

      return num2 >= num1;
    }

    public override bool Read() {
      this.EnsureBuffer();
      do {
        switch (this._currentState) {
          case JsonReader.State.Start:
          case JsonReader.State.Property:
          case JsonReader.State.ArrayStart:
          case JsonReader.State.Array:
          case JsonReader.State.ConstructorStart:
          case JsonReader.State.Constructor:
            return this.ParseValue();
          case JsonReader.State.ObjectStart:
          case JsonReader.State.Object:
            return this.ParseObject();
          case JsonReader.State.PostValue:
            continue;
          case JsonReader.State.Finished:
            goto label_6;
          default:
            goto label_13;
        }
      } while (!this.ParsePostValue(false));

      return true;
      label_6:
      if (this.EnsureChars(0, false)) {
        this.EatWhitespace();
        if (this._isEndOfFile) {
          this.SetToken(JsonToken.None);
          return false;
        }

        if (this._chars[this._charPos] != '/')
          throw JsonReaderException.Create((JsonReader) this,
            "Additional text encountered after finished reading JSON content: {0}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) this._chars[this._charPos]));
        this.ParseComment(true);
        return true;
      }

      this.SetToken(JsonToken.None);
      return false;
      label_13:
      throw JsonReaderException.Create((JsonReader) this,
        "Unexpected state: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) this.CurrentState));
    }

    public override int? ReadAsInt32() {
      return (int?) this.ReadNumberValue(ReadType.ReadAsInt32);
    }

    public override DateTime? ReadAsDateTime() {
      return (DateTime?) this.ReadStringValue(ReadType.ReadAsDateTime);
    }

    public override string ReadAsString() {
      return (string) this.ReadStringValue(ReadType.ReadAsString);
    }

    public override byte[] ReadAsBytes() {
      this.EnsureBuffer();
      bool flag = false;
      switch (this._currentState) {
        case JsonReader.State.Start:
        case JsonReader.State.Property:
        case JsonReader.State.ArrayStart:
        case JsonReader.State.Array:
        case JsonReader.State.ConstructorStart:
        case JsonReader.State.Constructor:
          char ch;
          do {
            do {
              ch = this._chars[this._charPos];
              switch (ch) {
                case char.MinValue:
                  continue;
                case '\t':
                case ' ':
                  goto label_21;
                case '\n':
                  goto label_20;
                case '\r':
                  goto label_19;
                case '"':
                case '\'':
                  goto label_6;
                case ',':
                  goto label_15;
                case '/':
                  goto label_14;
                case '[':
                  goto label_12;
                case ']':
                  goto label_16;
                case 'n':
                  goto label_13;
                case '{':
                  goto label_11;
                default:
                  goto label_22;
              }
            } while (!this.ReadNullChar());

            this.SetToken(JsonToken.None, (object) null, false);
            return (byte[]) null;
            label_6:
            this.ParseString(ch, ReadType.ReadAsBytes);
            byte[] numArray = (byte[]) this.Value;
            if (flag) {
              this.ReaderReadAndAssert();
              if (this.TokenType != JsonToken.EndObject)
                throw JsonReaderException.Create((JsonReader) this,
                  "Error reading bytes. Unexpected token: {0}.".FormatWith(
                    (IFormatProvider) CultureInfo.InvariantCulture, (object) this.TokenType));
              this.SetToken(JsonToken.Bytes, (object) numArray, false);
            }

            return numArray;
            label_11:
            ++this._charPos;
            this.SetToken(JsonToken.StartObject);
            this.ReadIntoWrappedTypeObject();
            flag = true;
            continue;
            label_12:
            ++this._charPos;
            this.SetToken(JsonToken.StartArray);
            return this.ReadArrayIntoByteArray();
            label_13:
            this.HandleNull();
            return (byte[]) null;
            label_14:
            this.ParseComment(false);
            continue;
            label_15:
            this.ProcessValueComma();
            continue;
            label_16:
            ++this._charPos;
            if (this._currentState != JsonReader.State.Array && this._currentState != JsonReader.State.ArrayStart &&
                this._currentState != JsonReader.State.PostValue)
              throw this.CreateUnexpectedCharacterException(ch);
            this.SetToken(JsonToken.EndArray);
            return (byte[]) null;
            label_19:
            this.ProcessCarriageReturn(false);
            continue;
            label_20:
            this.ProcessLineFeed();
            continue;
            label_21:
            ++this._charPos;
            continue;
            label_22:
            ++this._charPos;
          } while (char.IsWhiteSpace(ch));

          throw this.CreateUnexpectedCharacterException(ch);
        case JsonReader.State.PostValue:
          if (this.ParsePostValue(true))
            return (byte[]) null;
          goto case JsonReader.State.Start;
        case JsonReader.State.Finished:
          this.ReadFinished();
          return (byte[]) null;
        default:
          throw JsonReaderException.Create((JsonReader) this,
            "Unexpected state: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this.CurrentState));
      }
    }

    private object ReadStringValue(ReadType readType) {
      this.EnsureBuffer();
      switch (this._currentState) {
        case JsonReader.State.Start:
        case JsonReader.State.Property:
        case JsonReader.State.ArrayStart:
        case JsonReader.State.Array:
        case JsonReader.State.ConstructorStart:
        case JsonReader.State.Constructor:
          char ch;
          do {
            do {
              ch = this._chars[this._charPos];
              switch (ch) {
                case char.MinValue:
                  continue;
                case '\t':
                case ' ':
                  goto label_28;
                case '\n':
                  goto label_27;
                case '\r':
                  goto label_26;
                case '"':
                case '\'':
                  goto label_6;
                case ',':
                  goto label_22;
                case '-':
                  goto label_7;
                case '.':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                  goto label_10;
                case '/':
                  goto label_21;
                case 'I':
                  goto label_18;
                case 'N':
                  goto label_19;
                case ']':
                  goto label_23;
                case 'f':
                case 't':
                  goto label_13;
                case 'n':
                  goto label_20;
                default:
                  goto label_29;
              }
            } while (!this.ReadNullChar());

            this.SetToken(JsonToken.None, (object) null, false);
            return (object) null;
            label_6:
            this.ParseString(ch, readType);
            return this.FinishReadQuotedStringValue(readType);
            label_7:
            if (this.EnsureChars(1, true) && this._chars[this._charPos + 1] == 'I')
              return this.ParseNumberNegativeInfinity(readType);
            this.ParseNumber(readType);
            return this.Value;
            label_10:
            if (readType != ReadType.ReadAsString) {
              ++this._charPos;
              throw this.CreateUnexpectedCharacterException(ch);
            }

            this.ParseNumber(ReadType.ReadAsString);
            return this.Value;
            label_13:
            if (readType != ReadType.ReadAsString) {
              ++this._charPos;
              throw this.CreateUnexpectedCharacterException(ch);
            }

            string str = ch == 't' ? JsonConvert.True : JsonConvert.False;
            if (!this.MatchValueWithTrailingSeparator(str))
              throw this.CreateUnexpectedCharacterException(this._chars[this._charPos]);
            this.SetToken(JsonToken.String, (object) str);
            return (object) str;
            label_18:
            return this.ParseNumberPositiveInfinity(readType);
            label_19:
            return this.ParseNumberNaN(readType);
            label_20:
            this.HandleNull();
            return (object) null;
            label_21:
            this.ParseComment(false);
            continue;
            label_22:
            this.ProcessValueComma();
            continue;
            label_23:
            ++this._charPos;
            if (this._currentState != JsonReader.State.Array && this._currentState != JsonReader.State.ArrayStart &&
                this._currentState != JsonReader.State.PostValue)
              throw this.CreateUnexpectedCharacterException(ch);
            this.SetToken(JsonToken.EndArray);
            return (object) null;
            label_26:
            this.ProcessCarriageReturn(false);
            continue;
            label_27:
            this.ProcessLineFeed();
            continue;
            label_28:
            ++this._charPos;
            continue;
            label_29:
            ++this._charPos;
          } while (char.IsWhiteSpace(ch));

          throw this.CreateUnexpectedCharacterException(ch);
        case JsonReader.State.PostValue:
          if (this.ParsePostValue(true))
            return (object) null;
          goto case JsonReader.State.Start;
        case JsonReader.State.Finished:
          this.ReadFinished();
          return (object) null;
        default:
          throw JsonReaderException.Create((JsonReader) this,
            "Unexpected state: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this.CurrentState));
      }
    }

    private object FinishReadQuotedStringValue(ReadType readType) {
      switch (readType) {
        case ReadType.ReadAsBytes:
        case ReadType.ReadAsString:
          return this.Value;
        case ReadType.ReadAsDateTime:
          if (this.Value is DateTime)
            return (object) (DateTime) this.Value;
          return (object) this.ReadDateTimeString((string) this.Value);
        default:
          throw new ArgumentOutOfRangeException(nameof(readType));
      }
    }

    private JsonReaderException CreateUnexpectedCharacterException(char c) {
      return JsonReaderException.Create((JsonReader) this,
        "Unexpected character encountered while parsing value: {0}.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) c));
    }

    public override bool? ReadAsBoolean() {
      this.EnsureBuffer();
      switch (this._currentState) {
        case JsonReader.State.Start:
        case JsonReader.State.Property:
        case JsonReader.State.ArrayStart:
        case JsonReader.State.Array:
        case JsonReader.State.ConstructorStart:
        case JsonReader.State.Constructor:
          char ch;
          do {
            do {
              ch = this._chars[this._charPos];
              switch (ch) {
                case char.MinValue:
                  continue;
                case '\t':
                case ' ':
                  goto label_19;
                case '\n':
                  goto label_18;
                case '\r':
                  goto label_17;
                case '"':
                case '\'':
                  goto label_6;
                case ',':
                  goto label_13;
                case '-':
                case '.':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                  goto label_8;
                case '/':
                  goto label_12;
                case ']':
                  goto label_14;
                case 'f':
                case 't':
                  goto label_9;
                case 'n':
                  goto label_7;
                default:
                  goto label_20;
              }
            } while (!this.ReadNullChar());

            this.SetToken(JsonToken.None, (object) null, false);
            return new bool?();
            label_6:
            this.ParseString(ch, ReadType.Read);
            return this.ReadBooleanString(this._stringReference.ToString());
            label_7:
            this.HandleNull();
            return new bool?();
            label_8:
            this.ParseNumber(ReadType.Read);
            bool boolean = Convert.ToBoolean(this.Value, (IFormatProvider) CultureInfo.InvariantCulture);
            this.SetToken(JsonToken.Boolean, (object) boolean, false);
            return new bool?(boolean);
            label_9:
            bool flag = ch == 't';
            if (!this.MatchValueWithTrailingSeparator(flag ? JsonConvert.True : JsonConvert.False))
              throw this.CreateUnexpectedCharacterException(this._chars[this._charPos]);
            this.SetToken(JsonToken.Boolean, (object) flag);
            return new bool?(flag);
            label_12:
            this.ParseComment(false);
            continue;
            label_13:
            this.ProcessValueComma();
            continue;
            label_14:
            ++this._charPos;
            if (this._currentState != JsonReader.State.Array && this._currentState != JsonReader.State.ArrayStart &&
                this._currentState != JsonReader.State.PostValue)
              throw this.CreateUnexpectedCharacterException(ch);
            this.SetToken(JsonToken.EndArray);
            return new bool?();
            label_17:
            this.ProcessCarriageReturn(false);
            continue;
            label_18:
            this.ProcessLineFeed();
            continue;
            label_19:
            ++this._charPos;
            continue;
            label_20:
            ++this._charPos;
          } while (char.IsWhiteSpace(ch));

          throw this.CreateUnexpectedCharacterException(ch);
        case JsonReader.State.PostValue:
          if (this.ParsePostValue(true))
            return new bool?();
          goto case JsonReader.State.Start;
        case JsonReader.State.Finished:
          this.ReadFinished();
          return new bool?();
        default:
          throw JsonReaderException.Create((JsonReader) this,
            "Unexpected state: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this.CurrentState));
      }
    }

    private void ProcessValueComma() {
      ++this._charPos;
      if (this._currentState != JsonReader.State.PostValue) {
        this.SetToken(JsonToken.Undefined);
        JsonReaderException characterException = this.CreateUnexpectedCharacterException(',');
        --this._charPos;
        throw characterException;
      }

      this.SetStateBasedOnCurrent();
    }

    private object ReadNumberValue(ReadType readType) {
      this.EnsureBuffer();
      switch (this._currentState) {
        case JsonReader.State.Start:
        case JsonReader.State.Property:
        case JsonReader.State.ArrayStart:
        case JsonReader.State.Array:
        case JsonReader.State.ConstructorStart:
        case JsonReader.State.Constructor:
          char ch;
          do {
            do {
              ch = this._chars[this._charPos];
              switch (ch) {
                case char.MinValue:
                  continue;
                case '\t':
                case ' ':
                  goto label_21;
                case '\n':
                  goto label_20;
                case '\r':
                  goto label_19;
                case '"':
                case '\'':
                  goto label_6;
                case ',':
                  goto label_15;
                case '-':
                  goto label_10;
                case '.':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                  goto label_13;
                case '/':
                  goto label_14;
                case 'I':
                  goto label_9;
                case 'N':
                  goto label_8;
                case ']':
                  goto label_16;
                case 'n':
                  goto label_7;
                default:
                  goto label_22;
              }
            } while (!this.ReadNullChar());

            this.SetToken(JsonToken.None, (object) null, false);
            return (object) null;
            label_6:
            this.ParseString(ch, readType);
            return this.FinishReadQuotedNumber(readType);
            label_7:
            this.HandleNull();
            return (object) null;
            label_8:
            return this.ParseNumberNaN(readType);
            label_9:
            return this.ParseNumberPositiveInfinity(readType);
            label_10:
            if (this.EnsureChars(1, true) && this._chars[this._charPos + 1] == 'I')
              return this.ParseNumberNegativeInfinity(readType);
            this.ParseNumber(readType);
            return this.Value;
            label_13:
            this.ParseNumber(readType);
            return this.Value;
            label_14:
            this.ParseComment(false);
            continue;
            label_15:
            this.ProcessValueComma();
            continue;
            label_16:
            ++this._charPos;
            if (this._currentState != JsonReader.State.Array && this._currentState != JsonReader.State.ArrayStart &&
                this._currentState != JsonReader.State.PostValue)
              throw this.CreateUnexpectedCharacterException(ch);
            this.SetToken(JsonToken.EndArray);
            return (object) null;
            label_19:
            this.ProcessCarriageReturn(false);
            continue;
            label_20:
            this.ProcessLineFeed();
            continue;
            label_21:
            ++this._charPos;
            continue;
            label_22:
            ++this._charPos;
          } while (char.IsWhiteSpace(ch));

          throw this.CreateUnexpectedCharacterException(ch);
        case JsonReader.State.PostValue:
          if (this.ParsePostValue(true))
            return (object) null;
          goto case JsonReader.State.Start;
        case JsonReader.State.Finished:
          this.ReadFinished();
          return (object) null;
        default:
          throw JsonReaderException.Create((JsonReader) this,
            "Unexpected state: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this.CurrentState));
      }
    }

    private object FinishReadQuotedNumber(ReadType readType) {
      if (readType == ReadType.ReadAsInt32)
        return (object) this.ReadInt32String(this._stringReference.ToString());
      if (readType == ReadType.ReadAsDecimal)
        return (object) this.ReadDecimalString(this._stringReference.ToString());
      if (readType == ReadType.ReadAsDouble)
        return (object) this.ReadDoubleString(this._stringReference.ToString());
      throw new ArgumentOutOfRangeException(nameof(readType));
    }

    public override Decimal? ReadAsDecimal() {
      return (Decimal?) this.ReadNumberValue(ReadType.ReadAsDecimal);
    }

    public override double? ReadAsDouble() {
      return (double?) this.ReadNumberValue(ReadType.ReadAsDouble);
    }

    private void HandleNull() {
      if (this.EnsureChars(1, true)) {
        if (this._chars[this._charPos + 1] == 'u') {
          this.ParseNull();
        } else {
          this._charPos += 2;
          throw this.CreateUnexpectedCharacterException(this._chars[this._charPos - 1]);
        }
      } else {
        this._charPos = this._charsUsed;
        throw this.CreateUnexpectedEndException();
      }
    }

    private void ReadFinished() {
      if (this.EnsureChars(0, false)) {
        this.EatWhitespace();
        if (this._isEndOfFile)
          return;
        if (this._chars[this._charPos] != '/')
          throw JsonReaderException.Create((JsonReader) this,
            "Additional text encountered after finished reading JSON content: {0}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) this._chars[this._charPos]));
        this.ParseComment(false);
      }

      this.SetToken(JsonToken.None);
    }

    private bool ReadNullChar() {
      if (this._charsUsed == this._charPos) {
        if (this.ReadData(false) == 0) {
          this._isEndOfFile = true;
          return true;
        }
      } else
        ++this._charPos;

      return false;
    }

    private void EnsureBuffer() {
      if (this._chars != null)
        return;
      this._chars = BufferUtils.RentBuffer(this._arrayPool, 1024);
      this._chars[0] = char.MinValue;
    }

    private void ReadStringIntoBuffer(char quote) {
      int charPos1 = this._charPos;
      int charPos2 = this._charPos;
      int lastWritePosition = this._charPos;
      this._stringBuffer.Position = 0;
      do {
        char ch1 = this._chars[charPos1++];
        if (ch1 <= '\r') {
          if (ch1 != char.MinValue) {
            if (ch1 != '\n') {
              if (ch1 == '\r') {
                this._charPos = charPos1 - 1;
                this.ProcessCarriageReturn(true);
                charPos1 = this._charPos;
              }
            } else {
              this._charPos = charPos1 - 1;
              this.ProcessLineFeed();
              charPos1 = this._charPos;
            }
          } else if (this._charsUsed == charPos1 - 1) {
            --charPos1;
            if (this.ReadData(true) == 0) {
              this._charPos = charPos1;
              throw JsonReaderException.Create((JsonReader) this,
                "Unterminated string. Expected delimiter: {0}.".FormatWith(
                  (IFormatProvider) CultureInfo.InvariantCulture, (object) quote));
            }
          }
        } else if (ch1 != '"' && ch1 != '\'') {
          if (ch1 == '\\') {
            this._charPos = charPos1;
            if (!this.EnsureChars(0, true))
              throw JsonReaderException.Create((JsonReader) this,
                "Unterminated string. Expected delimiter: {0}.".FormatWith(
                  (IFormatProvider) CultureInfo.InvariantCulture, (object) quote));
            int writeToPosition = charPos1 - 1;
            char ch2 = this._chars[charPos1];
            ++charPos1;
            char ch3;
            switch (ch2) {
              case '"':
              case '\'':
              case '/':
                ch3 = ch2;
                break;
              case '\\':
                ch3 = '\\';
                break;
              case 'b':
                ch3 = '\b';
                break;
              case 'f':
                ch3 = '\f';
                break;
              case 'n':
                ch3 = '\n';
                break;
              case 'r':
                ch3 = '\r';
                break;
              case 't':
                ch3 = '\t';
                break;
              case 'u':
                this._charPos = charPos1;
                ch3 = this.ParseUnicode();
                if (StringUtils.IsLowSurrogate(ch3))
                  ch3 = '�';
                else if (StringUtils.IsHighSurrogate(ch3)) {
                  bool flag;
                  do {
                    flag = false;
                    if (this.EnsureChars(2, true) && this._chars[this._charPos] == '\\' &&
                        this._chars[this._charPos + 1] == 'u') {
                      char writeChar = ch3;
                      this._charPos += 2;
                      ch3 = this.ParseUnicode();
                      if (!StringUtils.IsLowSurrogate(ch3)) {
                        if (StringUtils.IsHighSurrogate(ch3)) {
                          writeChar = '�';
                          flag = true;
                        } else
                          writeChar = '�';
                      }

                      this.EnsureBufferNotEmpty();
                      this.WriteCharToBuffer(writeChar, lastWritePosition, writeToPosition);
                      lastWritePosition = this._charPos;
                    } else
                      ch3 = '�';
                  } while (flag);
                }

                charPos1 = this._charPos;
                break;
              default:
                this._charPos = charPos1;
                throw JsonReaderException.Create((JsonReader) this,
                  "Bad JSON escape sequence: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                    (object) ("\\" + ch2.ToString())));
            }

            this.EnsureBufferNotEmpty();
            this.WriteCharToBuffer(ch3, lastWritePosition, writeToPosition);
            lastWritePosition = charPos1;
          }
        }
      } while ((int) this._chars[charPos1 - 1] != (int) quote);

      this.FinishReadStringIntoBuffer(charPos1 - 1, charPos2, lastWritePosition);
    }

    private void FinishReadStringIntoBuffer(
      int charPos,
      int initialPosition,
      int lastWritePosition) {
      if (initialPosition == lastWritePosition) {
        this._stringReference = new StringReference(this._chars, initialPosition, charPos - initialPosition);
      } else {
        this.EnsureBufferNotEmpty();
        if (charPos > lastWritePosition)
          this._stringBuffer.Append(this._arrayPool, this._chars, lastWritePosition, charPos - lastWritePosition);
        this._stringReference = new StringReference(this._stringBuffer.InternalBuffer, 0, this._stringBuffer.Position);
      }

      this._charPos = charPos + 1;
    }

    private void WriteCharToBuffer(char writeChar, int lastWritePosition, int writeToPosition) {
      if (writeToPosition > lastWritePosition)
        this._stringBuffer.Append(this._arrayPool, this._chars, lastWritePosition, writeToPosition - lastWritePosition);
      this._stringBuffer.Append(this._arrayPool, writeChar);
    }

    private char ConvertUnicode(bool enoughChars) {
      if (!enoughChars)
        throw JsonReaderException.Create((JsonReader) this, "Unexpected end while parsing Unicode escape sequence.");
      int num1;
      if (!ConvertUtils.TryHexTextToInt(this._chars, this._charPos, this._charPos + 4, out num1))
        throw JsonReaderException.Create((JsonReader) this,
          "Invalid Unicode escape sequence: \\u{0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) new string(this._chars, this._charPos, 4)));
      int num2 = (int) Convert.ToChar(num1);
      this._charPos += 4;
      return (char) num2;
    }

    private char ParseUnicode() {
      return this.ConvertUnicode(this.EnsureChars(4, true));
    }

    private void ReadNumberIntoBuffer() {
      int charPos = this._charPos;
      while (true) {
        char currentChar;
        do {
          currentChar = this._chars[charPos];
          if (currentChar == char.MinValue)
            this._charPos = charPos;
          else
            goto label_5;
        } while (this._charsUsed == charPos && this.ReadData(true) != 0);

        break;
        label_5:
        if (!this.ReadNumberCharIntoBuffer(currentChar, charPos))
          ++charPos;
        else
          goto label_4;
      }

      return;
      label_4: ;
    }

    private bool ReadNumberCharIntoBuffer(char currentChar, int charPos) {
      switch (currentChar) {
        case '+':
        case '-':
        case '.':
        case '0':
        case '1':
        case '2':
        case '3':
        case '4':
        case '5':
        case '6':
        case '7':
        case '8':
        case '9':
        case 'A':
        case 'B':
        case 'C':
        case 'D':
        case 'E':
        case 'F':
        case 'X':
        case 'a':
        case 'b':
        case 'c':
        case 'd':
        case 'e':
        case 'f':
        case 'x':
          return false;
        default:
          this._charPos = charPos;
          if (char.IsWhiteSpace(currentChar) || currentChar == ',' || (currentChar == '}' || currentChar == ']') ||
              (currentChar == ')' || currentChar == '/'))
            return true;
          throw JsonReaderException.Create((JsonReader) this,
            "Unexpected character encountered while parsing number: {0}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) currentChar));
      }
    }

    private void ClearRecentString() {
      this._stringBuffer.Position = 0;
      this._stringReference = new StringReference();
    }

    private bool ParsePostValue(bool ignoreComments) {
      char c;
      while (true) {
        do {
          do {
            c = this._chars[this._charPos];
            switch (c) {
              case char.MinValue:
                if (this._charsUsed == this._charPos)
                  continue;
                goto label_4;
              case '\t':
              case ' ':
                goto label_11;
              case '\n':
                goto label_13;
              case '\r':
                goto label_12;
              case ')':
                goto label_7;
              case ',':
                goto label_10;
              case '/':
                goto label_8;
              case ']':
                goto label_6;
              case '}':
                goto label_5;
              default:
                goto label_14;
            }
          } while (this.ReadData(false) != 0);

          this._currentState = JsonReader.State.Finished;
          return false;
          label_4:
          ++this._charPos;
          continue;
          label_5:
          ++this._charPos;
          this.SetToken(JsonToken.EndObject);
          return true;
          label_6:
          ++this._charPos;
          this.SetToken(JsonToken.EndArray);
          return true;
          label_7:
          ++this._charPos;
          this.SetToken(JsonToken.EndConstructor);
          return true;
          label_8:
          this.ParseComment(!ignoreComments);
        } while (ignoreComments);

        break;
        label_11:
        ++this._charPos;
        continue;
        label_12:
        this.ProcessCarriageReturn(false);
        continue;
        label_13:
        this.ProcessLineFeed();
        continue;
        label_14:
        if (char.IsWhiteSpace(c))
          ++this._charPos;
        else
          goto label_16;
      }

      return true;
      label_10:
      ++this._charPos;
      this.SetStateBasedOnCurrent();
      return false;
      label_16:
      throw JsonReaderException.Create((JsonReader) this,
        "After parsing a value an unexpected character was encountered: {0}.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) c));
    }

    private bool ParseObject() {
      while (true) {
        char c;
        do {
          c = this._chars[this._charPos];
          switch (c) {
            case char.MinValue:
              if (this._charsUsed == this._charPos)
                continue;
              goto label_4;
            case '\t':
            case ' ':
              goto label_9;
            case '\n':
              goto label_8;
            case '\r':
              goto label_7;
            case '/':
              goto label_6;
            case '}':
              goto label_5;
            default:
              goto label_10;
          }
        } while (this.ReadData(false) != 0);

        break;
        label_4:
        ++this._charPos;
        continue;
        label_7:
        this.ProcessCarriageReturn(false);
        continue;
        label_8:
        this.ProcessLineFeed();
        continue;
        label_9:
        ++this._charPos;
        continue;
        label_10:
        if (char.IsWhiteSpace(c))
          ++this._charPos;
        else
          goto label_12;
      }

      return false;
      label_5:
      this.SetToken(JsonToken.EndObject);
      ++this._charPos;
      return true;
      label_6:
      this.ParseComment(true);
      return true;
      label_12:
      return this.ParseProperty();
    }

    private bool ParseProperty() {
      char ch = this._chars[this._charPos];
      char quote;
      switch (ch) {
        case '"':
        case '\'':
          ++this._charPos;
          quote = ch;
          this.ShiftBufferIfNeeded();
          this.ReadStringIntoBuffer(quote);
          break;
        default:
          if (!this.ValidIdentifierChar(ch))
            throw JsonReaderException.Create((JsonReader) this,
              "Invalid property identifier character: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) this._chars[this._charPos]));
          quote = char.MinValue;
          this.ShiftBufferIfNeeded();
          this.ParseUnquotedProperty();
          break;
      }

      string str = this.NameTable == null
        ? this._stringReference.ToString()
        : this.NameTable.Get(this._stringReference.Chars, this._stringReference.StartIndex,
            this._stringReference.Length) ?? this._stringReference.ToString();
      this.EatWhitespace();
      if (this._chars[this._charPos] != ':')
        throw JsonReaderException.Create((JsonReader) this,
          "Invalid character after parsing property name. Expected ':' but got: {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) this._chars[this._charPos]));
      ++this._charPos;
      this.SetToken(JsonToken.PropertyName, (object) str);
      this._quoteChar = quote;
      this.ClearRecentString();
      return true;
    }

    private bool ValidIdentifierChar(char value) {
      if (!char.IsLetterOrDigit(value) && value != '_')
        return value == '$';
      return true;
    }

    private void ParseUnquotedProperty() {
      int charPos = this._charPos;
      char currentChar;
      do {
        currentChar = this._chars[this._charPos];
        if (currentChar == char.MinValue) {
          if (this._charsUsed == this._charPos) {
            if (this.ReadData(true) == 0)
              throw JsonReaderException.Create((JsonReader) this,
                "Unexpected end while parsing unquoted property name.");
          } else {
            this._stringReference = new StringReference(this._chars, charPos, this._charPos - charPos);
            break;
          }
        }
      } while (!this.ReadUnquotedPropertyReportIfDone(currentChar, charPos));
    }

    private bool ReadUnquotedPropertyReportIfDone(char currentChar, int initialPosition) {
      if (this.ValidIdentifierChar(currentChar)) {
        ++this._charPos;
        return false;
      }

      if (!char.IsWhiteSpace(currentChar) && currentChar != ':')
        throw JsonReaderException.Create((JsonReader) this,
          "Invalid JavaScript property identifier character: {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) currentChar));
      this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
      return true;
    }

    private bool ParseValue() {
      char ch;
      while (true) {
        do {
          ch = this._chars[this._charPos];
          switch (ch) {
            case char.MinValue:
              if (this._charsUsed == this._charPos)
                continue;
              goto label_4;
            case '\t':
            case ' ':
              goto label_30;
            case '\n':
              goto label_29;
            case '\r':
              goto label_28;
            case '"':
            case '\'':
              goto label_5;
            case ')':
              goto label_27;
            case ',':
              goto label_26;
            case '-':
              goto label_17;
            case '/':
              goto label_21;
            case 'I':
              goto label_16;
            case 'N':
              goto label_15;
            case '[':
              goto label_24;
            case ']':
              goto label_25;
            case 'f':
              goto label_7;
            case 'n':
              goto label_8;
            case 't':
              goto label_6;
            case 'u':
              goto label_22;
            case '{':
              goto label_23;
            default:
              goto label_31;
          }
        } while (this.ReadData(false) != 0);

        break;
        label_4:
        ++this._charPos;
        continue;
        label_28:
        this.ProcessCarriageReturn(false);
        continue;
        label_29:
        this.ProcessLineFeed();
        continue;
        label_30:
        ++this._charPos;
        continue;
        label_31:
        if (char.IsWhiteSpace(ch))
          ++this._charPos;
        else
          goto label_33;
      }

      return false;
      label_5:
      this.ParseString(ch, ReadType.Read);
      return true;
      label_6:
      this.ParseTrue();
      return true;
      label_7:
      this.ParseFalse();
      return true;
      label_8:
      if (this.EnsureChars(1, true)) {
        switch (this._chars[this._charPos + 1]) {
          case 'e':
            this.ParseConstructor();
            break;
          case 'u':
            this.ParseNull();
            break;
          default:
            throw this.CreateUnexpectedCharacterException(this._chars[this._charPos]);
        }

        return true;
      }

      ++this._charPos;
      throw this.CreateUnexpectedEndException();
      label_15:
      this.ParseNumberNaN(ReadType.Read);
      return true;
      label_16:
      this.ParseNumberPositiveInfinity(ReadType.Read);
      return true;
      label_17:
      if (this.EnsureChars(1, true) && this._chars[this._charPos + 1] == 'I')
        this.ParseNumberNegativeInfinity(ReadType.Read);
      else
        this.ParseNumber(ReadType.Read);
      return true;
      label_21:
      this.ParseComment(true);
      return true;
      label_22:
      this.ParseUndefined();
      return true;
      label_23:
      ++this._charPos;
      this.SetToken(JsonToken.StartObject);
      return true;
      label_24:
      ++this._charPos;
      this.SetToken(JsonToken.StartArray);
      return true;
      label_25:
      ++this._charPos;
      this.SetToken(JsonToken.EndArray);
      return true;
      label_26:
      this.SetToken(JsonToken.Undefined);
      return true;
      label_27:
      ++this._charPos;
      this.SetToken(JsonToken.EndConstructor);
      return true;
      label_33:
      if (!char.IsNumber(ch) && ch != '-' && ch != '.')
        throw this.CreateUnexpectedCharacterException(ch);
      this.ParseNumber(ReadType.Read);
      return true;
    }

    private void ProcessLineFeed() {
      ++this._charPos;
      this.OnNewLine(this._charPos);
    }

    private void ProcessCarriageReturn(bool append) {
      ++this._charPos;
      this.SetNewLine(this.EnsureChars(1, append));
    }

    private void EatWhitespace() {
      while (true) {
        char c;
        do {
          c = this._chars[this._charPos];
          switch (c) {
            case char.MinValue:
              if (this._charsUsed == this._charPos)
                continue;
              goto label_5;
            case '\n':
              goto label_7;
            case '\r':
              goto label_6;
            case ' ':
              goto label_9;
            default:
              goto label_8;
          }
        } while (this.ReadData(false) != 0);

        break;
        label_5:
        ++this._charPos;
        continue;
        label_6:
        this.ProcessCarriageReturn(false);
        continue;
        label_7:
        this.ProcessLineFeed();
        continue;
        label_8:
        if (!char.IsWhiteSpace(c))
          goto label_4;
        label_9:
        ++this._charPos;
      }

      return;
      label_4: ;
    }

    private void ParseConstructor() {
      if (!this.MatchValueWithTrailingSeparator("new"))
        throw JsonReaderException.Create((JsonReader) this, "Unexpected content while parsing JSON.");
      this.EatWhitespace();
      int charPos1 = this._charPos;
      char c;
      while (true) {
        do {
          c = this._chars[this._charPos];
          if (c == char.MinValue) {
            if (this._charsUsed != this._charPos)
              goto label_6;
          } else
            goto label_7;
        } while (this.ReadData(true) != 0);

        break;
        label_7:
        if (char.IsLetterOrDigit(c))
          ++this._charPos;
        else
          goto label_9;
      }

      throw JsonReaderException.Create((JsonReader) this, "Unexpected end while parsing constructor.");
      label_6:
      int charPos2 = this._charPos;
      ++this._charPos;
      goto label_17;
      label_9:
      switch (c) {
        case '\n':
          charPos2 = this._charPos;
          this.ProcessLineFeed();
          break;
        case '\r':
          charPos2 = this._charPos;
          this.ProcessCarriageReturn(true);
          break;
        default:
          if (char.IsWhiteSpace(c)) {
            charPos2 = this._charPos;
            ++this._charPos;
            break;
          }

          if (c != '(')
            throw JsonReaderException.Create((JsonReader) this,
              "Unexpected character while parsing constructor: {0}.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) c));
          charPos2 = this._charPos;
          break;
      }

      label_17:
      this._stringReference = new StringReference(this._chars, charPos1, charPos2 - charPos1);
      string str = this._stringReference.ToString();
      this.EatWhitespace();
      if (this._chars[this._charPos] != '(')
        throw JsonReaderException.Create((JsonReader) this,
          "Unexpected character while parsing constructor: {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) this._chars[this._charPos]));
      ++this._charPos;
      this.ClearRecentString();
      this.SetToken(JsonToken.StartConstructor, (object) str);
    }

    private void ParseNumber(ReadType readType) {
      this.ShiftBufferIfNeeded();
      char firstChar = this._chars[this._charPos];
      int charPos = this._charPos;
      this.ReadNumberIntoBuffer();
      this.ParseReadNumber(readType, firstChar, charPos);
    }

    private void ParseReadNumber(ReadType readType, char firstChar, int initialPosition) {
      this.SetPostValueState(true);
      this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
      bool flag1 = char.IsDigit(firstChar) && this._stringReference.Length == 1;
      bool flag2 = firstChar == '0' && this._stringReference.Length > 1 &&
                   (this._stringReference.Chars[this._stringReference.StartIndex + 1] != '.' &&
                    this._stringReference.Chars[this._stringReference.StartIndex + 1] != 'e') &&
                   this._stringReference.Chars[this._stringReference.StartIndex + 1] != 'E';
      JsonToken newToken;
      object obj;
      switch (readType) {
        case ReadType.ReadAsInt32:
          if (flag1)
            obj = (object) ((int) firstChar - 48);
          else if (flag2) {
            string str = this._stringReference.ToString();
            try {
              obj = (object) (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt32(str, 16)
                : Convert.ToInt32(str, 8));
            } catch (Exception ex) {
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid integer.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) str), ex);
            }
          } else {
            int num;
            switch (ConvertUtils.Int32TryParse(this._stringReference.Chars, this._stringReference.StartIndex,
              this._stringReference.Length, out num)) {
              case ParseResult.Success:
                obj = (object) num;
                break;
              case ParseResult.Overflow:
                throw this.ThrowReaderError(
                  "JSON integer {0} is too large or small for an Int32.".FormatWith(
                    (IFormatProvider) CultureInfo.InvariantCulture, (object) this._stringReference.ToString()),
                  (Exception) null);
              default:
                throw this.ThrowReaderError(
                  "Input string '{0}' is not a valid integer.".FormatWith(
                    (IFormatProvider) CultureInfo.InvariantCulture, (object) this._stringReference.ToString()),
                  (Exception) null);
            }
          }

          newToken = JsonToken.Integer;
          break;
        case ReadType.ReadAsString:
          string s = this._stringReference.ToString();
          if (flag2) {
            try {
              if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                Convert.ToInt64(s, 16);
              else
                Convert.ToInt64(s, 8);
            } catch (Exception ex) {
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid number.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) s), ex);
            }
          } else {
            double result;
            if (!double.TryParse(s, NumberStyles.Float, (IFormatProvider) CultureInfo.InvariantCulture, out result))
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid number.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) this._stringReference.ToString()), (Exception) null);
          }

          newToken = JsonToken.String;
          obj = (object) s;
          break;
        case ReadType.ReadAsDecimal:
          if (flag1)
            obj = (object) ((Decimal) firstChar - new Decimal(48));
          else if (flag2) {
            string str = this._stringReference.ToString();
            try {
              obj = (object) Convert.ToDecimal(str.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(str, 16)
                : Convert.ToInt64(str, 8));
            } catch (Exception ex) {
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid decimal.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) str), ex);
            }
          } else {
            Decimal num;
            if (ConvertUtils.DecimalTryParse(this._stringReference.Chars, this._stringReference.StartIndex,
                  this._stringReference.Length, out num) != ParseResult.Success)
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid decimal.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) this._stringReference.ToString()), (Exception) null);
            obj = (object) num;
          }

          newToken = JsonToken.Float;
          break;
        case ReadType.ReadAsDouble:
          if (flag1)
            obj = (object) ((double) firstChar - 48.0);
          else if (flag2) {
            string str = this._stringReference.ToString();
            try {
              obj = (object) Convert.ToDouble(str.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(str, 16)
                : Convert.ToInt64(str, 8));
            } catch (Exception ex) {
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid double.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) str), ex);
            }
          } else {
            double result;
            if (!double.TryParse(this._stringReference.ToString(), NumberStyles.Float,
              (IFormatProvider) CultureInfo.InvariantCulture, out result))
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid double.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) this._stringReference.ToString()), (Exception) null);
            obj = (object) result;
          }

          newToken = JsonToken.Float;
          break;
        default:
          if (flag1) {
            obj = (object) ((long) firstChar - 48L);
            newToken = JsonToken.Integer;
            break;
          }

          if (flag2) {
            string str = this._stringReference.ToString();
            try {
              obj = (object) (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(str, 16)
                : Convert.ToInt64(str, 8));
            } catch (Exception ex) {
              throw this.ThrowReaderError(
                "Input string '{0}' is not a valid number.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) str), ex);
            }

            newToken = JsonToken.Integer;
            break;
          }

          long num1;
          switch (ConvertUtils.Int64TryParse(this._stringReference.Chars, this._stringReference.StartIndex,
            this._stringReference.Length, out num1)) {
            case ParseResult.Success:
              obj = (object) num1;
              newToken = JsonToken.Integer;
              break;
            case ParseResult.Overflow:
              throw this.ThrowReaderError(
                "JSON integer {0} is too large or small for an Int64.".FormatWith(
                  (IFormatProvider) CultureInfo.InvariantCulture, (object) this._stringReference.ToString()),
                (Exception) null);
            default:
              if (this._floatParseHandling == FloatParseHandling.Decimal) {
                Decimal num2;
                if (ConvertUtils.DecimalTryParse(this._stringReference.Chars, this._stringReference.StartIndex,
                      this._stringReference.Length, out num2) != ParseResult.Success)
                  throw this.ThrowReaderError(
                    "Input string '{0}' is not a valid decimal.".FormatWith(
                      (IFormatProvider) CultureInfo.InvariantCulture, (object) this._stringReference.ToString()),
                    (Exception) null);
                obj = (object) num2;
              } else {
                double result;
                if (!double.TryParse(this._stringReference.ToString(), NumberStyles.Float,
                  (IFormatProvider) CultureInfo.InvariantCulture, out result))
                  throw this.ThrowReaderError(
                    "Input string '{0}' is not a valid number.".FormatWith(
                      (IFormatProvider) CultureInfo.InvariantCulture, (object) this._stringReference.ToString()),
                    (Exception) null);
                obj = (object) result;
              }

              newToken = JsonToken.Float;
              break;
          }

          break;
      }


      this.ClearRecentString();
      this.SetToken(newToken, obj, false);
    }

    private JsonReaderException ThrowReaderError(string message, Exception ex = null) {
      this.SetToken(JsonToken.Undefined, (object) null, false);
      return JsonReaderException.Create((JsonReader) this, message, ex);
    }

    private void ParseComment(bool setToken) {
      ++this._charPos;
      if (!this.EnsureChars(1, false))
        throw JsonReaderException.Create((JsonReader) this, "Unexpected end while parsing comment.");
      bool flag;
      if (this._chars[this._charPos] == '*') {
        flag = false;
      } else {
        if (this._chars[this._charPos] != '/')
          throw JsonReaderException.Create((JsonReader) this,
            "Error parsing comment. Expected: *, got {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this._chars[this._charPos]));
        flag = true;
      }

      ++this._charPos;
      int charPos = this._charPos;
      while (true) {
        do {
          do {
            switch (this._chars[this._charPos]) {
              case char.MinValue:
                if (this._charsUsed == this._charPos)
                  continue;
                goto label_14;
              case '\n':
                goto label_20;
              case '\r':
                goto label_17;
              case '*':
                goto label_15;
              default:
                goto label_23;
            }
          } while (this.ReadData(true) != 0);

          if (!flag)
            throw JsonReaderException.Create((JsonReader) this, "Unexpected end while parsing comment.");
          this.EndComment(setToken, charPos, this._charPos);
          return;
          label_14:
          ++this._charPos;
          continue;
          label_15:
          ++this._charPos;
        } while (flag || !this.EnsureChars(0, true) || this._chars[this._charPos] != '/');

        break;
        label_17:
        if (!flag) {
          this.ProcessCarriageReturn(true);
          continue;
        }

        goto label_18;
        label_20:
        if (!flag) {
          this.ProcessLineFeed();
          continue;
        }

        goto label_21;
        label_23:
        ++this._charPos;
      }

      this.EndComment(setToken, charPos, this._charPos - 1);
      ++this._charPos;
      return;
      label_18:
      this.EndComment(setToken, charPos, this._charPos);
      return;
      label_21:
      this.EndComment(setToken, charPos, this._charPos);
    }

    private void EndComment(bool setToken, int initialPosition, int endPosition) {
      if (!setToken)
        return;
      this.SetToken(JsonToken.Comment,
        (object) new string(this._chars, initialPosition, endPosition - initialPosition));
    }

    private bool MatchValue(string value) {
      return this.MatchValue(this.EnsureChars(value.Length - 1, true), value);
    }

    private bool MatchValue(bool enoughChars, string value) {
      if (!enoughChars) {
        this._charPos = this._charsUsed;
        throw this.CreateUnexpectedEndException();
      }

      for (int index = 0; index < value.Length; ++index) {
        if ((int) this._chars[this._charPos + index] != (int) value[index]) {
          this._charPos += index;
          return false;
        }
      }

      this._charPos += value.Length;
      return true;
    }

    private bool MatchValueWithTrailingSeparator(string value) {
      if (!this.MatchValue(value))
        return false;
      if (!this.EnsureChars(0, false) || this.IsSeparator(this._chars[this._charPos]))
        return true;
      return this._chars[this._charPos] == char.MinValue;
    }

    private bool IsSeparator(char c) {
      switch (c) {
        case '\t':
        case '\n':
        case '\r':
        case ' ':
          return true;
        case ')':
          if (this.CurrentState == JsonReader.State.Constructor ||
              this.CurrentState == JsonReader.State.ConstructorStart)
            return true;
          break;
        case ',':
        case ']':
        case '}':
          return true;
        case '/':
          if (!this.EnsureChars(1, false))
            return false;
          char ch = this._chars[this._charPos + 1];
          if (ch != '*')
            return ch == '/';
          return true;
        default:
          if (char.IsWhiteSpace(c))
            return true;
          break;
      }

      return false;
    }

    private void ParseTrue() {
      if (!this.MatchValueWithTrailingSeparator(JsonConvert.True))
        throw JsonReaderException.Create((JsonReader) this, "Error parsing boolean value.");
      this.SetToken(JsonToken.Boolean, (object) true);
    }

    private void ParseNull() {
      if (!this.MatchValueWithTrailingSeparator(JsonConvert.Null))
        throw JsonReaderException.Create((JsonReader) this, "Error parsing null value.");
      this.SetToken(JsonToken.Null);
    }

    private void ParseUndefined() {
      if (!this.MatchValueWithTrailingSeparator(JsonConvert.Undefined))
        throw JsonReaderException.Create((JsonReader) this, "Error parsing undefined value.");
      this.SetToken(JsonToken.Undefined);
    }

    private void ParseFalse() {
      if (!this.MatchValueWithTrailingSeparator(JsonConvert.False))
        throw JsonReaderException.Create((JsonReader) this, "Error parsing boolean value.");
      this.SetToken(JsonToken.Boolean, (object) false);
    }

    private object ParseNumberNegativeInfinity(ReadType readType) {
      return this.ParseNumberNegativeInfinity(readType,
        this.MatchValueWithTrailingSeparator(JsonConvert.NegativeInfinity));
    }

    private object ParseNumberNegativeInfinity(ReadType readType, bool matched) {
      if (matched) {
        if (readType != ReadType.Read) {
          if (readType != ReadType.ReadAsString) {
            if (readType != ReadType.ReadAsDouble)
              goto label_7;
          } else {
            this.SetToken(JsonToken.String, (object) JsonConvert.NegativeInfinity);
            return (object) JsonConvert.NegativeInfinity;
          }
        }

        if (this._floatParseHandling == FloatParseHandling.Double) {
          this.SetToken(JsonToken.Float, (object) double.NegativeInfinity);
          return (object) double.NegativeInfinity;
        }

        label_7:
        throw JsonReaderException.Create((JsonReader) this, "Cannot read -Infinity value.");
      }

      throw JsonReaderException.Create((JsonReader) this, "Error parsing -Infinity value.");
    }

    private object ParseNumberPositiveInfinity(ReadType readType) {
      return this.ParseNumberPositiveInfinity(readType,
        this.MatchValueWithTrailingSeparator(JsonConvert.PositiveInfinity));
    }

    private object ParseNumberPositiveInfinity(ReadType readType, bool matched) {
      if (matched) {
        if (readType != ReadType.Read) {
          if (readType != ReadType.ReadAsString) {
            if (readType != ReadType.ReadAsDouble)
              goto label_7;
          } else {
            this.SetToken(JsonToken.String, (object) JsonConvert.PositiveInfinity);
            return (object) JsonConvert.PositiveInfinity;
          }
        }

        if (this._floatParseHandling == FloatParseHandling.Double) {
          this.SetToken(JsonToken.Float, (object) double.PositiveInfinity);
          return (object) double.PositiveInfinity;
        }

        label_7:
        throw JsonReaderException.Create((JsonReader) this, "Cannot read Infinity value.");
      }

      throw JsonReaderException.Create((JsonReader) this, "Error parsing Infinity value.");
    }

    private object ParseNumberNaN(ReadType readType) {
      return this.ParseNumberNaN(readType, this.MatchValueWithTrailingSeparator(JsonConvert.NaN));
    }

    private object ParseNumberNaN(ReadType readType, bool matched) {
      if (matched) {
        if (readType != ReadType.Read) {
          if (readType != ReadType.ReadAsString) {
            if (readType != ReadType.ReadAsDouble)
              goto label_7;
          } else {
            this.SetToken(JsonToken.String, (object) JsonConvert.NaN);
            return (object) JsonConvert.NaN;
          }
        }

        if (this._floatParseHandling == FloatParseHandling.Double) {
          this.SetToken(JsonToken.Float, (object) double.NaN);
          return (object) double.NaN;
        }

        label_7:
        throw JsonReaderException.Create((JsonReader) this, "Cannot read NaN value.");
      }

      throw JsonReaderException.Create((JsonReader) this, "Error parsing NaN value.");
    }

    public override void Close() {
      base.Close();
      if (this._chars != null) {
        BufferUtils.ReturnBuffer(this._arrayPool, this._chars);
        this._chars = (char[]) null;
      }

      if (this.CloseInput)
        this._reader?.Close();
      this._stringBuffer.Clear(this._arrayPool);
    }

    public bool HasLineInfo() {
      return true;
    }

    public int LineNumber {
      get {
        if (this.CurrentState == JsonReader.State.Start && this.LinePosition == 0 &&
            this.TokenType != JsonToken.Comment)
          return 0;
        return this._lineNumber;
      }
    }

    public int LinePosition {
      get { return this._charPos - this._lineStartPos; }
    }
  }
}