using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NeuroSync.Api.Auth;
using NeuroSync.Api.Data;
using NeuroSync.Api.Health;
using NeuroSync.Api.Middleware;
using NeuroSync.Api.Services;
using Serilog;
using StackExchange.Redis;

namespace NeuroSync.Api.Hosting;

public static class LiveBetaComposition
{
    public static void AddLiveBeta(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 1_048_576;
        });

        builder.Host.UseSerilog((context, _, logger) =>
        {
            logger.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<LiveBetaMetrics>();
        AddDatabase(builder);
        AddIdentity(builder);
        AddSessionCache(builder);
        builder.Services.AddScoped<IAccountDataService, AccountDataService>();
        builder.Services.AddScoped<IAuditLog, AuditLog>();
        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddSingleton(sp => new LlmUsageGovernor(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ILogger<LlmUsageGovernor>>(),
            sp.GetRequiredService<LiveBetaMetrics>(),
            sp.GetService<IConnectionMultiplexer>()));
        builder.Services.AddHostedService<RetentionSweepService>();
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" });
        AddRateLimits(builder);
    }

    public static void UseLiveBeta(this WebApplication app)
    {
        ProductionConfigurationGuard.Validate(app.Environment, app.Configuration);
        ApplySchema(app);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
        });
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (app.Configuration.GetValue("Https:TrustProxy", false))
        {
            var forwarded = new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            };
            forwarded.KnownNetworks.Clear();
            forwarded.KnownProxies.Clear();
            app.UseForwardedHeaders(forwarded);
        }

        if (!app.Environment.IsDevelopment())
        {
            if (app.Configuration.GetValue("Https:Hsts", true))
                app.UseHsts();
            if (app.Configuration.GetValue("Https:Redirect", false))
                app.UseHttpsRedirection();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseCors("NeuroSyncCors");
        app.UseMiddleware<ApiKeyAuthMiddleware>();
        app.UseRouting();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseMiddleware<UserIdentityEnforcementMiddleware>();
        app.UseMiddleware<PrivacyHydratorMiddleware>();
    }

    private static void AddDatabase(WebApplicationBuilder builder)
    {
        var inMemoryName = "NeuroSync_" + Guid.NewGuid().ToString("N");
        builder.Services.AddDbContext<NeuroSyncDbContext>(options =>
        {
            var config = builder.Configuration;
            var connectionString = config.GetConnectionString("DefaultConnection");
            var provider = ResolveProvider(config["Database:Provider"], connectionString);
            switch (provider)
            {
                case "postgres":
                    options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly("NeuroSync.Api"));
                    break;
                case "sqlite":
                    options.UseSqlite(string.IsNullOrWhiteSpace(connectionString)
                        ? "Data Source=Data/neurosync.db"
                        : connectionString);
                    break;
                case "sqlserver":
                    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("NeuroSync.Api"));
                    break;
                default:
                    options.UseInMemoryDatabase(inMemoryName);
                    break;
            }
        });
    }

    private static void AddIdentity(WebApplicationBuilder builder)
    {
        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddEntityFrameworkStores<NeuroSyncDbContext>()
            .AddDefaultTokenProviders();

        var configuration = builder.Configuration;
        var environment = builder.Environment;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                    {
                        var key = AuthPolicy.SigningKey(configuration, environment);
                        return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) };
                    },
                    IssuerValidator = (issuer, _, _) =>
                    {
                        var expected = configuration["Auth:Jwt:Issuer"] ?? "NeuroSync";
                        if (!string.Equals(issuer, expected, StringComparison.Ordinal))
                            throw new SecurityTokenInvalidIssuerException("Issuer is not accepted.");
                        return issuer;
                    },
                    AudienceValidator = (audiences, _, _) =>
                    {
                        var expected = configuration["Auth:Jwt:Audience"] ?? "NeuroSync";
                        return audiences.Contains(expected, StringComparer.Ordinal);
                    }
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("NeuroSync.Auth");
                        logger.LogWarning("JWT rejected: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/emotionHub"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();
    }

    private static void AddSessionCache(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<EphemeralSessionContextService>();
        var redis = builder.Configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));
            builder.Services.AddSingleton<IEphemeralSessionContext, RedisSessionContextStore>();
            return;
        }

        builder.Services.AddSingleton<IEphemeralSessionContext>(sp => sp.GetRequiredService<EphemeralSessionContextService>());
    }

    private static void AddRateLimits(WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                  ?? ctx.Connection.RemoteIpAddress?.ToString()
                                  ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            options.AddPolicy("auth", ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            options.AddPolicy("companion", ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? ctx.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 40,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            options.OnRejected = async (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many requests. Please try again later." }, token);
            };
        });
    }

    private static void ApplySchema(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeuroSyncDbContext>();
        if (!db.Database.IsRelational())
            return;

        var provider = ResolveProvider(app.Configuration["Database:Provider"], app.Configuration.GetConnectionString("DefaultConnection"));
        if (provider == "sqlite")
        {
            Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data"));
            db.Database.EnsureCreated();
            return;
        }

        if (app.Configuration.GetValue("Database:ApplyMigrations", true))
            db.Database.Migrate();
    }

    public static string ResolveProvider(string? configured, string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim().ToLowerInvariant() switch
            {
                "postgresql" => "postgres",
                var other => other
            };

        if (string.IsNullOrWhiteSpace(connectionString))
            return "inmemory";
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return "postgres";
        return "sqlserver";
    }
}
