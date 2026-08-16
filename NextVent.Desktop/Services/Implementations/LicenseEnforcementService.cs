using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using NextVent.Services.Interfaces;

namespace NextVent.Services.Implementations;

public class LicenseEnforcementService : ILicenseEnforcementService
{
    // Esta es la contraparte PÚBLICA de la llave generada en el servidor.
    private readonly string _publicKeyPem = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA3PFWxVE8VDgvM4duY7IG7usZD9+v+RFYcXOt5HfLGAsR4dRXe7Sj
n1wx8m+qfW6DACJkLdsoisrGOw+g7sPCm2zo08MlilPdrO9Kjz4oA/PcEfiovT8W
LdSueog4XgvcY8l9tWekSI0gGYVEQqQpXUbEVKubNHgKaaT5LLkvvZPePQClLcmO
d2hxw/z67AlJT4NSwDmDU8bKoD08jlOuUtkJYrpVr1xjCCjkz7er+IF7JnP73rAN
G2HDFPevvGanTmKKX2DWPd8EnpkLKVIzCE7TeEIQF8QjScsDbpEC/4gp71a1JHOp
KX4N79HJMXR5TcS0IH7mdXHzsWciQJxoDQIDAQAB
-----END RSA PUBLIC KEY-----";

    public bool IsSystemLocked()
    {
        if (!File.Exists("license.jwt")) 
            return true; // No hay licencia, Kill Switch ACTIVADO.

        var tokenString = File.ReadAllText("license.jwt");
        var tokenHandler = new JwtSecurityTokenHandler();
        
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_publicKeyPem);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true, // Verifica que 'exp' no haya caducado
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            tokenHandler.ValidateToken(tokenString, validationParameters, out _);
            return false; // Token válido, sistema liberado.
        }
        catch (Exception)
        {
            // Caducado, alterado o firma inválida.
            return true; // Kill Switch ACTIVADO.
        }
    }
}
