namespace WebApit4s.Services.Options
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // for HMAC; use RSA provider if you switch to RSA
        public int AccessTokenMinutes { get; set; } = 15;
    }
}
