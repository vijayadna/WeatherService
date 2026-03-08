using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using WeatherService.API;
using WeatherService.Application.Services;
using WeatherService.Infrastructure;
using WeatherService.Infrastructure.Repositories;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Database ─────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<WeatherDbContext>(opts =>
        opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ─── Repositories & Services ──────────────────────────────────────────────
    builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
    builder.Services.AddScoped<IWeatherService, WeatherServiceImpl>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
 
    // ─── HTTP Client with Polly Resilience ────────────────────────────────────
    builder.Services.AddHttpClient<IExternalWeatherService, OpenWeatherMapService>()
        .AddStandardResilienceHandler(opts =>
        {
            opts.Retry.MaxRetryAttempts = 3;
            opts.Retry.Delay = TimeSpan.FromSeconds(1);
            opts.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("ExternalApis:OpenWeatherMap:TimeoutSeconds", 10));
        });

    // ─── Authentication / JWT ─────────────────────────────────────────────────
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();

    // ─── Rate Limiting ────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(
        builder.Configuration.GetSection("RateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // ─── Controllers ──────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ─── Swagger / OpenAPI ────────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Weather Microservice API",
            Version = "v1",
            Description = """
                A production-grade Weather REST API backed by OpenWeatherMap.

                **Features**
                - Current conditions
                - Weather Forecast
                - CSV export
                - Weather alert subscriptions
                - JWT-secured endpoints with rate limiting

                **Demo credentials**  
                `admin / Admin@Weather1!` or `readonly / ReadOnly@Weather1!`
                """,
            Contact = new OpenApiContact { Name = "Weather Service Team" }
        });

        // JWT security definition
        var jwtScheme = new OpenApiSecurityScheme
        {
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "Paste your JWT token here (without 'Bearer ' prefix).",
            Reference = new OpenApiReference { Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme }
        };
        c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });

        // XML comments
        var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

        c.EnableAnnotations();
    });
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<WeatherDbContext>("database");

    // ─── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(p => p
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()));

    // ─── Build ────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── DB Migration ─────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        db.Database.EnsureCreated();
    }

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<GlobalExceptionMiddleware>();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Weather API starting up...");

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather API v1");
            c.RoutePrefix = string.Empty; // Swagger at root
            c.DocumentTitle = "Weather Microservice";
            c.DisplayRequestDuration();
        });
    }
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health");

    logger.LogInformation("Weather Microservice started");
    app.Run();
}
catch (Exception ex)
{
}