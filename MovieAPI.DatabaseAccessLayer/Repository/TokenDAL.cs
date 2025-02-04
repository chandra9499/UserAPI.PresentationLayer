﻿using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MovieAPI.DatabaseAccessLayer.Context;
using MovieAPI.DatabaseAccessLayer.Interface;
using MovieAPI.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MovieAPI.DatabaseAccessLayer.Repository
{
    public class TokenDAL : ITokenDAL
    {
        private readonly IConfiguration _configuration;
        private readonly DataBaseContext _context;
        public TokenDAL(IConfiguration configuration, DataBaseContext context) 
        {
            this._configuration = configuration;
            this._context = context;
        }
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameter = new TokenValidationParameters()
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:secret"])),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameter, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken != null || jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid Token");
            return principal;
        }

        public string GetRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public TokenResponce GetToken(IEnumerable<Claim> claim)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddDays(7),
                claims: claim,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResponce { TokenString = tokenString, ValidTo = token.ValidTo };
        }

        public RefreshTokenRequest RefreshToken(RefreshTokenRequest tokenApiModel)
        {
            var principal = GetPrincipalFromExpiredToken(tokenApiModel.AccessToken);
            var userName = principal.Identity.Name;
            var user = _context.tokenInfos.SingleOrDefault(u => u.UserName == userName);
            if (user == null || user.RefreshToken != tokenApiModel.RefreshToken || user.RefreshTokenExpiry <= DateTime.Now)
            {
                return null;
            }
            var newAccessToken = GetToken(principal.Claims);
            var newRefreshToken = GetRefreshToken();
            user.RefreshToken = newRefreshToken;
            _context.SaveChanges();
            return new RefreshTokenRequest
            {
                AccessToken = newAccessToken.TokenString,
                RefreshToken = newRefreshToken
            };
        }

        public bool RevokeToken(string userName)
        {
            var user = _context.tokenInfos.SingleOrDefault(u => u.UserName == userName);
            if (user == null)
            {
                return false;
            }
            user.RefreshToken = null;
            _context.SaveChanges();
            return true;
        }
    }
}
