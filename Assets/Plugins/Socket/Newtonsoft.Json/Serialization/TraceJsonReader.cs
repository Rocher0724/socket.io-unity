using System;
using System.Globalization;
using System.IO;

namespace Socket.Newtonsoft.Json.Serialization {
  internal class TraceJsonReader : JsonReader, IJsonLineInfo {
    private readonly JsonReader _innerReader;
    private readonly JsonTextWriter _textWriter;
    private readonly StringWriter _sw;

    public TraceJsonReader(JsonReader innerReader) {
      this._innerReader = innerReader;
      this._sw = new StringWriter((IFormatProvider) CultureInfo.InvariantCulture);
      this._sw.Write("Deserialized JSON: " + Environment.NewLine);
      this._textWriter = new JsonTextWriter((TextWriter) this._sw);
      this._textWriter.Formatting = Formatting.Indented;
    }

    public string GetDeserializedJsonMessage() {
      return this._sw.ToString();
    }

    public override bool Read() {
      int num = this._innerReader.Read() ? 1 : 0;
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return num != 0;
    }

    public override int? ReadAsInt32() {
      int? nullable = this._innerReader.ReadAsInt32();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return nullable;
    }

    public override string ReadAsString() {
      string str = this._innerReader.ReadAsString();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return str;
    }

    public override byte[] ReadAsBytes() {
      byte[] numArray = this._innerReader.ReadAsBytes();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return numArray;
    }

    public override Decimal? ReadAsDecimal() {
      Decimal? nullable = this._innerReader.ReadAsDecimal();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return nullable;
    }

    public override double? ReadAsDouble() {
      double? nullable = this._innerReader.ReadAsDouble();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return nullable;
    }

    public override bool? ReadAsBoolean() {
      bool? nullable = this._innerReader.ReadAsBoolean();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return nullable;
    }

    public override DateTime? ReadAsDateTime() {
      DateTime? nullable = this._innerReader.ReadAsDateTime();
      this._textWriter.WriteToken(this._innerReader, false, false, true);
      return nullable;
    }

    public override int Depth {
      get { return this._innerReader.Depth; }
    }

    public override string Path {
      get { return this._innerReader.Path; }
    }

    public override char QuoteChar {
      get { return this._innerReader.QuoteChar; }
      protected internal set { this._innerReader.QuoteChar = value; }
    }

    public override JsonToken TokenType {
      get { return this._innerReader.TokenType; }
    }

    public override object Value {
      get { return this._innerReader.Value; }
    }

    public override Type ValueType {
      get { return this._innerReader.ValueType; }
    }

    public override void Close() {
      this._innerReader.Close();
    }

    bool IJsonLineInfo.HasLineInfo() {
      IJsonLineInfo innerReader = this._innerReader as IJsonLineInfo;
      if (innerReader != null)
        return innerReader.HasLineInfo();
      return false;
    }

    int IJsonLineInfo.LineNumber {
      get {
        IJsonLineInfo innerReader = this._innerReader as IJsonLineInfo;
        if (innerReader == null)
          return 0;
        return innerReader.LineNumber;
      }
    }

    int IJsonLineInfo.LinePosition {
      get {
        IJsonLineInfo innerReader = this._innerReader as IJsonLineInfo;
        if (innerReader == null)
          return 0;
        return innerReader.LinePosition;
      }
    }
  }
}