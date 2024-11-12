using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace E_Commerce_BackEnd.MIddleware;

public class JwtTokenMiddlewareFactory : IMiddleware
{
    private readonly ILogger<JwtTokenMiddlewareFactory> _logger;
    private readonly ITokenService _tokenService;

    public JwtTokenMiddlewareFactory(ILogger<JwtTokenMiddlewareFactory> logger, ITokenService tokenService)
    {
        _logger = logger;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            var endpoint = context.GetEndpoint();
            _logger.LogInformation("EXECUTING JWT TOKEN MIDDLEWARE");
            // Check if the request path is for Google authentication and bypass JWT validation
            var path = context.Request.Path;
            _logger.LogInformation($"PATH IN JWT TOKEN MIDDLEWARE -> {path}");

            if (path.StartsWithSegments("/signin-google") || 
                path.StartsWithSegments("/google-response"))
            {
                await next(context);
                return;
            }

            

            // Check if endpoint allows anonymous access
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                _logger.LogInformation("AM AJUNS IN ANONYMOUS");
                await next(context);
                return;
            }
            
            Console.WriteLine("INAINTE DE TRAGEREA TOKENULUI");
            
            if (context.Request.Cookies.TryGetValue("JWTToken", out var token) &&
                context.Request.Cookies.TryGetValue("userLoggedIn", out var isLoggedIn))
            {
                Console.WriteLine("Dupa");

                
                
                var principalUser = await _tokenService.TokenValidation(token);
                

                if (principalUser != null && isLoggedIn == "1")
                {
                    context.User = principalUser;
                    _logger.LogInformation("User successfully set.");
                   
                }
                else
                {
                    _logger.LogWarning("Invalid JWT Token");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid JWT Token");
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Missing JWT Token");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing JWT Token");
                return;
            }

            await next(context);
        }
        catch (AuthenticationFailureException ex)
        {
            _logger.LogError(ex, "Authentication failure: Access was denied by the resource owner or by the remote server.");
            context.Response.Redirect("http://localhost:3000/home");
        }
    }
}
