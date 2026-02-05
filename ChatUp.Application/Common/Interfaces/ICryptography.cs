using ChatUp.Domain.Entities;

namespace ChatUp.Application.Common.Interfaces
{
    public interface ICryptography
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
