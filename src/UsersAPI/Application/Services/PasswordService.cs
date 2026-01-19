using System.Security.Cryptography;
using System.Text;
using UsersAPI.Application.Interface;

namespace UsersAPI.Application.Services;

public class PasswordService : IPasswordService
{
    public string EncryptPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool IsPasswordMatch(string password, string hash)
    {
        var passhash = EncryptPassword(password);
        return passhash == hash;
    }
}
