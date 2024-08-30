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

                    if (IsValidAdminRequest(context, path))
                    {
                        await next(context);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync(GetUnauthorizedMessage(context, path));
                    }
                }
                else
                {
                    await next(context);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error in the admin middleware");
                _logger.LogInformation($"Error -> {e.Message}");
                throw;
            }
        }

        private bool IsValidAdminRequest(HttpContext context, PathString path)
        {
            bool isLoggedIn = context.Request.Cookies.TryGetValue("userLoggedIn", out var isLoggedInString) && isLoggedInString == "1";
            bool isAdmin = context.Request.Cookies.TryGetValue("admin", out var adminString) && adminString == "1";
            bool isAdminLoggedIn = context.Request.Cookies.TryGetValue("adminLoggedIn", out var adminLoggedInString) && adminLoggedInString == "1";

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

            if (isAdmin && isAdminLoggedIn)
            {
                return true;
            }

            _logger.LogWarning($"Non-admin tried to access/ Or invalid validation for the adminLoggedIn cookie -> {path}");
            return false;
        }

        private string GetUnauthorizedMessage(HttpContext context, PathString path)
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
