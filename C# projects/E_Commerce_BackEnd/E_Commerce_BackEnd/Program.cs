using System.IO.Compression;
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
using E_Commerce_BackEnd.Services.uGeneralService;
using E_Commerce_BackEnd.Services.uInelePrindereService;
using E_Commerce_BackEnd.Services.uManopereService;
using E_Commerce_BackEnd.Services.uMJMLService;
using E_Commerce_BackEnd.Services.uOrdersService;
using E_Commerce_BackEnd.Services.uPopUpService;
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
using Microsoft.AspNetCore.ResponseCompression;
using Quartz;
using Sqids;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] {
            "application/json",
            "application/javascript",
            "application/xml",
            "text/css",
            "application/octet-stream",
            "image/svg+xml",
            "application/x-font-ttf",
            "application/vnd.ms-fontobject",
            "font/woff",
            "font/woff2"
        });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal; // Choose Fastest or Optimal
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Choose Fastest or Optimal
});


builder.Services.AddHealthChecks();
// Configure AWS options
// FOR DOCKER AWS CREDENTIALS
// testing migration
// ------
Environment.SetEnvironmentVariable("DOCKER" , "true");
var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
var awsCredentials = new string[3];
var awsAccessKey = "";
var awsSecretKey = "";
var awsRegion = "";
if (getDockerEnv == "true")
{
    awsCredentials = await File.ReadAllLinesAsync("/run/secrets/aws_secrets");
    awsAccessKey =  awsCredentials.Length > 0 ? awsCredentials[0].Trim() : "";
    awsSecretKey =  awsCredentials.Length > 1 ? awsCredentials[1].Trim() : "";
    awsRegion = awsCredentials.Length > 2 ? awsCredentials[2].Trim() : "eu-central-1";
    Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID" , awsAccessKey);
    Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY" , awsSecretKey);
    Environment.SetEnvironmentVariable("AWS_REGION" , awsRegion);
}
else
{
    Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID" , "AKIAVRUVWGMELKZCKMLQ");
    Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY" , "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");
    Environment.SetEnvironmentVariable("AWS_REGION" , "eu-central-1");
}

Environment.SetEnvironmentVariable("AWS_SECURITY_TOKEN" , "");
Environment.SetEnvironmentVariable("AWS_EC2_METADATA_DISABLED" , "true");

AWSOptions awsOptions;
if (getDockerEnv == "true")
{
    var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);
    var basicAwsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
    awsOptions = new AWSOptions
    {
        Credentials = basicAwsCredentials,
        Region = regionEndpoint
    };
}
else
{
    var region = builder.Configuration.GetAWSOptions().Region;
    awsOptions  = new AWSOptions
    {
        Profile = "misu_stefan",
        ProfilesLocation = "/Users/misustefan/.aws/credentials",
        Region = region
    };
}


builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddSingleton<IAmazonSecretsManager>
    (sp => new AmazonSecretsManagerClient(awsOptions.Credentials, awsOptions.Region));

builder.Services.AddSingleton<IAmazonKeyManagementService>
    (sp => new AmazonKeyManagementServiceClient(awsOptions.Credentials, awsOptions.Region));

// user-secrets
builder.Configuration.AddUserSecrets<Program>();


// Add data protection using AWS Systems Manager Parameter Store
builder.Services.AddDataProtection()
    .PersistKeysToAWSSystemsManager("prod/texx.ro/JWT_key")
    .PersistKeysToAWSSystemsManager("prod/texx.ro/admin")
    .PersistKeysToAWSSystemsManager("prod/texx.ro/admin-header");


// Configure DbContext and logger

var connectionString = getDockerEnv == "false" ? builder.Configuration.GetConnectionString("CMDatabase") 
    : "Server=dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com;Database=ComertDatabase;User=admin;Password=Stefan30122003!;";


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
// builder.Services.AddScoped<ColorCodesResolver>();



// CORS configuration
var corsPolicy = getDockerEnv == "true" ? "http://46.101.141.122:3000" : "http://localhost:3000";


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy =>
        {
            policy.WithOrigins(corsPolicy)
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
// builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ITipuriGalerieService, TipuriGalerieService>();
builder.Services.AddScoped<ITipuriLinieService, TipuriLinieService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPopUpService, PopUpService>();
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

var googleCredentials = Array.Empty<string>();
var googleAuthId = "";
var googleAuthKey = "";
if (getDockerEnv! == "true")
{
    googleCredentials = await File.ReadAllLinesAsync("/run/secrets/google_auth");
    googleAuthId = googleCredentials[0];
    googleAuthKey = googleCredentials[1];
}
else
{
    var googleAuth = builder.Configuration.GetSection("GoogleAuth");
    googleAuthId = googleAuth["ClientId"]!;
    googleAuthKey = googleAuth["ClientSecret"]!;
}
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
        options.ClientId = googleAuthId;
        options.ClientSecret = googleAuthKey;
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
app.MapHealthChecks("/healthz").AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHsts();
}

app.UseRateLimiter();
app.UseMiddleware<CartMiddleware>();
app.UseMiddleware<JwtTokenMiddleware>();
app.UseMiddleware<AdminMiddleware>();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
