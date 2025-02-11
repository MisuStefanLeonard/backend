using System.Collections.Specialized;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using E_Commerce_BackEnd.MIddleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using E_Commerce_BackEnd.Models.Context.ContextInjection;
using E_Commerce_BackEnd.Services.Helpers.DtoMapper;
using E_Commerce_BackEnd.Services.emailService;
using E_Commerce_BackEnd.UnitOfWork;
using E_Commerce_BackEnd.Models.Context;
using E_Commerce_BackEnd.QuartzJobs;
using E_Commerce_BackEnd.Services.Helpers.adminHelpers;
using E_Commerce_BackEnd.Services.uService;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.Resolvers;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.Services.uAdminService;
using E_Commerce_BackEnd.Services.uAdressService;
using E_Commerce_BackEnd.Services.uBucketService;
using E_Commerce_BackEnd.Services.uGeneralService;
using E_Commerce_BackEnd.Services.uInelePrindereService;
using E_Commerce_BackEnd.Services.uManopereService;
using E_Commerce_BackEnd.Services.uMJMLService;
using E_Commerce_BackEnd.Services.uOrdersService;
using E_Commerce_BackEnd.Services.uProductsService;
using E_Commerce_BackEnd.Services.uReviewService;
using E_Commerce_BackEnd.Services.uSeturiService;
using E_Commerce_BackEnd.Services.uShoppingCartService;
using E_Commerce_BackEnd.Services.uTipuriGalerieService;
using E_Commerce_BackEnd.Services.uTipuriLinieService;
using E_Commerce_BackEnd.Services.uVoucherService;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;
using Quartz;
using Quartz.Impl;
using Sqids;
var builder = WebApplication.CreateBuilder(args);

// Configure AWS options

var awsCredentials = File.ReadAllLines("/run/secrets/aws_secrets");
var awsAccessKey = awsCredentials.Length > 0 ? awsCredentials[0].Trim() : "";
var awsSecretKey = awsCredentials.Length > 1 ? awsCredentials[1].Trim() : "";
var awsRegion = awsCredentials.Length > 2 ? awsCredentials[2].Trim() : "eu-central-1"; // Default region if not provided

Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID" , awsAccessKey);
Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY" , awsSecretKey);
Environment.SetEnvironmentVariable("AWS_REGION" , awsRegion);
Environment.SetEnvironmentVariable("AWS_SECURITY_TOKEN" , "");



Console.WriteLine($"✅ AWS_ACCESS_KEY_ID: {awsAccessKey}");
Console.WriteLine($"✅ AWS_SECRET_ACCESS_KEY: {awsSecretKey}"); // Mask secret for security
Console.WriteLine($"✅ AWS_REGION: {awsRegion}");




var basicAwsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
var regionEnpoint = RegionEndpoint.GetBySystemName(awsRegion);

var awsOptions = new AWSOptions
{
   Credentials = basicAwsCredentials,
   Region = regionEnpoint
};

// var region = builder.Configuration.GetAWSOptions();
// var awsOptions = new AWSOptions
// {
//     Profile = "misu_stefan",
//     ProfilesLocation = "/Users/misustefan/.aws/credentials",
//     Region = region.Region
// };
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddSingleton<IAmazonSecretsManager>
    (sp => new AmazonSecretsManagerClient(awsOptions.Credentials, awsOptions.Region));

builder.Services.AddSingleton<IAmazonKeyManagementService>
    (sp => new AmazonKeyManagementServiceClient(awsOptions.Credentials, awsOptions.Region));
// user-secrets

var credentials = FallbackCredentialsFactory.GetCredentials();
Console.WriteLine($"-----Using AWS Credentials: {credentials.GetCredentials().AccountId}");
builder.Configuration.AddUserSecrets<Program>();
// Quartz integration for task scheduling


// Add data protection using AWS Systems Manager Parameter Store
builder.Services.AddDataProtection()
    .PersistKeysToAWSSystemsManager("prod/texx.ro/JWT_key")
    .PersistKeysToAWSSystemsManager("prod/texx.ro/admin");

