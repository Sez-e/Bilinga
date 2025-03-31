using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using backend.Services;
using backend.Data;
using bili;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Загружаем переменные окружения из .env файла
DotNetEnv.Env.Load();

// Проверяем наличие необходимых переменных
var requiredEnvVars = new[]
{
    "EMAIL_SMTP_SERVER",
    "EMAIL_SMTP_PORT",
    "EMAIL_FROM",
    "EMAIL_PASSWORD"
};

foreach (var envVar in requiredEnvVars)
{
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
    {
        throw new InvalidOperationException($"Missing required environment variable: {envVar}");
    }
}

var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var botToken = Environment.GetEnvironmentVariable("TG_TOKEN");

if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("JWT переменные не заданы.");
}

if (string.IsNullOrWhiteSpace(botToken))
{
    throw new InvalidOperationException("TELEGRAM_BOT_TOKEN не задан.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton(new JwtService(secretKey, issuer, audience));
builder.Services.AddSingleton(new TGbot(botToken));
builder.Services.AddScoped<EmailService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<PasswordGeneratorService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BILINGA API",
        Version = "v1",
        Description = "API Documentation with Swagger"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите токен JWT в формате: Bearer <token>"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 🟢 Запускаем Telegram-бота в фоне
var bot = app.Services.GetRequiredService<TGbot>();
_ = Task.Run(() => bot.Start());

// 🔐 Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BILINGA API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.Run();