using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.Services;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DATABASE_URL"));
    }
});

var originsFromConfig = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
var singleOrigin = builder.Configuration["AllowedOrigins"];

string[] allowedOrigins = (originsFromConfig is { Length: > 0 })
    ? originsFromConfig
    : (!string.IsNullOrEmpty(singleOrigin) ? [singleOrigin] : []);

if (allowedOrigins.Length == 0)
{
    Console.WriteLine("[WARNING] AllowedOrigins is empty! CORS might block requests.");
}
else
{
    Console.WriteLine($"[CORS] Dozwolone źródła: {string.Join(", ", allowedOrigins)}");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var tokenKey = builder.Configuration["AppSettings:Token"]
    ?? throw new InvalidOperationException("JWT Token Key is missing!");

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
}else {
    app.UseHsts();
}

app.UseCors("AllowVueApp");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();