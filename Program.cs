using System;
using System.Text;
using System.Threading.Tasks;
using CentralPress.API.Common;
using CentralPress.API.Data;
using CentralPress.API.Entities;
using CentralPress.API.Filters;
using CentralPress.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Connection
if (builder.Configuration["UseInMemoryDatabase"] != "true")
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Database=AssiutCentralPressDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(60);
        }));
}

// 2. Identity Configuration with Enterprise Password Complexity (MED-04)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Authentication (CRIT-01, HIGH-01)
var jwtKey = builder.Configuration["JwtSettings:SecurityKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("JwtSettings:SecurityKey is not configured or is shorter than 32 characters.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "CentralPressAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "CentralPressClient",
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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
                    ctx.User.IsInRole("SuperAdmin") ||
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(c => c.Type == PermissionConstants.ClaimType &&
                        (c.Value.Equals(normalizedClaim, StringComparison.OrdinalIgnoreCase) ||
                         c.Value.Equals(legacyPolicyName, StringComparison.OrdinalIgnoreCase)))));

            options.AddPolicy(normalizedClaim, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("SuperAdmin") ||
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(c => c.Type == PermissionConstants.ClaimType &&
                        (c.Value.Equals(normalizedClaim, StringComparison.OrdinalIgnoreCase) ||
                         c.Value.Equals(legacyPolicyName, StringComparison.OrdinalIgnoreCase)))));
        }
    }
});

// 5. Register MemoryCache & Application Services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SuperAdminTotpValidator>();
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
builder.Services.AddSingleton<ITafqeetService, TafqeetService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ITasweyaService, TasweyaService>();
builder.Services.AddScoped<IPaymentOrderService, PaymentOrderService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

// 6. MVC & Sanitized Audit Logging Filter (CRIT-03)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditBacklogActionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// 7. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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
        await DbInitializer.InitializeAsync(db, userMgr, roleMgr);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[!] خطأ أثناء تهيئة قاعدة البيانات: {Message}", ex.Message);
    }
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Central Press API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Export Program type for WebApplicationFactory in integration tests
public partial class Program { }
