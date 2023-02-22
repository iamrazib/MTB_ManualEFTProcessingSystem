using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ManualEFTProcessingSystem.DBUtility
{
    class Utility
    {
        static string _key = "ABCDEFFEDCBAABCDEFFEDCBAABCDEFFEDCBAABCDEFFEDCBA";
        static string _vector = "ABCDEFFEDCBABCDE";

        public static int GetRandomTINForSMSBanking()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            Random random = new Random();
            int intElements = 4;

            for (int i = 0; i < intElements; i++)
            {
                builder.Append(random.Next(1, 9));
            }

            return Int32.Parse(builder.ToString());
        }

        public static string EncryptString(string stringToEncrypt)
        {
            if (stringToEncrypt == null || stringToEncrypt.Length == 0)
            {
                return "";
            }

            TripleDESCryptoServiceProvider _cryptoProvider = new TripleDESCryptoServiceProvider();
            try
            {
                _cryptoProvider.Key = HexToByte(_key);
                _cryptoProvider.IV = HexToByte(_vector);


                byte[] valBytes = Encoding.Unicode.GetBytes(stringToEncrypt);
                ICryptoTransform transform = _cryptoProvider.CreateEncryptor();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Write);
                cs.Write(valBytes, 0, valBytes.Length);
                cs.FlushFinalBlock();
                byte[] returnBytes = ms.ToArray();
                cs.Close();
                return Convert.ToBase64String(returnBytes);
            }
            catch
            {
                return "";
            }
        }

        public static string DecryptString(string stringToDecrypt)
        {
            if (stringToDecrypt == null || stringToDecrypt.Length == 0)
            {
                return "";
            }

            TripleDESCryptoServiceProvider _cryptoProvider = new TripleDESCryptoServiceProvider();

            try
            {
                _cryptoProvider.Key = HexToByte(_key);
                _cryptoProvider.IV = HexToByte(_vector);

                byte[] valBytes = Convert.FromBase64String(stringToDecrypt);
                ICryptoTransform transform = _cryptoProvider.CreateDecryptor();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Write);
                cs.Write(valBytes, 0, valBytes.Length);
                cs.FlushFinalBlock();
                byte[] returnBytes = ms.ToArray();
                cs.Close();
                return Encoding.Unicode.GetString(returnBytes);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array
        /// </summary>
        /// <param name="hexString">hex value</param>
        /// <returns>byte array</returns>
        /// 
        private static byte[] HexToByte(string hexString)
        {
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] =
                Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
    

    }
}
