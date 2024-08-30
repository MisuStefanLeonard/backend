using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_BackEnd.Models.Context.ContextInjection
{
    public static class DbContextInjection
    {
        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddJsonConsole(opt =>
            {
                opt.IncludeScopes = true;
                opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                opt.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
                {
                    Indented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };
                
            });
        
        });
        
        public static void M_DbContextInjection<TContext>(this IServiceCollection serviceCollection, string connectionString
            , ILoggerFactory loggerFactory) where TContext : ECommerceContext
        {
            serviceCollection.AddDbContext<TContext>(options =>
            {
                ConfigureDbContext(options, connectionString, loggerFactory);
            });
        }

        private static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString, ILoggerFactory loggerFactory)
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .UseLoggerFactory(loggerFactory);
        }
        
       
    }
}