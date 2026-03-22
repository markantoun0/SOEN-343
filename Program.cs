using dotenv.net;
using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Patterns.Command;
using SUMMS.Api.Patterns.Observer;
using SUMMS.Api.Services;
using SUMMS.Api.Services.Adapters;
using SUMMS.Api.Services.Interfaces;

DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true));

if (Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY") is { } placesKey)
    Environment.SetEnvironmentVariable("GooglePlaces__ApiKey", placesKey);

if (Environment.GetEnvironmentVariable("GOOGLE_MAPS_JS_API_KEY") is { } mapsKey)
    Environment.SetEnvironmentVariable("GoogleMaps__JsApiKey", mapsKey);

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new() { Title = "SUMMS API", Version = "v1" }); });

builder.Services.AddHttpClient<BixiAdapter>();
builder.Services.AddHttpClient<GooglePlacesAdapter>();

builder.Services.AddScoped<IBixiService>(sp => sp.GetRequiredService<BixiAdapter>());
builder.Services.AddScoped<IMobilityService>(sp => sp.GetRequiredService<GooglePlacesAdapter>());
builder.Services.AddScoped<IMobilityProviderAdapter>(sp => sp.GetRequiredService<BixiAdapter>());
builder.Services.AddScoped<IMobilityProviderAdapter>(sp => sp.GetRequiredService<GooglePlacesAdapter>());

builder.Services.AddScoped<IMobilityLocationService, MobilityLocationService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddScoped<ReservationCommandInvoker>();
builder.Services.AddScoped<IParkingEventPublisher, ParkingEventPublisher>();
builder.Services.AddScoped<IParkingObserver, LoggingParkingObserver>();

builder.Services.AddHostedService<CleanupHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigration");

    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied at startup.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply database migrations at startup.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/ping", () => Results.Ok(new { message = "pong", time = DateTimeOffset.UtcNow }));

app.MapGet("/api/config/maps-key", () =>
    Results.Ok(new { key = builder.Configuration["GoogleMaps:JsApiKey"] ?? "" }));

app.Run();