using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Services.Helpers.AWS_Secret;

public interface ITokenService
{
    Task<string> GenerateJwtAccesToken(Conturi currentLogIn);
    string RefreshToken();
    Task<ClaimsPrincipal? >TokenValidation(string token);
}