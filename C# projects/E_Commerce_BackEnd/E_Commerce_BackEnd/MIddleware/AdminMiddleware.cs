using System.Text;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;

namespace E_Commerce_BackEnd.MIddleware
{
    public class AdminMiddleware : IMiddleware
{
    private readonly ILogger<AdminMiddleware> _logger;

    public AdminMiddleware(ILogger<AdminMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            _logger.LogInformation("Executing admin middleware");

            var path = context.Request.Path;

            if (path.StartsWithSegments("/api/admin"))
            {
                _logger.LogInformation("PATH IN ADMIN " + path);

                if (await IsValidAdminRequest(context, path))
                {
                    _logger.LogInformation("AUTHORIZED ADMIN");
                   

                    await next(context);
                }
                else
                {
                    _logger.LogInformation("Unauthorized access attempt in admin path: " + path);

                    // Set the response code and short-circuit the pipeline
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    // Write response and end request processing
                    await context.Response.WriteAsync(GetUnauthorizedMessage(context));
                  
                }
            }
            else
            {
                _logger.LogInformation("Not an admin path");
                await next(context);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error in the admin middleware: " + e.Message);
            throw; // Rethrow the exception after logging
        }
    }

    private async Task<bool> IsValidAdminRequest(HttpContext context, PathString path)
    {
            
        var isLoggedIn = context.Request.Cookies.TryGetValue("userLoggedIn", out var isLoggedInString) && isLoggedInString == "1";
        var isAdmin = context.Request.Cookies.TryGetValue("admin", out var adminString) && adminString == "1";
        var isAdminLoggedIn = context.Request.Cookies.TryGetValue("adminLoggedIn", out var adminLoggedInString) && adminLoggedInString == "1";

        if (!isLoggedIn)
        {
            _logger.LogWarning("User is not logged in (middleware)");
            return false;
        }

        if (path.StartsWithSegments("/api/admin/login"))
        {
            if (isAdmin)
            {
                return true;
            }
            _logger.LogWarning($"Non-admin tried to access -> {path}");
            return false;
        }
        
        var getHeaderSecret = await TokenService.GetSecret("prod/texx.ro/admin-header");
        var decodedString = "";
        if (context.Request.Cookies.TryGetValue("ASP_NET_ADMIN_SESSION", out var encodedString))
        {
            decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
        }

        if (decodedString == "" || decodedString != getHeaderSecret)
        {
            return false;
        }

        if (isAdmin && isAdminLoggedIn)
        {
            return true;
        }

        _logger.LogWarning($"Non-admin tried to access/ Or invalid validation for the adminLoggedIn cookie -> {path}");
        return false;
    }

    private static string GetUnauthorizedMessage(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey("userLoggedIn"))
        {
            return "User is not logged in";
        }
        if (!context.Request.Cookies.ContainsKey("admin"))
        {
            return "Admin cookie not present";
        }
        return "You are not allowed here!";
    }
}

}
