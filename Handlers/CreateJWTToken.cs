﻿using Anevo.Models.User;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Anevo.Handlers
{
    public class CreateJWTToken
    {
        private Users _users;
        private JWTSettings _options;
        public CreateJWTToken(Users users, JWTSettings options)
        {
            _users = users;
            _options = options;
        }
        public CreateJWTToken(LoginTemplate users, JWTSettings options)
        {
            Users c_user = new Users();
            c_user.Email = users.Email;
            c_user.Password = users.Password;
            _users = c_user;
            _options = options;
        }
        public async Task<string> CreateToken()
        {
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, _users.Email)); // Передаем в токен Имя
            claims.Add(new Claim("level", "123")); // Передаем в токен кастомное поле
            claims.Add(new Claim(ClaimTypes.Role, _users.Email)); // Передаем в токен роль
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(1000)), // Действие токена 10 минут
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );
            var resp = new JwtSecurityTokenHandler().WriteToken(jwt);
            return resp;
        }
    }
}