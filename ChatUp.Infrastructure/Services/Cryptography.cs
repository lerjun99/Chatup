using ChatUp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ChatUp.Infrastructure.Services.Cryptography
{
    public class Cryptography : ICryptography
    {
        private readonly string _encryptionKey;

        public Cryptography(IConfiguration configuration)
        {
            _encryptionKey = configuration["EncryptionKey"]
                             ?? "0ram@1234xxxxxxxxxxtttttuuuuuiiiiio";
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] clearBytes = Encoding.Unicode.GetBytes(plainText);

            using (Aes encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(_encryptionKey, new byte[] {
                    0x49, 0x76, 0x61, 0x6e,
                    0x20, 0x4d, 0x65, 0x64,
                    0x76, 0x65, 0x64, 0x65,
                    0x76
                });

                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearBytes, 0, clearBytes.Length);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            // Ensure no whitespace issues
            cipherText = cipherText.Replace(" ", "+");

            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (Aes encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(_encryptionKey, new byte[] {
                    0x49, 0x76, 0x61, 0x6e,
                    0x20, 0x4d, 0x65, 0x64,
                    0x76, 0x65, 0x64, 0x65,
                    0x76
                });

                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                }

                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }
    }
}
