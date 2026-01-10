using System;
using System.Text;

namespace MornLib
{
    public static class MornByteEx
    {
        public static byte[] ToBytesBase64(this string text)
        {
            return Convert.FromBase64String(text);
        }

        public static string ToStringBase64(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static byte[] ToBytesUTF8(this string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public static string ToStringUTF8(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}