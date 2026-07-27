using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using movaa_project_back.Application.Services;
using movaa_project_back.Data;
using movaa_project_back.Data.Repositories;
using movaa_project_back.Domain.Interfaces;
using movaa_project_back.Infrastructure.Auth;
using movaa_project_back.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("MovaaDb"));
}

// Dependency Injection Registrations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Controllers / Endpoints
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Movaa API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? "SuperSecretKeyForMovaaProjectJwtAuthentication2026!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure Database & Tables Are Created Automatically on Neon PostgreSQL
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.EnsureCreated();

        // Seed default Admin account if not existing in Users table
        var adminPhone = "merichichyan";
        var existingAdmin = dbContext.Users.FirstOrDefault(u => u.Phone == adminPhone);
        if (existingAdmin == null)
        {
            var adminUser = new movaa_project_back.Domain.Entities.User(
                phone: adminPhone,
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Meri.12345"),
                fullName: "Admin Merichichyan",
                role: "admin",
                email: "admin@movaa.com"
            );
            dbContext.Users.Add(adminUser);
            dbContext.SaveChanges();
            Console.WriteLine("Seeded Admin user 'merichichyan' in Users table successfully.");
        }

        // Ensure Admins table is created in PostgreSQL if it doesn't exist yet
        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""Admins"" (
                ""Id"" uuid NOT NULL CONSTRAINT ""PK_Admins"" PRIMARY KEY,
                ""Username"" character varying(50) NOT NULL,
                ""PasswordHash"" text NOT NULL,
                ""FullName"" character varying(150) NULL,
                ""Role"" character varying(50) NOT NULL,
                ""Email"" character varying(255) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Admins_Username"" ON ""Admins"" (""Username"");
        ");

        // Seed default Admin in Admins table
        var existingAdminInAdminsTable = dbContext.Admins.FirstOrDefault(a => a.Username.ToLower() == adminPhone.ToLower());
        if (existingAdminInAdminsTable == null)
        {
            var adminObj = new movaa_project_back.Domain.Entities.Admin(
                username: adminPhone,
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Meri.12345"),
                fullName: "Meri Chichyan",
                email: "admin@movaa.com"
            );
            dbContext.Admins.Add(adminObj);
            dbContext.SaveChanges();
            Console.WriteLine("Seeded Admin user 'merichichyan' in Admins table successfully.");
        }
        else
        {
            existingAdminInAdminsTable.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Meri.12345");
            dbContext.SaveChanges();
            Console.WriteLine("Updated Admin user 'merichichyan' password hash successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ensuring database creation: {ex.Message}");
    }
}

// Enable Swagger in Development and Production for Easy API Testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movaa API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map API Routes
app.MapAuthEndpoints();
app.MapAdminEndpoints();

// Root status endpoint
app.MapGet("/", () => Results.Ok(new
{
    Status = "Online",
    Service = "Movaa Backend API",
    Timestamp = DateTime.UtcNow
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow
}));

app.Run();
