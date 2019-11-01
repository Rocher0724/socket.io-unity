using System.Collections.Generic;
using System.Text;

namespace Socket.Quobject.EngineIoClientDotNet.Modules {
  public class UTF8
  {
    private static List<int> byteArray;
    private static int byteCount;
    private static int byteIndex;

    public static string Encode(string str)
    {
      List<int> intList = UTF8.Ucs2Decode(str);
      int count = intList.Count;
      int index = -1;
      StringBuilder stringBuilder = new StringBuilder();
      while (++index < count)
      {
        int codePoint = intList[index];
        stringBuilder.Append(UTF8.EncodeCodePoint(codePoint));
      }
      return stringBuilder.ToString();
    }

    public static string Decode(string byteString)
    {
      UTF8.byteArray = UTF8.Ucs2Decode(byteString);
      UTF8.byteCount = UTF8.byteArray.Count;
      UTF8.byteIndex = 0;
      List<int> array = new List<int>();
      int num;
      while ((num = UTF8.DecodeSymbol()) != -1)
        array.Add(num);
      return UTF8.Ucs2Encode(array);
    }

    private static int DecodeSymbol()
    {
      if (UTF8.byteIndex > UTF8.byteCount)
        throw new UTF8Exception("Invalid byte index");
      if (UTF8.byteIndex == UTF8.byteCount)
        return -1;
      int num1 = UTF8.byteArray[UTF8.byteIndex] & (int) byte.MaxValue;
      ++UTF8.byteIndex;
      if ((num1 & 128) == 0)
        return num1;
      if ((num1 & 224) == 192)
      {
        int num2 = UTF8.ReadContinuationByte();
        int num3 = (num1 & 31) << 6 | num2;
        if (num3 >= 128)
          return num3;
        throw new UTF8Exception("Invalid continuation byte");
      }
      if ((num1 & 240) == 224)
      {
        int num2 = UTF8.ReadContinuationByte();
        int num3 = UTF8.ReadContinuationByte();
        int num4 = (num1 & 15) << 12 | num2 << 6 | num3;
        if (num4 >= 2048)
          return num4;
        throw new UTF8Exception("Invalid continuation byte");
      }
      if ((num1 & 248) == 240)
      {
        int num2 = UTF8.ReadContinuationByte();
        int num3 = UTF8.ReadContinuationByte();
        int num4 = UTF8.ReadContinuationByte();
        int num5 = (num1 & 15) << 18 | num2 << 12 | num3 << 6 | num4;
        if (num5 >= 65536 && num5 <= 1114111)
          return num5;
      }
      throw new UTF8Exception("Invalid continuation byte");
    }

    private static int ReadContinuationByte()
    {
      if (UTF8.byteIndex >= UTF8.byteCount)
        throw new UTF8Exception("Invalid byte index");
      int num = UTF8.byteArray[UTF8.byteIndex] & (int) byte.MaxValue;
      ++UTF8.byteIndex;
      if ((num & 192) == 128)
        return num & 63;
      throw new UTF8Exception("Invalid continuation byte");
    }

    private static string EncodeCodePoint(int codePoint)
    {
      StringBuilder stringBuilder = new StringBuilder();
      if (((long) codePoint & 4294967168L) == 0L)
      {
        stringBuilder.Append((char) codePoint);
        return stringBuilder.ToString();
      }
      if (((long) codePoint & 4294965248L) == 0L)
        stringBuilder.Append((char) (codePoint >> 6 & 31 | 192));
      else if (((long) codePoint & 4294901760L) == 0L)
      {
        stringBuilder.Append((char) (codePoint >> 12 & 15 | 224));
        stringBuilder.Append(UTF8.CreateByte(codePoint, 6));
      }
      else if (((long) codePoint & 4292870144L) == 0L)
      {
        stringBuilder.Append((char) (codePoint >> 18 & 7 | 240));
        stringBuilder.Append(UTF8.CreateByte(codePoint, 12));
        stringBuilder.Append(UTF8.CreateByte(codePoint, 6));
      }
      stringBuilder.Append((char) (codePoint & 63 | 128));
      return stringBuilder.ToString();
    }

    private static char CreateByte(int codePoint, int shift)
    {
      return (char) (codePoint >> shift & 63 | 128);
    }

    private static List<int> Ucs2Decode(string str)
    {
      List<int> intList = new List<int>();
      int num1 = 0;
      int length = str.Length;
      while (num1 < length)
      {
        int num2 = (int) str[num1++];
        if (num2 >= 55296 && num2 <= 56319 && num1 < length)
        {
          int num3 = (int) str[num1++];
          if ((num3 & 64512) == 56320)
          {
            intList.Add(((num2 & 1023) << 10) + (num3 & 1023) + 65536);
          }
          else
          {
            intList.Add(num2);
            --num1;
          }
        }
        else
          intList.Add(num2);
      }
      return intList;
    }

    private static string Ucs2Encode(List<int> array)
    {
      StringBuilder stringBuilder = new StringBuilder();
      int index = -1;
      while (++index < array.Count)
      {
        int num1 = array[index];
        if (num1 > (int) ushort.MaxValue)
        {
          int num2 = num1 - 65536;
          stringBuilder.Append((char) ((int) ((uint) num2 >> 10) & 1023 | 55296));
          num1 = 56320 | num2 & 1023;
        }
        stringBuilder.Append((char) num1);
      }
      return stringBuilder.ToString();
    }
  }
}
