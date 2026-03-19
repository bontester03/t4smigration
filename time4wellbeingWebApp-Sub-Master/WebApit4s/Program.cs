using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApit4s.DAL;
using WebApit4s.Identity;
using WebApit4s.Services;
using WebApit4s.Services.GuestRegistration;
using WebApit4s.Services.Interfaces;
using WebApit4s.Services.Options;
using WebApit4s.Services.Registration;
using WebAPIts.Services.Interfaces;





var builder = WebApplication.CreateBuilder(args);
var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(configuredUrls))
{
    builder.WebHost.UseUrls("http://localhost:5206", "http://0.0.0.0:5206");
}
else
{
    builder.WebHost.UseUrls(
        configuredUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));



// Bind strongly-typed options (Development overrides will apply automatically)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Needed by TokenService to read IP/UserAgent when not supplied
builder.Services.AddHttpContextAccessor();

// Read Jwt values for bearer validation
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing.");
var jwtAudience = jwtSection["Audience"]; // optional

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            NameClaimType = "sub",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        if (!string.IsNullOrWhiteSpace(jwtAudience))
        {
            tokenValidationParameters.ValidAudience = jwtAudience;
        }

        options.TokenValidationParameters = tokenValidationParameters;

        // ? ADD EVENT LOGGING
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"?? JWT Auth FAILED: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"?? JWT Token VALIDATED");
                var claims = context.Principal?.Claims;
                if (claims != null)
                {
                    foreach (var claim in claims)
                    {
                        Console.WriteLine($"   Claim: {claim.Type} = {claim.Value}");
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// Our services
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IHealthScoreService, HealthScoreService>();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();
builder.Services.AddScoped<IJwtFactory, JwtFactory>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        // Tell ASP.NET Core to also scan the API namespace for controllers
        manager.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(typeof(Program).Assembly));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

//swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebApit4s API",
        Version = "v1",
        Description = "Time4Sport Web API for Health, Measurement, Profile, and Dashboard"
    });

    // ? Include only [ApiController] controllers
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.ActionDescriptor is ControllerActionDescriptor cad)
        {
            return cad.ControllerTypeInfo.GetCustomAttributes(typeof(ApiControllerAttribute), true).Any();
        }
        return false;
    });

    // ? FIX for duplicate class name conflicts (e.g., HealthScoreDto appears twice)
    // This makes Swagger use the *full namespace* as schema ID instead of just the class name.
    c.CustomSchemaIds(type => type.FullName);

    // (optional but good practice)
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// Register Identity services with custom ApplicationUser and ApplicationRole
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<TimeContext>()
    .AddDefaultTokenProviders();


// Add services to the container.
builder.Services.AddControllersWithViews();
//Hostservices
builder.Services.AddHostedService<RegistrationReminderService>();


// Optional: Register MailKit SMTP client if injecting
builder.Services.AddTransient<SmtpClient>();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<TimeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<NotificationService>();

builder.Services.AddScoped<GenAiGoalService>();
builder.Services.AddScoped<IGuestRegistrationService, GuestRegistrationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

builder.Services.AddHostedService<RecurringTaskService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

// KPI Export Service
builder.Services.AddScoped<KpiExportService>();

// ? Register CORS policy before Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Add session services with a timeout of 30 minutes
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

//ChildContextService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChildContextService, ChildContextService>();
builder.Services.AddSession();

builder.Services.AddControllers(); // Current line
    builder.Services.AddControllers()
    .AddApplicationPart(typeof(WebApit4s.API.ApiDashboardController).Assembly);

// ? Build comes after all service registrations
var app = builder.Build();

app.Use(async (context, next) =>
{
    await next();

    // If API request returns 401, don't redirect to login page
    if (context.Response.StatusCode == 401 &&
        context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.Remove("Location");
    }
});
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // ? Now safe to use

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // for API attributes
// Enable session

app.MapGet("/guest/register", (HttpContext context) =>
{
    var code = context.Request.Query["code"].ToString();
    var target = string.IsNullOrWhiteSpace(code)
        ? "/guest-registration/invalid"
        : $"/guest-registration/{Uri.EscapeDataString(code)}/step-1";

    return Results.Redirect(target, permanent: false);
});

app.MapGet("/guest/register/{**path}", (HttpContext context) =>
{
    var code = context.Request.Query["code"].ToString();
    var target = string.IsNullOrWhiteSpace(code)
        ? "/guest-registration/invalid"
        : $"/guest-registration/{Uri.EscapeDataString(code)}/step-1";

    return Results.Redirect(target, permanent: false);
});

app.MapGet("/guest-registration/register/{**path}", (HttpContext context) =>
{
    var remainder = context.Request.RouteValues["path"]?.ToString();
    var target = string.IsNullOrWhiteSpace(remainder)
        ? "/register/step-1"
        : $"/register/{remainder}";

    return Results.Redirect(target, permanent: false);
});


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapFallbackToFile("/register/{*path:nonfile}", "guest-registration/index.html");
app.MapFallbackToFile("/guest-registration/{*path:nonfile}", "guest-registration/index.html");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var context = services.GetRequiredService<TimeContext>();
        var provider = context.Database.ProviderName ?? string.Empty;

        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            context.Database.EnsureCreated();
        }
        else
        {
            context.Database.Migrate();
        }

        await IdentitySeeder.SeedAdminAsync(services);
    }
    catch (Exception ex)
    {
        // Keep the web app running even when DB is unavailable in local/sandbox environments.
        logger.LogWarning(ex, "Database migration/seed skipped during startup.");
    }
   
}


app.Run();



