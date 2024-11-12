using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Extensions.Caching;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using Microsoft.IdentityModel.Tokens;
using Console = System.Console;

namespace E_Commerce_BackEnd.Services.Helpers.AWS_Secret;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private static SecretsManagerCache? _cache;

    public TokenService( IConfiguration configuration)
    {
       
        _configuration = configuration;
       
    }

    private static void InitalizeCache()
    {
        var client = new AmazonSecretsManagerClient(RegionEndpoint.EUCentral1);
        var cacheConfiguration = new SecretCacheConfiguration
        {
            Client = client,
            CacheItemTTL = 86400000
        };
        _cache = new SecretsManagerCache(client, cacheConfiguration);
    }
    
    /// <summary>
    /// Retrieves the secret value from AWS Secrets Manager with caching
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static async Task<string> GetSecret(string secretName)
    {
        try
        {
            
            if (_cache == null)
            {
                
                InitalizeCache();
            }
            
            // Retrieve the secret value from cache
            // 
           
            var secret = await _cache!.GetSecretString(secretName);
            var keyValuePairJson = JsonDocument.Parse(secret);
            var rootElement = keyValuePairJson.RootElement;

            Dictionary<string, string> secretDicts = [];

            foreach (var jsonPair in rootElement.EnumerateObject())
            {
                secretDicts[jsonPair.Name] = jsonPair.Value.GetString()!;

            }
            
            var keyNameArr = secretName.Split("/");
            var keyName = keyNameArr[^1];
          
            return secretDicts[keyName];
        }
        catch (Exception e)
        {
            // Handle any exceptions that occur when retrieving the secret
            throw new Exception($"Error retrieving secret from AWS Secrets Manager: {e.Message} , with source ${e.Source}", e);
        }
    }
    
    public async Task<string> GenerateJwtAccesToken(Conturi currentLogIn)
    {
       
        var secretValue = await GetSecret("prod/texx.ro/JWT_key"); 
       

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new ArgumentNullException("secret value not correct");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretValue));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var claims = new List<Claim>
        {
            new Claim("username" , currentLogIn.Username!),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Email , currentLogIn.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role , currentLogIn.Rol),
            new Claim("user_id" , currentLogIn.IdCont.ToString())
        };
        
        var token = new JwtSecurityToken
        (
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string RefreshToken()
    {
        var secureRandomBytes = new byte[128];
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(secureRandomBytes);
        var refreshToken = Convert.ToBase64String(secureRandomBytes);
        return refreshToken;
    }

    private async Task<TokenValidationParameters> GetValidationParameters()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = await GetSecret("prod/texx.ro/JWT_key"); // Note: Using .Result for simplicity, but consider using async/await appropriately
        var key = Encoding.UTF8.GetBytes(secret);
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    }
    
    public async Task<ClaimsPrincipal?> TokenValidation(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var validationParameters = await GetValidationParameters();
            
            var claimsPrincipal =  tokenHandler.ValidateToken(token, validationParameters, 
                out var validatedToken);
            return claimsPrincipal;
        }
        catch (SecurityTokenValidationException e)
        {
            Console.WriteLine("Error when validating the token -> Error: " + e.Message);
            return null;
        }
    }
}
