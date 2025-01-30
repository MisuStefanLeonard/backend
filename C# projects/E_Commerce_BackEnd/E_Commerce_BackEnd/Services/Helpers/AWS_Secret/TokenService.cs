using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Extensions.Caching;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Console = System.Console;

namespace E_Commerce_BackEnd.Services.Helpers.AWS_Secret;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TokenService> _logger;
    private static SecretsManagerCache? _cache;
    

    public TokenService( IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
            new ("username" , currentLogIn.Username!),
            new (Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Email , currentLogIn.Email!),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (ClaimTypes.Role , currentLogIn.Rol),
            new ("user_id" , currentLogIn.IdCont.ToString())
        };
        
        var token = new JwtSecurityToken
        (
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
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
        var secret = await GetSecret("prod/texx.ro/JWT_key"); 
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
    
    public async Task<Tuple<ClaimsPrincipal? , Conturi?>> TokenValidation(string token , string refreshToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            if (token == "refresh")
            {
                throw new SecurityTokenExpiredException("Expired token . Issuing a new one.");
            }
            
            var validationParameters = await GetValidationParameters();

            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters,
                out var validatedToken);

            return new Tuple<ClaimsPrincipal?, Conturi?>(claimsPrincipal , null); // succesfull
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogInformation("Issuing new acces token..");
            var checkRefreshToken = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(rm => rm.SessionToken == refreshToken)
                .FirstOrDefaultAsync();

            if (checkRefreshToken == null || checkRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogError("Refresh token expired. Session expired");
                return new Tuple<ClaimsPrincipal?, Conturi?>(null , null);
            }

            var userId = checkRefreshToken.IdCont;
            var findUser = await _unitOfWork.Repository<Conturi>()
                .FindQueryable(c => c.IdCont == userId)
                .FirstOrDefaultAsync();

            if (findUser == null)
            {
                _logger.LogError("User not found in db!");
                return new Tuple<ClaimsPrincipal?, Conturi?>(null , null);
            }
            var validateAgain = tokenHandler.ValidateToken(await GenerateJwtAccesToken(findUser),  await GetValidationParameters(),
                out  _);
            return new Tuple<ClaimsPrincipal?, Conturi?>(validateAgain, findUser); // refresh 
        }
        catch (Exception e)
        {
            Console.WriteLine("Error when validating the token -> Error: " + e.Message);
            return new Tuple<ClaimsPrincipal?, Conturi?>(null, null); // general error  
        }
    }
}
