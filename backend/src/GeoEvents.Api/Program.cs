using GeoEvents.Application.Abstractions;
using GeoEvents.Application.EventProcessing;
using GeoEvents.Infrastructure.Messaging;
using GeoEvents.Infrastructure.Persistence;
using GeoEvents.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<PostgreSqlOptions>(
    builder.Configuration.GetSection("PostgreSql"));

// ===== Database =====
var connectionString = builder.Configuration.GetConnectionString("GeoEventsDb")
    ?? "Host=localhost;Port=5432;Database=geoevents;Username=postgres;Password=postgres";

builder.Services.AddDbContext<GeoEventsDbContext>(options =>
{
    options.UseNpgsql(connectionString, o =>
    {
        o.UseNetTopologySuite();
        o.MigrationsAssembly(typeof(GeoEventsDbContext).Assembly.FullName);
    });
});

// ===== Application Services =====
builder.Services.AddSingleton<IEventDispatcher, EventDispatcher>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// ===== Repositories =====
builder.Services.AddScoped<UnitRepository>();
builder.Services.AddScoped<ZoneRepository>();

// ===== Messaging =====
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqConsumer>();

// ===== API Services =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GeoEventsDbContext>("database");

var app = builder.Build();

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

// ===== Database Migration (Development only) =====
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GeoEventsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
