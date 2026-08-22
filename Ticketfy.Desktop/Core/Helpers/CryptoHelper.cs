using System;
using Ticketfy.Services.Security;

namespace Ticketfy.Core.Helpers;

/// <summary>
/// Cryptographic helper delegating password hashing and verification to SecurityManager.
/// </summary>
public static class CryptoHelper
{
    public static string HashPassword(string password) => SecurityManager.HashPassword(password);
    public static string HashSecret(string secret) => SecurityManager.HashPassword(secret);

    public static bool VerifyPassword(string password, string hash) => SecurityManager.VerifyPassword(password, hash);
    public static bool VerifySecret(string secret, string hash) => SecurityManager.VerifyPassword(secret, hash);
}
