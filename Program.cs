using EcommerceApi.Data;
using EcommerceApi.Middleware;
using EcommerceApi.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);



// ======== DATABASE

// Recupere la chaine de connexion vers SQLite depuis appsettings.json
var conn = builder.Configuration.GetConnectionString("DefaultConnection");

// ajoute DbContext (changer UseSqlite en UseSqlServer si on utilise SQL Server)
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseSqlite(conn));



// ======== JWT / AUTHENTICATION

var jwtSettings = builder.Configuration.GetSection("Jwt");      // Configuration JWT
var keyString = jwtSettings["Key"];

if (string.IsNullOrEmpty(keyString))
    throw new Exception("La clé JWT est vide ou absente dans appsettings.json !");

var key = Encoding.UTF8.GetBytes(keyString);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();



// ======== SERVICES / CONTROLLERS

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    );      // ignore les cycles JSON (pour tester Postman +facilement)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ecommerce API", Version = "v1" });

    // Définie la sécurité JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Mettre: Bearer {votre token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// SERVICES :
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

// Payments/Background queue
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<PaymentProcessingBackgroundService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();



// ======== HTTP PIPELINE

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();             // doit être en premier dans le pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();     // tjrs avant Authorization
app.UseAuthorization();

app.MapControllers();



// ======== MIGRATIONS + SEED

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
    ctx.Database.Migrate();             // applique les migrations
    DbInitializer.Initialize(ctx);      // ajoute les données de test si besoin

    // Espace ci-dessous pour Tests rapides
    Console.WriteLine("=== USERS ===");
    foreach (var u in ctx.Users.ToList())
    {
        Console.WriteLine($"{u.Id} - {u.Username} - {u.Role}");
    }

    Console.WriteLine("=== PRODUCTS ===");
    foreach (var p in ctx.Products.ToList())
    {
        Console.WriteLine($"{p.Id} - {p.Name} - {p.Price}€ - Stock: {p.Stock}");
    }
}

app.Run();