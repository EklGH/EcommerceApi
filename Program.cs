// PROJET en FULL DOCKER (API+SQL Server)
using EcommerceApi.Data;
using EcommerceApi.Middleware;
using EcommerceApi.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);



// ======== DATABASE

var conn = builder.Configuration.GetConnectionString("DefaultConnection");     // Récupère la chaîne de connexion depuis User Secrets

builder.Services.AddDbContext<EcommerceContext>(options =>                                               // Ajoute DbContext (SQL Server)
    options.UseSqlServer(conn, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);



// ======== JWT / AUTHENTICATION

var jwtSettings = builder.Configuration.GetSection("JwtSettings");      // Configuration JWT
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
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();



// ======== SERVICES / CONTROLLERS

// Controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();        // Validation automatique des DTO
})
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

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Background processing
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<PaymentProcessingBackgroundService>();



// ======== DOCKER PORT

builder.WebHost.UseUrls("http://+:8080");



// ======== BUILD APPLICATION

var app = builder.Build();



// ======== MIDDLEWARE

app.UseMiddleware<ExceptionMiddleware>();             // doit être en premier dans le pipeline

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1");
});

//app.UseHttpsRedirection();   // (permet à swagger de fonctionner avec docker) A ACTIVER EN PROD

app.UseAuthentication();     // tjrs avant Authorization
app.UseAuthorization();

app.MapControllers();



// ======== DATABASE MIGRATIONS + SEED

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