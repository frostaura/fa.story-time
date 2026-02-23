using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StoryTime.Api.Data;
using StoryTime.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Get database connection string from environment or use default
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? "Host=postgres;Database=storytime;Username=storytime;Password=storytime";

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<StoryTimeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ITtsService, TtsService>();
builder.Services.AddScoped<IStoryService, StoryService>();

// Add controllers
builder.Services.AddControllers();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("StoryTime.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://signoz:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://signoz:4317");
        }));

// Add API documentation
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        var context = services.GetRequiredService<StoryTimeDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        // Seed default data
        logger.LogInformation("Seeding database...");
        await DbSeeder.SeedAsync(context, logger);
        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        // Don't throw - allow the app to start even if seeding fails
    }
}

// Enable CORS
app.UseCors();

// Enable static files (for SPA)
app.UseStaticFiles();

// Map controllers
app.MapControllers();

// SPA fallback routing
app.MapFallbackToFile("index.html");

app.Run();
