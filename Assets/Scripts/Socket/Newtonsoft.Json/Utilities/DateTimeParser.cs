using System;

namespace Socket.Newtonsoft.Json.Utilities {
  internal struct DateTimeParser {
    private static readonly int[] Power10 = new int[7] {
      -1,
      10,
      100,
      1000,
      10000,
      100000,
      1000000
    };

    private static readonly int Lzyyyy = "yyyy".Length;
    private static readonly int Lzyyyy_ = "yyyy-".Length;
    private static readonly int Lzyyyy_MM = "yyyy-MM".Length;
    private static readonly int Lzyyyy_MM_ = "yyyy-MM-".Length;
    private static readonly int Lzyyyy_MM_dd = "yyyy-MM-dd".Length;
    private static readonly int Lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;
    private static readonly int LzHH = "HH".Length;
    private static readonly int LzHH_ = "HH:".Length;
    private static readonly int LzHH_mm = "HH:mm".Length;
    private static readonly int LzHH_mm_ = "HH:mm:".Length;
    private static readonly int LzHH_mm_ss = "HH:mm:ss".Length;
    private static readonly int Lz_ = "-".Length;
    private static readonly int Lz_zz = "-zz".Length;
    public int Year;
    public int Month;
    public int Day;
    public int Hour;
    public int Minute;
    public int Second;
    public int Fraction;
    public int ZoneHour;
    public int ZoneMinute;
    public ParserTimeZone Zone;
    private char[] _text;
    private int _end;
    private const short MaxFractionDigits = 7;

    public bool Parse(char[] text, int startIndex, int length) {
      this._text = text;
      this._end = startIndex + length;
      return this.ParseDate(startIndex) && this.ParseChar(DateTimeParser.Lzyyyy_MM_dd + startIndex, 'T') &&
             this.ParseTimeAndZoneAndWhitespace(DateTimeParser.Lzyyyy_MM_ddT + startIndex);
    }

    private bool ParseDate(int start) {
      if (this.Parse4Digit(start, out this.Year) && 1 <= this.Year &&
          (this.ParseChar(start + DateTimeParser.Lzyyyy, '-') &&
           this.Parse2Digit(start + DateTimeParser.Lzyyyy_, out this.Month)) &&
          (1 <= this.Month && this.Month <= 12 && (this.ParseChar(start + DateTimeParser.Lzyyyy_MM, '-') &&
                                                   this.Parse2Digit(start + DateTimeParser.Lzyyyy_MM_,
                                                     out this.Day))) && 1 <= this.Day)
        return this.Day <= DateTime.DaysInMonth(this.Year, this.Month);
      return false;
    }

    private bool ParseTimeAndZoneAndWhitespace(int start) {
      if (this.ParseTime(ref start))
        return this.ParseZone(start);
      return false;
    }

    private bool ParseTime(ref int start) {
      if (!this.Parse2Digit(start, out this.Hour) || this.Hour > 24 ||
          (!this.ParseChar(start + DateTimeParser.LzHH, ':') ||
           !this.Parse2Digit(start + DateTimeParser.LzHH_, out this.Minute)) ||
          (this.Minute >= 60 || !this.ParseChar(start + DateTimeParser.LzHH_mm, ':') ||
           (!this.Parse2Digit(start + DateTimeParser.LzHH_mm_, out this.Second) || this.Second >= 60)) ||
          this.Hour == 24 && (this.Minute != 0 || this.Second != 0))
        return false;
      start += DateTimeParser.LzHH_mm_ss;
      if (this.ParseChar(start, '.')) {
        this.Fraction = 0;
        int num1;
        for (num1 = 0; ++start < this._end && num1 < 7; ++num1) {
          int num2 = (int) this._text[start] - 48;
          if (num2 >= 0 && num2 <= 9)
            this.Fraction = this.Fraction * 10 + num2;
          else
            break;
        }

        if (num1 < 7) {
          if (num1 == 0)
            return false;
          this.Fraction *= DateTimeParser.Power10[7 - num1];
        }

        if (this.Hour == 24 && this.Fraction != 0)
          return false;
      }

      return true;
    }

    private bool ParseZone(int start) {
      if (start < this._end) {
        char ch = this._text[start];
        switch (ch) {
          case 'Z':
          case 'z':
            this.Zone = ParserTimeZone.Utc;
            ++start;
            break;
          default:
            if (start + 2 < this._end && this.Parse2Digit(start + DateTimeParser.Lz_, out this.ZoneHour) &&
                this.ZoneHour <= 99) {
              switch (ch) {
                case '+':
                  this.Zone = ParserTimeZone.LocalEastOfUtc;
                  start += DateTimeParser.Lz_zz;
                  break;
                case '-':
                  this.Zone = ParserTimeZone.LocalWestOfUtc;
                  start += DateTimeParser.Lz_zz;
                  break;
              }
            }

            if (start < this._end) {
              if (this.ParseChar(start, ':')) {
                ++start;
                if (start + 1 < this._end && this.Parse2Digit(start, out this.ZoneMinute) && this.ZoneMinute <= 99) {
                  start += 2;
                  break;
                }

                break;
              }

              if (start + 1 < this._end && this.Parse2Digit(start, out this.ZoneMinute) && this.ZoneMinute <= 99) {
                start += 2;
                break;
              }

              break;
            }

            break;
        }
      }

      return start == this._end;
    }

    private bool Parse4Digit(int start, out int num) {
      if (start + 3 < this._end) {
        int num1 = (int) this._text[start] - 48;
        int num2 = (int) this._text[start + 1] - 48;
        int num3 = (int) this._text[start + 2] - 48;
        int num4 = (int) this._text[start + 3] - 48;
        if (0 <= num1 && num1 < 10 && (0 <= num2 && num2 < 10) &&
            (0 <= num3 && num3 < 10 && (0 <= num4 && num4 < 10))) {
          num = ((num1 * 10 + num2) * 10 + num3) * 10 + num4;
          return true;
        }
      }

      num = 0;
      return false;
    }

    private bool Parse2Digit(int start, out int num) {
      if (start + 1 < this._end) {
        int num1 = (int) this._text[start] - 48;
        int num2 = (int) this._text[start + 1] - 48;
        if (0 <= num1 && num1 < 10 && (0 <= num2 && num2 < 10)) {
          num = num1 * 10 + num2;
          return true;
        }
      }

      num = 0;
      return false;
    }

    private bool ParseChar(int start, char ch) {
      if (start < this._end)
        return (int) this._text[start] == (int) ch;
      return false;
    }
  }
}