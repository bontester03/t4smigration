using System.Security.Cryptography;
using System.Text;

namespace WebApit4s.Services
{
    public class RefreshTokenFactory
    {

        public static (string raw, string hash) CreateToken()
        {
            var rawBytes = RandomNumberGenerator.GetBytes(32); // 256-bit
            var raw = Convert.ToBase64String(rawBytes);
            using var sha = SHA256.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return (raw, hash);
        }

        public static string Hash(string raw)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}
