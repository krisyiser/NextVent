using System;
using System.IO;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace KeyGen
{
    class Program
    {
        static void Main(string[] args)
        {
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportRSAPrivateKeyPem();
            var publicKey = rsa.ExportRSAPublicKeyPem();

            Console.WriteLine("Public Key Generated.");

            var desktopPath = Path.Combine("..", "Ticketfy.Desktop");
            var servicePath = Path.Combine(desktopPath, "Services", "Implementations", "LicenseEnforcementService.cs");
            
            var serviceCode = File.ReadAllText(servicePath);
            
            // replace public key
            var startIdx = serviceCode.IndexOf("-----BEGIN RSA PUBLIC KEY-----");
            var endIdx = serviceCode.IndexOf("-----END RSA PUBLIC KEY-----") + "-----END RSA PUBLIC KEY-----".Length;
            
            if (startIdx >= 0 && endIdx >= 0)
            {
                var newCode = serviceCode.Substring(0, startIdx) + publicKey + serviceCode.Substring(endIdx);
                File.WriteAllText(servicePath, newCode);
                Console.WriteLine("Updated LicenseEnforcementService.cs");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            File.WriteAllText(Path.Combine(desktopPath, "license.jwt"), jwt);
            Console.WriteLine("license.jwt created.");
        }
    }
}
