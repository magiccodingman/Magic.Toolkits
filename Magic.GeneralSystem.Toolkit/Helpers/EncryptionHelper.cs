using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    public static class EncryptionHelper
    {
        /// <summary>
        /// Encrypts a given plaintext using AES-256 with a password-derived key.
        /// </summary>
        public static string Encrypt(string plainText, string password)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(password);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV and encrypted data for secure decryption
            byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts AES-256 encrypted text using a password-derived key.
        /// </summary>
        public static string Decrypt(string cipherText, string password)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(password);

            byte[] fullCipher = Convert.FromBase64String(cipherText);

            // Extract IV and encrypted data
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipherBytes = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Securely hashes the password for authentication without storing plaintext.
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// Verifies that the provided password matches the stored hash.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }

        /// <summary>
        /// Derives a strong AES-256 encryption key from the provided password dynamically.
        /// </summary>
        private static byte[] DeriveKey(string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(password), 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256-bit key
        }
    }
}