// Configure DbContext and logger
var connectionString = builder.Configuration.GetConnectionString("CMDatabase");
if (connectionString == null)
{
    throw new InvalidOperationException("Connection string CMDatabase is null");
}
var loggerFactory = DbContextInjection.MyLoggerFactory;
builder.Services.M_DbContextInjection<ECommerceContext>(connectionString, loggerFactory);
// Background cleanup jobs using Quartz.net
builder.Services.AddQuartz(q =>
{
    q.UsePersistentStore(opt =>
    {
        opt.UseProperties = true;
        opt.UseMySql(connectionString);
        opt.UseSystemTextJsonSerializer();
        opt.PerformSchemaValidation = true;
    });

    var cartCleanUpJobKey = JobKey.Create("cart-clean-up-job", "cart");
    var sessionTokenCleanUpJobKey = JobKey.Create("session-token-clean-up-job", "session-tokens");
    var unlockingProductsKey = JobKey.Create("unlocking-locked-products" , "products-job");

    q.AddJob<CartCleanUp>(cartCleanUpJobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(cartCleanUpJobKey)
                .WithIdentity("cart-clean-up-trigger", "cart-trigger")
                .WithCronSchedule("0 0 */12 ? * *")
                .StartNow();
                //  At 00:00:00am, every 3 days starting on Monday, every month 
                
        });
    
    q.AddJob<SessionTokenCleanUp>(sessionTokenCleanUpJobKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(sessionTokenCleanUpJobKey)
                .WithIdentity("session-token-clean-up-trigger", "session-token-trigger")
                .WithCronSchedule("0 0 */12 ? * *")
                .StartNow();
            //  Every 12 hours
                
        });
    q.AddJob<UnlockProductsInCaseOfError>(unlockingProductsKey)
        .AddTrigger(trigger =>
        {
            trigger.ForJob(unlockingProductsKey)
                .WithIdentity("unlocking-products-trig", "products-trigs")
                .WithCronSchedule("0 4 0 * * ?")
                .StartNow();
            //  Every 12 hours
                
        });
  
});

builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

// Mapper configuration
builder.Services.AddAutoMapper((serviceProvider, cfg) =>
{
    // Add your mapping profiles here
    cfg.AddProfile<MappersProfile>();

    // Use the DI container to resolve services
    cfg.ConstructServicesUsing(serviceProvider.GetService);
},typeof(Program));
// Resolver for mapper
builder.Services.AddScoped<ProducatorValueResolver>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Add AWS services

// Unit of work and repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Service layer
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IMjmlService, MjmlService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAdressService, AdressService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISeturiService, SeturiService>();
builder.Services.AddScoped<IGeneralSettingsService, GeneralSettingsService>();
builder.Services.AddScoped<IInelePrindereService, InelePrindereService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IManopereService, ManopereService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IBucketAcces, BucketAccess>();
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ITipuriGalerieService, TipuriGalerieService>();
builder.Services.AddScoped<ITipuriLinieService, TipuriLinieService>();
builder.Services.AddTransient<JwtTokenMiddleware>();
builder.Services.AddTransient<AdminMiddleware>();
builder.Services.AddTransient<CartMiddleware>();
builder.Services.AddScoped<DocumentProcessing>();
builder.Services.AddSingleton<UserHelpers>();
builder.Services.AddSingleton<SqidsEncoder<int>>();

builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(x =>
    x.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4; // 4 requests
        options.Window = TimeSpan.FromSeconds(30);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = await TokenService.GetSecret("prod/texx.ro/JWT_key");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Prevent CSRF
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/signin-google";
    options.LogoutPath = "/logout";
})
    .AddGoogle(options =>
{
    var googleAuth = builder.Configuration.GetSection("GoogleAuth");
    options.ClientId = googleAuth["ClientId"]!;
    options.ClientSecret = googleAuth["ClientSecret"]!;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddJwtBearer(x =>
{
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["JWTToken"];
            return Task.CompletedTask;
        }
    };

    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policy
builder.Services.AddAuthorization();


// Controllers and Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // for enums // to send the string representation to the backend 
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
       
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowVueApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseMiddleware<CartMiddleware>();
app.UseMiddleware<JwtTokenMiddleware>();
app.UseMiddleware<AdminMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
