using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApit4s.Identity;
using WebApit4s.Services.Interfaces;
using WebApit4s.Services.Options;

namespace WebApit4s.Services
{
    public sealed class JwtFactory : IJwtFactory
    {
        private readonly IOptions<JwtOptions> _jwtOptions;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtFactory(IOptions<JwtOptions> jwtOptions, UserManager<ApplicationUser> userManager)
        {
            _jwtOptions = jwtOptions;
            _userManager = userManager;
        }

        public async Task<string> CreateAccessTokenAsync(ApplicationUser user, CancellationToken ct = default)
        {
            var opts = _jwtOptions.Value;

            // core identity claims
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // roles (optional but common)
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // add any custom claims here as needed (tenant, child context, etc.)

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: opts.Issuer,
                audience: opts.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(opts.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
