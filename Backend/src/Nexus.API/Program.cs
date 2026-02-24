using Microsoft.EntityFrameworkCore;
using Nexus.API.Auth;
using Nexus.API.Hubs;
using Nexus.API.Health;
using Nexus.API.Security;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Application.Services;
using Nexus.Infrastructure.Data;
using Nexus.Infrastructure.Caching;
using Nexus.Infrastructure.Services;
using Nexus.Infrastructure.Resilience;
using Nexus.Infrastructure.Messaging;
// OpenText integration removed - using internal document identifier system
using Nexus.Infrastructure.Repositories;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Read Authentication Mode from configuration
var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfig>() 
    ?? new AuthenticationConfig { Mode = AuthenticationMode.Local };

builder.Services.AddSingleton(authConfig);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Swagger description for auth modes
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "NexusPM API",
        Version = "v1",
        Description = $"Authentication Mode: {authConfig.Mode}"
    });
});

// ==================== APPLICATION INSIGHTS (Azure optional) ====================
var monitoringMode = builder.Configuration.GetValue<string>("Monitoring:Mode", "Private");

if (monitoringMode == "Azure")
{
    // Azure Application Insights (Admin paneldən aktiv edəndə)
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["Monitoring:ApplicationInsights:ConnectionString"];
        options.EnableAdaptiveSampling = true;
        options.EnableQuickPulseMetricStream = true;
    });
}

// ==================== FEATURE FLAGS ====================
builder.Services.AddFeatureManagement();

// ==================== MEDAITR (CQRS) ====================
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// ==================== RESILIENCE (Polly) ====================
builder.Services.AddHttpClient("ExternalServices")
    .AddPolicyHandler(ResiliencePolicies.GetRetryPolicy())
    .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy());

// ==================== PRIVATE MONITORING (Enable/Disable switch) ====================
builder.Services.AddScoped<IMonitoringRepository, MonitoringRepository>();
builder.Services.AddScoped<IPrivateMonitoringService, PrivateMonitoringService>();

// ==================== EVENT BUS (Private default, Azure optional) ====================
var messagingMode = builder.Configuration.GetValue<string>("Messaging:Mode", "Private");

if (messagingMode == "Azure")
{
    // Azure Service Bus (Admin paneldən aktiv edəndə)
    builder.Services.AddSingleton<IEventBus, AzureServiceBus>();
}
else
{
    // Default: Tam şəxsi SQL Server-based Event Bus
    builder.Services.AddSingleton<IEventBus, PrivateEventBus>();
    builder.Services.AddHostedService<MessageQueueProcessor>();
    builder.Services.AddScoped<IMessageQueueRepository, MessageQueueRepository>();
}

// ==================== API GATEWAY (Ocelot) ====================
builder.Configuration.AddJsonFile("Gateway/ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

// ==================== ENTERPRISE SCALABILITY CONFIGURATION ====================

// 0. Read/Write Context Factory
builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>();

// 1. Database with Enterprise Optimizations
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
    {
        MaxPoolSize = 200,
        MinPoolSize = 10,
        ConnectTimeout = 30,
        CommandTimeout = 30,
        MultipleActiveResultSets = true,
        ApplicationName = "NexusPM",
        Encrypt = true,
        TrustServerCertificate = true
    };
    
    options.UseSqlServer(connBuilder.ConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.MinBatchSize(1);
        sqlOptions.MaxBatchSize(100);
        sqlOptions.MigrationsAssembly("Nexus.Infrastructure");
    });
    
    options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
});

// 2. Caching (SQL Server Distributed Cache - Windows native)
builder.Services.AddMemoryCache(); // L1: In-memory
builder.Services.AddNCacheDistributedCache(builder.Configuration); // L2: SQL Server
builder.Services.AddSingleton<IEnterpriseCacheService, EnterpriseCacheService>();

// 3. SignalR (SQL Server backplane - no Redis dependency)
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 50;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
})
.AddMessagePackProtocol(); // Binary serialization for performance

// SQL Server backplane for SignalR scale-out (if multiple servers)
if (builder.Configuration.GetValue<bool>("SignalR:UseSqlServerBackplane"))
{
    builder.Services.AddSignalR()
        .AddSqlServer(options =>
        {
            options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
            options.SchemaName = "SignalR";
        });
}

// 4. Enterprise Health Checks
builder.Services.AddEnterpriseHealthChecks(builder.Configuration);

// 5. Rate Limiting (per-user, per-endpoint)
builder.Services.AddRateLimiting(builder.Configuration);

// 6. API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// 7. Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// ==================== AUTHENTICATION CONFIGURATION ====================

// JWT Authentication (common for both modes)
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authentication Mode specific configuration
switch (authConfig.Mode)
{
    case AuthenticationMode.ActiveDirectory:
        // Active Directory Authentication only
        builder.Services.AddActiveDirectoryAuthentication();
        builder.Services.AddScoped<IActiveDirectoryAuthService, ActiveDirectoryAuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        break;
        
    case AuthenticationMode.Mixed:
        // Both AD and Local available
        builder.Services.AddActiveDirectoryAuthentication();
        builder.Services.AddScoped<IActiveDirectoryAuthService, ActiveDirectoryAuthService>();
        builder.Services.AddScoped<ILocalAuthService, LocalAuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        break;
        
    case AuthenticationMode.Local:
    default:
        // Local Authentication (Email + 2FA)
        builder.Services.AddScoped<ILocalAuthService, LocalAuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        break;
}

// ==================== CORE SERVICES ====================

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStorageSettingsRepository, StorageSettingsRepository>();
builder.Services.AddScoped<IStorageFileRepository, StorageFileRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ITaskDependencyRepository, TaskDependencyRepository>();
builder.Services.AddScoped<ITaskLabelRepository, TaskLabelRepository>();
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();

// Storage
builder.Services.AddScoped<IStorageFactory, StorageFactory>();
builder.Services.AddScoped<IDocumentFileService, DocumentFileService>();

// Services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentIdentifierService, DocumentIdentifierService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddSingleton<JwtService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:3000",
                "http://localhost:8080",
                "app://localhost")  // Flutter Windows
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    
    options.AddPolicy("AllowProduction", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetValue<string>("Cors:ProductionOrigin") ?? "https://nexus.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security headers middleware
app.UseSecurityHeaders();

// Private Monitoring Middleware (Enable/Disable from Admin Panel)
app.UseMiddleware<MonitoringMiddleware>();

// Response compression
app.UseResponseCompression();

// Rate limiting
app.UseRateLimiter();

// CORS
app.UseCors(app.Environment.IsProduction() ? "AllowProduction" : "AllowFlutter");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Request logging middleware
app.UseEnterpriseRequestLogging();

// API Gateway (Ocelot) - Load Balancing
await app.UseOcelot();

// Map endpoints
app.MapControllers();
app.MapHub<SyncHub>("/hubs/sync");
app.MapHub<NotificationHub>("/hubs/notifications");

// Enterprise Health Checks endpoint
app.MapEnterpriseHealthChecks();

// Readiness/Liveness probes for Kubernetes
app.MapGet("/ready", async (HealthCheckService healthCheck) =>
{
    var report = await healthCheck.CheckHealthAsync();
    var isReady = report.Status != HealthStatus.Unhealthy;
    return isReady 
        ? Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow })
        : Results.StatusCode(503);
});

app.MapGet("/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }));

// Database migration on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database migrated successfully.");
        logger.LogInformation("Authentication Mode: {AuthMode}", authConfig.Mode);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
