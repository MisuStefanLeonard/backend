namespace E_Commerce_BackEnd.MIddleware;

public class CartMiddleware : IMiddleware
{
    private readonly ILogger<CartMiddleware> _logger;

    public CartMiddleware(ILogger<CartMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _logger.LogInformation("Processing request for {Path}", context.Request.Path);
        
        if (!context.Request.Cookies.ContainsKey("ASP.NET_COOKIE_cartSession"))
        {
            var sessionId = Guid.NewGuid() + $"|{DateTime.UtcNow}";
            context.Response.Cookies.Append($"ASP.NET_COOKIE_cartSession", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(3)
            });
           
            _logger.LogInformation("CART SESSION COOKIE SET");
        }
       
        await next(context);
       

    }
}