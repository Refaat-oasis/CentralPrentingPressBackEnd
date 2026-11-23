using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using CentralPress.API.Common;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

bool isProdLike = !builder.Environment.IsDevelopment();
bool requireHttps = builder.Configuration.GetValue<bool>("Security:RequireHttps", isProdLike);

// 1. Database Connection
if (builder.Configuration["UseInMemoryDatabase"] == "true")
{
    if (isProdLike)
    {
        throw new InvalidOperationException("UseInMemoryDatabase=true is strictly prohibited outside Development.");
    }
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    //if (string.IsNullOrWhiteSpace(connectionString))
    //{
    //    if (isProdLike)
    //    {
    //        throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured in Production.");
    //    }
    //    connectionString = "Server=localhost;Database=AssiutCentralPressDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
    //}

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(60);
        });
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
}

// 2. Identity Configuration with Enterprise Password Complexity & Lockout
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:SecurityKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32 ||
    (isProdLike && (jwtKey.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) || jwtKey.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase))))
{
    throw new InvalidOperationException("JwtSettings:SecurityKey is not configured securely (minimum 32 non-placeholder characters required).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = requireHttps;
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "CentralPressAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "CentralPressClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// 4. Policy-Based Authorization (HIGH-06, LOW-01)
builder.Services.AddAuthorization(options =>
{
    string[] modules = { "employees", "job_titles", "machinery", "materials", "customers", "moqaysa", "formulas", "tasweya", "payment_orders", "reports" };
    string[] actions = { "access", "create", "update", "toggleactive" };

    foreach (var mod in modules)
    {
        foreach (var act in actions)
        {
            var normalizedClaim = $"{mod}:{act}";
            var legacyPolicyName = $"{mod.ToUpperInvariant()}:Can{char.ToUpper(act[0])}{act[1..]}";

            options.AddPolicy(legacyPolicyName, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Developer") ||
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(c => c.Type == PermissionConstants.ClaimType &&
                        (c.Value.Equals(normalizedClaim, StringComparison.OrdinalIgnoreCase) ||
                         c.Value.Equals(legacyPolicyName, StringComparison.OrdinalIgnoreCase)))));

            options.AddPolicy(normalizedClaim, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Developer") ||
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(c => c.Type == PermissionConstants.ClaimType &&
                        (c.Value.Equals(normalizedClaim, StringComparison.OrdinalIgnoreCase) ||
                         c.Value.Equals(legacyPolicyName, StringComparison.OrdinalIgnoreCase)))));
        }
    }
});

// 5. Register MemoryCache, Hangfire & Application Services
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IJobTitleService, JobTitleService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<MoqaysaCalculationService>();
builder.Services.AddScoped<IPaperCuttingCalculationService, PaperCuttingCalculationService>();
builder.Services.AddScoped<IMoqaysaService, MoqaysaService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IMachineryService, MachineryService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IFormulaConfigService, FormulaConfigService>();
builder.Services.AddScoped<WordExportService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailRetryJob, EmailRetryJob>();
builder.Services.AddSingleton<ITafqeetService, TafqeetService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ITasweyaService, TasweyaService>();
builder.Services.AddScoped<IPaymentOrderService, PaymentOrderService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

// Hangfire Background Jobs (Email Auto-Retry + Developer Monitoring)
if (builder.Configuration["UseInMemoryDatabase"] == "true")
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage());
}
else
{
    var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConn, new SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
}
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "emails", "default" };
    options.WorkerCount = 2;
});

// Documents Printing Engine (PLAN.md - Single Source of Truth)
builder.Services.Configure<CentralPress.API.Documents.DocumentOptions>(builder.Configuration.GetSection(CentralPress.API.Documents.DocumentOptions.Section));
builder.Services.AddSingleton<CentralPress.API.Documents.DocxTemplateFiller>();
builder.Services.AddSingleton<CentralPress.API.Documents.PdfConverter>();
builder.Services.AddHostedService<CentralPress.API.Documents.LibreOfficeWarmService>();
builder.Services.AddScoped<CentralPress.API.Documents.IAmountInWords, CentralPress.API.Documents.TafqeetAmountInWords>();
builder.Services.AddScoped<CentralPress.API.Documents.IPrintDataSource, CentralPress.API.Documents.EfPrintDataSource>();
builder.Services.AddScoped<CentralPress.API.Documents.DocumentService>();

// 6. MVC & Sanitized Audit Logging Filter (CRIT-03)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditBacklogActionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// 7. Strict CORS Policy, Rate Limiting, ForwardedHeaders & HealthChecks
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Select(o => o.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? Array.Empty<string>();

if (isProdLike && allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must specify at least one allowed origin in Production.");
}

if (allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:5173", "https://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOriginsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition", "X-Document-Template");
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "تم تجاوز الحد المسموح من المحاولات. يرجى الانتظار دقيقة ثم إعادة المحاولة."
        }, ct);
    };

    options.AddPolicy("AuthRateLimit", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

// 8. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assiut University Central Printing Press API",
        Version = "v1",
        Description = "مطبعة جامعة أسيوط - النظام المحاسبي والإداري (الإصدار الثاني)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-seed Database on Startup (CRIT-02, HIGH-06, HIGH-25)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.InitializeAsync(db, userMgr, roleMgr, builder.Configuration, app.Environment, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[!] خطأ أثناء تهيئة قاعدة البيانات: {Message}", ex.Message);
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        if (feature?.Error != null)
        {
            logger.LogError(feature.Error, "Unhandled server exception for {Path} (TraceId: {TraceId})",
                context.Request.Path, context.TraceIdentifier);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "حدث خطأ داخلي في الخادم. يرجى المحاولة مجدداً أو إبلاغ الدعم الفني.",
            traceId = context.TraceIdentifier
        });
    });
});

if (!app.Environment.IsDevelopment() && requireHttps)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    if (context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache";
        context.Response.Headers["Pragma"] = "no-cache";
    }

    await next();
});

app.UseCors("AllowedOriginsPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Central Press API v1");
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CentralPress.API.Middleware.UserSessionValidationMiddleware>();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new DeveloperHangfireAuthorizationFilter() }
});

app.MapHealthChecks("/health");
app.MapHealthChecks("/api/health");
app.MapControllers();

app.Run();

// Export Program type for WebApplicationFactory in integration tests
public partial class Program { }
