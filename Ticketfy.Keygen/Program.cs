using System;
using System.IO;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Ticketfy.Keygen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ticketfy POS - Keygen y Generador de Licencias");
            Console.WriteLine("----------------------------------------------");

            // 1. Cargar clave privada de Valcore Server
            string privateKeyPem = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCggqLqbGJkaUGB
9JcgBhM6mFLG+A5hve+yM8Ygs5PLbs2cu+PzzKOI8nC+3NjnyQk3jDjLkmtjzG/R
Ntl/JkJ7LtUksws5Q5UjUselxWqiA4GBnggPBTGB/jjOspDaa/WIVeOlPfdYiebd
I/Nh2bJ7CJUc8gnukV4sBzQJfWs4o6oNrLFYZLVMQwY/YwG3JPZJO1KwxsRGLNQe
Esfr/4w/OFxi/zJbMgHgloTttgG3D9lqDbbuCW0LgQUX9U/VjfZrLLVDfVYQo7oS
rqH+crvL/e3RVhW6pjNadBTkxzLxCfg093sH+BzI20IhhJJ8x1L+oGTuQAx0sHYZ
Qt3M0CCLAgMBAAECggEAC+qW8MN6LI057U182MZKsCt13cq1uqDwxjCc0Xmzi8Ne
pXe3jTaQn46sOydHPTIKKqMQ5mAo8+BOHuCtFgj/n4iTD2Xxe99eF6DCLLby2zNa
boaOf5r5mXaHviS4p1ajhGsn+vEWR275gkp0e7u/Se4Rr/PyM9F8BdXFEb1LdAlo
T61CDEUuLU5HdUILQDbdGeDNLcjtGtd/BwefEfhNVVxG6BlRdy/hVxVtFLV5xHa7
TgCSrJqrbuhpVBsMW0yBfOyzgBfjmMSgOZ24jq2lXbi6MtZFRynQncnGxT4ou55Q
v/mY6bEkW3mpCisqmFSEySHuvHDzPtVED4ge8vRZjQKBgQDetXjNwEyyswc5tXPx
kzl9aU8CqwWmKxUlesyfmGjvydZas35mjY6Q4SBr8vmJhTCHA4EVSjbNloYdmRe+
vRpw5chZUc9HuXfUbsHGQTNxrRhcoXtwrtBA38Hnl3gSOKRTQOL9KGxXX4u8oNJh
Q+ORXEsAhBdD+Ez0uHIVmDIqtwKBgQC4gPnXrN1mQLXnw+p1cAWa+6gRRi0kiYAh
arRoYYMFtP0ud77MxsECge7yRhQTSCDyBfQ4xKm2ZKa1EBchRQYRCMn2niHua8j0
R+4crAFHrlj7rVEnnV1qwKdchfQGjk1eie/FptLg0VtHvcWKdUKCsrzyOD5srvpO
e8V4g/90zQKBgQDPIabmbfinpzyMHshkIRKyInSavac2YhF711djA+RtSKK1nwVr
qjKzar61x7jwf1Cf8dFnlKud0GaSNqXP/58M65nIAP+w7L2XdR+CHXgUPPJQjNv6
9Iu6GqIEnGrPP6EN0WaMH8GMDdAwMr8YOYT42AblxvTAgbpJTRbGINRfxwKBgA0k
+ye3ZAqN36fEWGbHdU7GUQHyCvNIbH10+adaExiL/WGbHFfAbS29jgVqorGA+P/l
FrIYqLKa1xmdLNis7zK1epX8TBSNT0LAASG/y1ONAz/i6B43YtlhIktAK9NvvelX
UGK3cNydbbBdv74Ofo+LJlTnVuMtUB3ZSYc9eCydAoGAeuMwjIjZ4FntpJty/8jx
l73g1LvYwY04HGncODST3c3yoid/3c2ZRt5p7HYI+5lj7KrscpRdjGbms1/ZLzXw
RrBJ5rE5ZhjZPfTl38XrjZr356ryhdVlpDLnymOUS2H7posi4O2iqwhGqJHP973U
YoKNcYTNyQ2tZqZ3Jwlbyd0=
-----END PRIVATE KEY-----";

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
            
            string publicKey = rsa.ExportRSAPublicKeyPem();

            File.WriteAllText("private_key.pem", privateKeyPem);
            File.WriteAllText("public_key.pem", publicKey);

            Console.WriteLine("[OK] Claves RSA generadas y guardadas (private_key.pem, public_key.pem).");
            Console.WriteLine("[!] IMPORTANTE: Copia el contenido de public_key.pem a LicenseEnforcementService.cs");

            // 2. Crear un token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new RsaSecurityKey(rsa);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = "Ticketfy POS",
                Audience = "Ticketfy Client",
                Expires = DateTime.UtcNow.AddYears(10), // Valido por 10 años
                SigningCredentials = credentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            File.WriteAllText("license.jwt", tokenString);
            Console.WriteLine("[OK] license.jwt generado exitosamente. Validez: 10 años.");
            Console.WriteLine("Usa este archivo license.jwt para empaquetarlo en Inno Setup.");
        }
    }
}
