using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MornLib
{
    public static class MornCrypt
    {
        /// <summary>
        ///     暗号化
        /// </summary>
        /// <param name="text">平文</param>
        /// <param name="iv">128bit ブロックサイズ</param>
        /// <param name="key">256bit</param>
        /// <returns></returns>
        public static string Encrypt(string text, string iv, string key)
        {
            using var myRijndael = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.UTF8.GetBytes(iv),
                Key = Encoding.UTF8.GetBytes(key)
            };
            var encryptor = myRijndael.CreateEncryptor(myRijndael.Key, myRijndael.IV);
            using var mStream = new MemoryStream();
            using var ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(ctStream))
            {
                sw.Write(text);
            }

            var encrypted = mStream.ToArray();
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        ///     復号
        /// </summary>
        /// <param name="cipher">暗号文</param>
        /// <param name="iv">128bit ブロックサイズ</param>
        /// <param name="key">256bit</param>
        /// <returns></returns>
        public static string Decrypt(string cipher, string iv, string key)
        {
            using var rijndael = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.UTF8.GetBytes(iv),
                Key = Encoding.UTF8.GetBytes(key)
            };
            var decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            using var mStream = new MemoryStream(Convert.FromBase64String(cipher));
            using var ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(ctStream);
            return sr.ReadLine();
        }

        /// <summary>
        ///     バイト配列を暗号化
        /// </summary>
        /// <param name="data">平文バイト配列</param>
        /// <param name="iv">128bit ブロックサイズ</param>
        /// <param name="key">256bit</param>
        /// <returns>暗号化されたバイト配列</returns>
        public static byte[] EncryptBytes(byte[] data, string iv, string key)
        {
            using var rijndael = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.UTF8.GetBytes(iv),
                Key = Encoding.UTF8.GetBytes(key)
            };
            var encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);
            using var mStream = new MemoryStream();
            using var ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write);
            ctStream.Write(data, 0, data.Length);
            ctStream.FlushFinalBlock();
            return mStream.ToArray();
        }

        /// <summary>
        ///     バイト配列を復号
        /// </summary>
        /// <param name="cipher">暗号化されたバイト配列</param>
        /// <param name="iv">128bit ブロックサイズ</param>
        /// <param name="key">256bit</param>
        /// <returns>復号されたバイト配列</returns>
        public static byte[] DecryptBytes(byte[] cipher, string iv, string key)
        {
            using var rijndael = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.UTF8.GetBytes(iv),
                Key = Encoding.UTF8.GetBytes(key)
            };
            var decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            using var mStream = new MemoryStream(cipher);
            using var ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read);
            using var resultStream = new MemoryStream();
            ctStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}