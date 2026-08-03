using System;
using System.Security.Cryptography;
using System.Text;

namespace NextVent.Core.Helpers;

public static class CryptoHelper
{
    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static string HashSecret(string secret) => HashPassword(secret);

    public static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    public static bool VerifySecret(string secret, string hash) => VerifyPassword(secret, hash);
}
