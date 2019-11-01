using System;
using System.Globalization;
using System.IO;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json {
public class JsonTextWriter : JsonWriter
  {
    private const int IndentCharBufferSize = 12;
    private readonly TextWriter _writer;
    private Base64Encoder _base64Encoder;
    private char _indentChar;
    private int _indentation;
    private char _quoteChar;
    private bool _quoteName;
    private bool[] _charEscapeFlags;
    private char[] _writeBuffer;
    private IArrayPool<char> _arrayPool;
    private char[] _indentChars;

    private Base64Encoder Base64Encoder
    {
      get
      {
        if (this._base64Encoder == null)
          this._base64Encoder = new Base64Encoder(this._writer);
        return this._base64Encoder;
      }
    }

    public IArrayPool<char> ArrayPool
    {
      get
      {
        return this._arrayPool;
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException(nameof (value));
        this._arrayPool = value;
      }
    }

    public int Indentation
    {
      get
      {
        return this._indentation;
      }
      set
      {
        if (value < 0)
          throw new ArgumentException("Indentation value must be greater than 0.");
        this._indentation = value;
      }
    }

    public char QuoteChar
    {
      get
      {
        return this._quoteChar;
      }
      set
      {
        if (value != '"' && value != '\'')
          throw new ArgumentException("Invalid JavaScript string quote character. Valid quote characters are ' and \".");
        this._quoteChar = value;
        this.UpdateCharEscapeFlags();
      }
    }

    public char IndentChar
    {
      get
      {
        return this._indentChar;
      }
      set
      {
        if ((int) value == (int) this._indentChar)
          return;
        this._indentChar = value;
        this._indentChars = (char[]) null;
      }
    }

    public bool QuoteName
    {
      get
      {
        return this._quoteName;
      }
      set
      {
        this._quoteName = value;
      }
    }

    public JsonTextWriter(TextWriter textWriter)
    {
      if (textWriter == null)
        throw new ArgumentNullException(nameof (textWriter));
      this._writer = textWriter;
      this._quoteChar = '"';
      this._quoteName = true;
      this._indentChar = ' ';
      this._indentation = 2;
      this.UpdateCharEscapeFlags();
    }

    public override void Flush()
    {
      this._writer.Flush();
    }

    public override void Close()
    {
      base.Close();
      this.CloseBufferAndWriter();
    }

    private void CloseBufferAndWriter()
    {
      if (this._writeBuffer != null)
      {
        BufferUtils.ReturnBuffer(this._arrayPool, this._writeBuffer);
        this._writeBuffer = (char[]) null;
      }
      if (!this.CloseOutput)
        return;
      this._writer?.Close();
    }

    public override void WriteStartObject()
    {
      this.InternalWriteStart(JsonToken.StartObject, JsonContainerType.Object);
      this._writer.Write('{');
    }

    public override void WriteStartArray()
    {
      this.InternalWriteStart(JsonToken.StartArray, JsonContainerType.Array);
      this._writer.Write('[');
    }

    public override void WriteStartConstructor(string name)
    {
      this.InternalWriteStart(JsonToken.StartConstructor, JsonContainerType.Constructor);
      this._writer.Write("new ");
      this._writer.Write(name);
      this._writer.Write('(');
    }

    protected override void WriteEnd(JsonToken token)
    {
      switch (token)
      {
        case JsonToken.EndObject:
          this._writer.Write('}');
          break;
        case JsonToken.EndArray:
          this._writer.Write(']');
          break;
        case JsonToken.EndConstructor:
          this._writer.Write(')');
          break;
        default:
          throw JsonWriterException.Create((JsonWriter) this, "Invalid JsonToken: " + (object) token, (Exception) null);
      }
    }

    public override void WritePropertyName(string name)
    {
      this.InternalWritePropertyName(name);
      this.WriteEscapedString(name, this._quoteName);
      this._writer.Write(':');
    }

    public override void WritePropertyName(string name, bool escape)
    {
      this.InternalWritePropertyName(name);
      if (escape)
      {
        this.WriteEscapedString(name, this._quoteName);
      }
      else
      {
        if (this._quoteName)
          this._writer.Write(this._quoteChar);
        this._writer.Write(name);
        if (this._quoteName)
          this._writer.Write(this._quoteChar);
      }
      this._writer.Write(':');
    }

    internal override void OnStringEscapeHandlingChanged()
    {
      this.UpdateCharEscapeFlags();
    }

    private void UpdateCharEscapeFlags()
    {
      this._charEscapeFlags = JavaScriptUtils.GetCharEscapeFlags(this.StringEscapeHandling, this._quoteChar);
    }

    protected override void WriteIndent()
    {
      int val1 = this.Top * this._indentation;
      int index = this.SetIndentChars();
      this._writer.Write(this._indentChars, 0, index + Math.Min(val1, 12));
      while ((val1 -= 12) > 0)
        this._writer.Write(this._indentChars, index, Math.Min(val1, 12));
    }

    private int SetIndentChars()
    {
      string newLine = this._writer.NewLine;
      int length = newLine.Length;
      bool flag = this._indentChars != null && this._indentChars.Length == 12 + length;
      if (flag)
      {
        for (int index = 0; index != length; ++index)
        {
          if ((int) newLine[index] != (int) this._indentChars[index])
          {
            flag = false;
            break;
          }
        }
      }
      if (!flag)
        this._indentChars = (newLine + new string(this._indentChar, 12)).ToCharArray();
      return length;
    }

    protected override void WriteValueDelimiter()
    {
      this._writer.Write(',');
    }

    protected override void WriteIndentSpace()
    {
      this._writer.Write(' ');
    }

    private void WriteValueInternal(string value, JsonToken token)
    {
      this._writer.Write(value);
    }

    public override void WriteValue(object value)
    {
      base.WriteValue(value);
    }

    public override void WriteNull()
    {
      this.InternalWriteValue(JsonToken.Null);
      this.WriteValueInternal(JsonConvert.Null, JsonToken.Null);
    }

    public override void WriteUndefined()
    {
      this.InternalWriteValue(JsonToken.Undefined);
      this.WriteValueInternal(JsonConvert.Undefined, JsonToken.Undefined);
    }

    public override void WriteRaw(string json)
    {
      this.InternalWriteRaw();
      this._writer.Write(json);
    }

    public override void WriteValue(string value)
    {
      this.InternalWriteValue(JsonToken.String);
      if (value == null)
        this.WriteValueInternal(JsonConvert.Null, JsonToken.Null);
      else
        this.WriteEscapedString(value, true);
    }

    private void WriteEscapedString(string value, bool quote)
    {
      this.EnsureWriteBuffer();
      JavaScriptUtils.WriteEscapedJavaScriptString(this._writer, value, this._quoteChar, quote, this._charEscapeFlags, this.StringEscapeHandling, this._arrayPool, ref this._writeBuffer);
    }

    public override void WriteValue(int value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue((long) value);
    }

    [CLSCompliant(false)]
    public override void WriteValue(uint value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue((long) value);
    }

    public override void WriteValue(long value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue(value);
    }

    [CLSCompliant(false)]
    public override void WriteValue(ulong value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue(value, false);
    }

    public override void WriteValue(float value)
    {
      this.InternalWriteValue(JsonToken.Float);
      this.WriteValueInternal(JsonConvert.ToString(value, this.FloatFormatHandling, this.QuoteChar, false), JsonToken.Float);
    }

    public override void WriteValue(float? value)
    {
      if (!value.HasValue)
      {
        this.WriteNull();
      }
      else
      {
        this.InternalWriteValue(JsonToken.Float);
        this.WriteValueInternal(JsonConvert.ToString(value.GetValueOrDefault(), this.FloatFormatHandling, this.QuoteChar, true), JsonToken.Float);
      }
    }

    public override void WriteValue(double value)
    {
      this.InternalWriteValue(JsonToken.Float);
      this.WriteValueInternal(JsonConvert.ToString(value, this.FloatFormatHandling, this.QuoteChar, false), JsonToken.Float);
    }

    public override void WriteValue(double? value)
    {
      if (!value.HasValue)
      {
        this.WriteNull();
      }
      else
      {
        this.InternalWriteValue(JsonToken.Float);
        this.WriteValueInternal(JsonConvert.ToString(value.GetValueOrDefault(), this.FloatFormatHandling, this.QuoteChar, true), JsonToken.Float);
      }
    }

    public override void WriteValue(bool value)
    {
      this.InternalWriteValue(JsonToken.Boolean);
      this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Boolean);
    }

    public override void WriteValue(short value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue((long) value);
    }

    [CLSCompliant(false)]
    public override void WriteValue(ushort value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue((long) value);
    }

    public override void WriteValue(char value)
    {
      this.InternalWriteValue(JsonToken.String);
      this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.String);
    }

    public override void WriteValue(byte value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue((long) value);
    }

    [CLSCompliant(false)]
    public override void WriteValue(sbyte value)
    {
      this.InternalWriteValue(JsonToken.Integer);
      this.WriteIntegerValue((long) value);
    }

    public override void WriteValue(Decimal value)
    {
      this.InternalWriteValue(JsonToken.Float);
      this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Float);
    }

    public override void WriteValue(DateTime value)
    {
      this.InternalWriteValue(JsonToken.Date);
      value = DateTimeUtils.EnsureDateTime(value, this.DateTimeZoneHandling);
      if (string.IsNullOrEmpty(this.DateFormatString))
      {
        this._writer.Write(this._writeBuffer, 0, this.WriteValueToBuffer(value));
      }
      else
      {
        this._writer.Write(this._quoteChar);
        this._writer.Write(value.ToString(this.DateFormatString, (IFormatProvider) this.Culture));
        this._writer.Write(this._quoteChar);
      }
    }

    private int WriteValueToBuffer(DateTime value)
    {
      this.EnsureWriteBuffer();
      int num1 = 0;
      char[] writeBuffer1 = this._writeBuffer;
      int index1 = num1;
      int start = index1 + 1;
      int quoteChar1 = (int) this._quoteChar;
      writeBuffer1[index1] = (char) quoteChar1;
      int num2 = DateTimeUtils.WriteDateTimeString(this._writeBuffer, start, value, new TimeSpan?(), value.Kind, this.DateFormatHandling);
      char[] writeBuffer2 = this._writeBuffer;
      int index2 = num2;
      int num3 = index2 + 1;
      int quoteChar2 = (int) this._quoteChar;
      writeBuffer2[index2] = (char) quoteChar2;
      return num3;
    }

    public override void WriteValue(byte[] value)
    {
      if (value == null)
      {
        this.WriteNull();
      }
      else
      {
        this.InternalWriteValue(JsonToken.Bytes);
        this._writer.Write(this._quoteChar);
        this.Base64Encoder.Encode(value, 0, value.Length);
        this.Base64Encoder.Flush();
        this._writer.Write(this._quoteChar);
      }
    }

    public override void WriteValue(Guid value)
    {
      this.InternalWriteValue(JsonToken.String);
      string str = value.ToString("D", (IFormatProvider) CultureInfo.InvariantCulture);
      this._writer.Write(this._quoteChar);
      this._writer.Write(str);
      this._writer.Write(this._quoteChar);
    }

    public override void WriteValue(TimeSpan value)
    {
      this.InternalWriteValue(JsonToken.String);
      string str = value.ToString();
      this._writer.Write(this._quoteChar);
      this._writer.Write(str);
      this._writer.Write(this._quoteChar);
    }

    public override void WriteValue(Uri value)
    {
      if (value == (Uri) null)
      {
        this.WriteNull();
      }
      else
      {
        this.InternalWriteValue(JsonToken.String);
        this.WriteEscapedString(value.OriginalString, true);
      }
    }

    public override void WriteComment(string text)
    {
      this.InternalWriteComment();
      this._writer.Write("/*");
      this._writer.Write(text);
      this._writer.Write("*/");
    }

    public override void WriteWhitespace(string ws)
    {
      this.InternalWriteWhitespace(ws);
      this._writer.Write(ws);
    }

    private void EnsureWriteBuffer()
    {
      if (this._writeBuffer != null)
        return;
      this._writeBuffer = BufferUtils.RentBuffer(this._arrayPool, 35);
    }

    private void WriteIntegerValue(long value)
    {
      if (value >= 0L && value <= 9L)
      {
        this._writer.Write((char) (48UL + (ulong) value));
      }
      else
      {
        bool negative = value < 0L;
        this.WriteIntegerValue(negative ? (ulong) -value : (ulong) value, negative);
      }
    }

    private void WriteIntegerValue(ulong uvalue, bool negative)
    {
      if (!negative & uvalue <= 9UL)
        this._writer.Write((char) (48UL + uvalue));
      else
        this._writer.Write(this._writeBuffer, 0, this.WriteNumberToBuffer(uvalue, negative));
    }

    private int WriteNumberToBuffer(ulong value, bool negative)
    {
      this.EnsureWriteBuffer();
      int num1 = MathUtils.IntLength(value);
      if (negative)
      {
        ++num1;
        this._writeBuffer[0] = '-';
      }
      int num2 = num1;
      do
      {
        this._writeBuffer[--num2] = (char) (48UL + value % 10UL);
        value /= 10UL;
      }
      while (value != 0UL);
      return num1;
    }
  }
}
