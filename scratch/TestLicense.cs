using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

class Program
{
    static void Main()
    {
        string _publicKeyPem = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA5XAsNhA3W+YFqNxBzsybTsSPlHcB5n+FAHGP0ySmmo93vfm56/qG
ky51yySummsFTtiyeN95z8n9h3tagT/1A6QwNRSJb8YFRayc4p1LfcDsGV8YEAvZ
v+j6UvG0XgKiFTbKQJBRTdcXutAzDXWOXm/ljDnr3FXQFWLHbGnCAYF2BE/mtzR2
tX9eTD2uAlUWt4C3njMuu60HL6Ad78V/ZxTW02lycLjPZRdzq2AlzQOhg2gHR+Ql
AfGbnftEGwsEH1VZVtWxqR6S/msQwW7zT5ePk1pqc/kE/RmftAcQQxsZIVphJpOw
9HUkjVHI0kAaJfUr6F2gzfROHkHGOubDzQIDAQAB
-----END RSA PUBLIC KEY-----";

        string licensePath = "c:\\Users\\YERSI\\.gemini\\antigravity-ide\\scratch\\NextVent\\NextVent.Desktop\\bin\\Release\\net9.0\\win-x64\\publish\\license.jwt";

        try
        {
            var tokenString = File.ReadAllText(licensePath).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();
            var rsa = RSA.Create();
            rsa.ImportFromPem(_publicKeyPem);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(tokenString, validationParameters, out SecurityToken validatedToken);
            Console.WriteLine("Token is valid!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Validation failed: " + ex.Message);
        }
    }
}
