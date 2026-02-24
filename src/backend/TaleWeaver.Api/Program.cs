using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TaleWeaver.Api.Data;
using TaleWeaver.Api.Middleware;
using TaleWeaver.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------
// Database
// -------------------------------------------------------------------
builder.Services.AddDbContext<TaleWeaverDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------------------------------------------------------
// Application Services
// -------------------------------------------------------------------
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();
builder.Services.AddHttpClient<ITtsService, TtsService>();
builder.Services.AddScoped<IStoryGenerationPipeline, StoryGenerationPipeline>();
builder.Services.AddScoped<IStripeService, StripeService>();

// -------------------------------------------------------------------
// Controllers + Swagger / OpenAPI
// -------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------------------------------------------------------------------
// CORS — allow the Vite dev frontend
// -------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// -------------------------------------------------------------------
// OpenTelemetry
// -------------------------------------------------------------------
var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "TaleWeaver.Api";
var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(otelServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint));
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint));
});

// -------------------------------------------------------------------
// Structured JSON logging
// -------------------------------------------------------------------
builder.Logging.AddJsonConsole(options =>
{
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});

// -------------------------------------------------------------------
// Build
// -------------------------------------------------------------------
var app = builder.Build();

// -------------------------------------------------------------------
// Middleware pipeline
// -------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseMiddleware<SoftUserIdMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
