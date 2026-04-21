using System;
using System.Security.Cryptography;
using System.Text;

namespace SelfServiceAPI.Classes
{
    public static class EncryptDecryptText
    {
        const string key = "1prt56";

        public static string Encryptword(string Encryptval)
        {
            byte[] SrctArray;

            byte[] EnctArray = UTF8Encoding.UTF8.GetBytes(Encryptval);

            SrctArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider objt = new TripleDESCryptoServiceProvider();

            MD5CryptoServiceProvider objcrpt = new MD5CryptoServiceProvider();

            SrctArray = objcrpt.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

            objcrpt.Clear();

            objt.Key = SrctArray;

            objt.Mode = CipherMode.ECB;

            objt.Padding = PaddingMode.PKCS7;

            ICryptoTransform crptotrns = objt.CreateEncryptor();

            byte[] resArray = crptotrns.TransformFinalBlock(EnctArray, 0, EnctArray.Length);

            objt.Clear();

            return Convert.ToBase64String(resArray, 0, resArray.Length);
        }

        public static string Decryptword(string DecryptText)
        {
            byte[] SrctArray;

            byte[] DrctArray = Convert.FromBase64String(DecryptText);

            SrctArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider objt = new TripleDESCryptoServiceProvider();

            MD5CryptoServiceProvider objmdcript = new MD5CryptoServiceProvider();

            SrctArray = objmdcript.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));

            objmdcript.Clear();

            objt.Key = SrctArray;

            objt.Mode = CipherMode.ECB;

            objt.Padding = PaddingMode.PKCS7;

            ICryptoTransform crptotrns = objt.CreateDecryptor();

            byte[] resArray = crptotrns.TransformFinalBlock(DrctArray, 0, DrctArray.Length);

            objt.Clear();

            return UTF8Encoding.UTF8.GetString(resArray);
        }
    }
}