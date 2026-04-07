using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var originsFromConfig = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
var singleOrigin = builder.Configuration["AllowedOrigins"];

string[] allowedOrigins = [.. (originsFromConfig ?? [singleOrigin ?? ""])
    .Select(o => o?.Trim().TrimEnd('/'))
    .Where(o => !string.IsNullOrWhiteSpace(o))!];

if (allowedOrigins.Length == 0)
{
    Console.WriteLine("[WARNING] AllowedOrigins jest pusta!");
}
else
{
    Console.WriteLine($"[CORS] Aktywne źródła: |{string.Join("|", allowedOrigins)}|");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Environment.IsDevelopment()
        ? builder.Configuration.GetConnectionString("DefaultConnection")
        : builder.Configuration.GetConnectionString("DATABASE_URL");

    options.UseNpgsql(connectionString);
});

var tokenKey = builder.Configuration["AppSettings:Token"]
    ?? throw new InvalidOperationException("BŁĄD: Brak klucza JWT!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<IImageService, CloudinaryService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowVueApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();