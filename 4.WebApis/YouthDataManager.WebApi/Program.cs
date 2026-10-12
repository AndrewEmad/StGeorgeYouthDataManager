using YouthDataManager.WebApi.Operations;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.WebApi.Authorization;
using YouthDataManager.Shared.Service.Abstractions;
using Microsoft.AspNetCore.Identity;
using YouthDataManager.Domain.Entities;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.WebApi.BackgroundWorkers;
using YouthDataManager.WebApi.DependencyInjection;
using YouthDataManager.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RequestTelemetry>();
builder.Services.AddHealthChecks().AddCheck<ReadinessCheck>("readiness",tags:["ready"]);

// Add services to the container
builder.Services.AddScoped<ICallerContext,CallerContext>();
builder.Services.AddScoped<TransactionalActionFilter>();
builder.Services.AddScoped<ResourceAccess>();
builder.Services.AddControllers(o => o.Filters.Add<TransactionalActionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Youth Data Manager V2", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Modular DI
builder.Services.AddWebApiServices(builder.Configuration);

// Authentication
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfiguration>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents { OnTokenValidated = async context => {
        var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = id == null ? null : await manager.FindByIdAsync(id);
        if (user == null || !user.IsActive || user.SecurityStamp != context.Principal?.FindFirstValue("ss")) { context.Fail("Session revoked."); return; }
        var store=context.HttpContext.RequestServices.GetRequiredService<IApplicationStore>();
        if(!Guid.TryParse(context.Principal?.FindFirstValue("fid"),out var family) || !await store.Query<RefreshSession>(false).AnyAsync(s=>s.UserId==user.Id && s.FamilyId==family && s.RevokedAt==null && s.ExpiresAt>DateTime.UtcNow,context.HttpContext.RequestAborted)){context.Fail("Session revoked.");return;}
        var roles = await manager.GetRolesAsync(user);
        if (!roles.Order().SequenceEqual(context.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).Order())) { context.Fail("Roles changed."); return; }
        context.HttpContext.Items["Account"] = user;
        context.HttpContext.Items["ProfileIncomplete"] = !roles.Contains("Admin") && !roles.Contains("Priest") && (string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.Phone) || string.IsNullOrWhiteSpace(user.Address));
    }};
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.ValidIssuer,
        ValidAudience = jwtConfig.ValidAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode = 429; options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10,Window = TimeSpan.FromMinutes(1),QueueLimit = 0 })); });

// Firebase Admin SDK
var firebaseKeyPath = builder.Configuration.GetValue<string>("Firebase:ServiceAccountKeyPath") ?? "firebase-service-account.json";
if (File.Exists(firebaseKeyPath) && FirebaseApp.DefaultInstance==null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseKeyPath)
    });
}

// Background worker for notification reminders
builder.Services.AddHostedService<NotificationReminderWorker>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply pending migrations and initialize required data before accepting requests.
// Enabled by default in every environment; isolated tests explicitly opt out.
if (builder.Configuration.GetValue<bool>("Database:InitializeOnStartup", true))
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<YouthDataManager.Infrastructure.Data.DatabaseSeeder>();
    await seeder.SeedAsync();
}


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<RequirePasswordChangeMiddleware>();
app.UseMiddleware<RequireProfileCompletionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health/ready",new HealthCheckOptions {Predicate=r=>r.Tags.Contains("ready")}).AllowAnonymous();
app.MapGet("/health/live",()=>Results.Ok(new {Status="alive"})).AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
