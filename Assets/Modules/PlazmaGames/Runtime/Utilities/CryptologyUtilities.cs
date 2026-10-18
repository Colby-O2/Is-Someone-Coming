using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace PlazmaGames.Core.Utils
{
    public static class CryptologyUtilities
    {
        /// <summary>
        /// Create a MD5 hash from a string from <paramref name="input"/>.
        /// </summary>
        public static byte[] CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return hashBytes;
            }
        }

        /// <summary>
        /// Generate the encrpytion key. This will need to be modified in the future
        /// for better security. i.e. Key should not be equal IV. 
        /// </summary>
        public static void InitializesEncryptor(ref SymmetricAlgorithm key, string encryptionKey)
        {
            key = new DESCryptoServiceProvider();
            var fullHash = CreateMD5(encryptionKey);
            var keyBytes = new byte[8];
            Array.Copy(fullHash, keyBytes, 8);
            key.Key = keyBytes;
            key.IV = keyBytes;
        }
    }
}
