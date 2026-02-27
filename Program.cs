using dotenv.net;
using SUMMS.Api.Services;
using SUMMS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;

// ── Load .env file (ignored if missing so production env vars still work) ─────
DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true));

// Map GOOGLE_PLACES_API_KEY env var → IConfiguration key used in services
if (Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY") is { } placesKey)
    Environment.SetEnvironmentVariable("GooglePlaces__ApiKey", placesKey);

if (Environment.GetEnvironmentVariable("GOOGLE_MAPS_JS_API_KEY") is { } mapsKey)
    Environment.SetEnvironmentVariable("GoogleMaps__JsApiKey", mapsKey);

var builder = WebApplication.CreateBuilder(args);

// ── CORS ───────────────────────────────────────────────────────────────────────
var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Make env-based keys available via IConfiguration (must be before Build())
builder.Configuration.AddEnvironmentVariables();


// DB
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Controllers + Swagger ──────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new() { Title = "SUMMS API", Version = "v1" }); });

// ── Application Services (layered architecture) ───────────────────────────────
builder.Services.AddHttpClient<IMobilityService, GooglePlacesService>();
builder.Services.AddHttpClient<IBixiService, BixiService>();

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

// ── Legacy minimal-API endpoints (kept for backwards compat) ──────────────────
app.MapGet("/api/ping", () => Results.Ok(new { message = "pong", time = DateTimeOffset.UtcNow }));

// Expose the Maps JS API key to the frontend safely
app.MapGet("/api/config/maps-key", () =>
    Results.Ok(new { key = builder.Configuration["GoogleMaps:JsApiKey"] ?? "" }));

//TEST DB CONNECTION 
/*
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

*/
app.Run();