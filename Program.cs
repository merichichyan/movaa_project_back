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

// Increase Max Request Body Size to 50 MB for Base64 Logo uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50 MB
});

// Add DbContext
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
    {
        connectionString = ConvertPostgresUrlToConnectionString(connectionString);
    }
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

        // Ensure missing columns are dynamically added to existing PostgreSQL tables
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""PhoneNumber"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Category"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""WorkingHours"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Name"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""NameHy"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""NameEn"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""NameRu"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Address"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AddressHy"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AddressEn"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""AddressRu"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Email"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""Description"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""DescriptionHy"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""DescriptionEn"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""DescriptionRu"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""LogoUrl"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerFullName"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerNameHy"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerNameEn"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerNameRu"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""OwnerPhoneNumber"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""TaxId"" text;
                ALTER TABLE ""Salons"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;

                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitle"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitleHy"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitleEn"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""JobTitleRu"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""NameHy"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""NameEn"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""NameRu"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""Bio"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""BioHy"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""BioEn"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""BioRu"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ExperienceYears"" integer DEFAULT 0;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkingHours"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""CommissionRate"" double precision DEFAULT 0.0;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""Email"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""SalonId"" uuid;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""SalonName"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""Rating"" double precision DEFAULT 5.0;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""ServicesJson"" text DEFAULT '[]';
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""WorkplacesJson"" text DEFAULT '[]';
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                ALTER TABLE ""Specialists"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone DEFAULT NOW();

                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Phone"" text;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsBlocked"" boolean DEFAULT false;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsOnboardingCompleted"" boolean DEFAULT false;

                ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""ServiceId"" text;
                ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""Status"" text DEFAULT 'Confirmed';
                ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""SalonId"" uuid;
                ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""SalonName"" text;
                ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""IsNoShow"" boolean DEFAULT false;

                ALTER TABLE ""ServiceResources"" ADD COLUMN IF NOT EXISTS ""ServiceId"" text;
                ALTER TABLE ""ServiceResources"" ADD COLUMN IF NOT EXISTS ""ServiceName"" text;
                ALTER TABLE ""ServiceResources"" ADD COLUMN IF NOT EXISTS ""SalonId"" uuid;
                ALTER TABLE ""ServiceResources"" ADD COLUMN IF NOT EXISTS ""ResourceId"" uuid;
                ALTER TABLE ""ServiceResources"" ADD COLUMN IF NOT EXISTS ""RequiredQuantity"" integer DEFAULT 1;

                CREATE TABLE IF NOT EXISTS ""Offers"" (
                    ""Id"" uuid PRIMARY KEY,
                    ""Title"" text NOT NULL,
                    ""TitleHy"" text,
                    ""TitleEn"" text,
                    ""TitleRu"" text,
                    ""Subtitle"" text,
                    ""SubtitleHy"" text,
                    ""SubtitleEn"" text,
                    ""SubtitleRu"" text,
                    ""BadgeText"" text,
                    ""BadgeTextHy"" text,
                    ""BadgeTextEn"" text,
                    ""BadgeTextRu"" text,
                    ""DiscountPercent"" double precision,
                    ""SalonId"" uuid,
                    ""SalonName"" text,
                    ""SpecialistId"" uuid,
                    ""SpecialistName"" text,
                    ""ImageUrl"" text,
                    ""ValidUntil"" text,
                    ""OrderIndex"" integer DEFAULT 0,
                    ""IsActive"" boolean DEFAULT true,
                    ""CreatedAt"" timestamp with time zone DEFAULT NOW(),
                    ""UpdatedAt"" timestamp with time zone
                );

                ALTER TABLE ""Offers"" ADD COLUMN IF NOT EXISTS ""ValidUntil"" text;
                ALTER TABLE ""Offers"" ADD COLUMN IF NOT EXISTS ""OrderIndex"" integer DEFAULT 0;
            ");
        }
        catch (Exception migrationEx)
        {
            Console.WriteLine($"DB Auto-Migration notice: {migrationEx.Message}");
        }

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

        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Categories"" (
                    ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    ""NameHy"" TEXT NOT NULL,
                    ""NameEn"" TEXT NOT NULL,
                    ""NameRu"" TEXT NOT NULL,
                    ""IconName"" TEXT DEFAULT 'grid_view_rounded',
                    ""DisplayOrder"" INT DEFAULT 0,
                    ""IsActive"" BOOLEAN DEFAULT TRUE,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE
                );
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""UserFavorites"" (
                    ""Id"" UUID PRIMARY KEY,
                    ""UserId"" UUID NOT NULL,
                    ""TargetId"" TEXT NOT NULL,
                    ""Type"" TEXT NOT NULL,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserFavorites_UserId_TargetId_Type"" ON ""UserFavorites"" (""UserId"", ""TargetId"", ""Type"");
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Bookings"" (
                    ""Id"" UUID PRIMARY KEY,
                    ""SpecialistId"" UUID NOT NULL,
                    ""SpecialistName"" TEXT NOT NULL,
                    ""ServiceId"" TEXT,
                    ""ServiceName"" TEXT NOT NULL,
                    ""Price"" NUMERIC NOT NULL,
                    ""DurationMinutes"" INT NOT NULL,
                    ""BookingDate"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""TimeSlot"" TEXT NOT NULL,
                    ""UserId"" UUID NOT NULL,
                    ""UserEmail"" TEXT NOT NULL,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""IsNoShow"" BOOLEAN DEFAULT FALSE,
                    ""Status"" TEXT DEFAULT 'Confirmed',
                    ""SalonId"" UUID,
                    ""SalonName"" TEXT
                );

                CREATE TABLE IF NOT EXISTS ""SalonResources"" (
                    ""Id"" UUID PRIMARY KEY,
                    ""SalonId"" UUID NOT NULL,
                    ""Name"" TEXT NOT NULL,
                    ""Quantity"" INT NOT NULL,
                    ""Description"" TEXT,
                    ""IsActive"" BOOLEAN DEFAULT TRUE,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS ""ServiceResources"" (
                    ""Id"" UUID PRIMARY KEY,
                    ""SalonId"" UUID NOT NULL,
                    ""ServiceName"" TEXT NOT NULL,
                    ""ResourceId"" UUID NOT NULL,
                    ""RequiredQuantity"" INT DEFAULT 1
                );
            ");

            if (!dbContext.Categories.Any())
            {
                var defaultCategories = new List<movaa_project_back.Domain.Entities.Category>
                {
                    new("Վարսավիր", "Hair Stylist", "Парикмахер", "content_cut_rounded", 1),
                    new("Մատնահարդար", "Nail Art", "Ногтевой сервис", "auto_fix_high_rounded", 2),
                    new("Գեղեցկություն", "Beauty & Makeup", "Макияж", "face_retouching_natural_rounded", 3),
                    new("Barber Shop", "Barber Shop", "Барбершоп", "content_cut_outlined", 4),
                    new("Սպա / Մասաժ", "Spa & Massage", "СПА и Массаж", "spa_rounded", 5),
                    new("Կոսմետոլոգիա", "Cosmetology", "Косметология", "health_and_safety_rounded", 6),
                };
                dbContext.Categories.AddRange(defaultCategories);
                dbContext.SaveChanges();
                Console.WriteLine("Seeded initial categories successfully.");
            }
        }
        catch (Exception catEx)
        {
            Console.WriteLine($"Category Migration notice: {catEx.Message}");
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

var logosFolder = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "logos");
if (!Directory.Exists(logosFolder))
{
    Directory.CreateDirectory(logosFolder);
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
    }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(logosFolder),
    RequestPath = "/logos",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
    }
});

app.UseAuthentication();
app.UseAuthorization();

// Map API Routes
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapOfferEndpoints();
app.MapFavoritesEndpoints();
app.MapBookingEndpoints();
app.MapResourceEndpoints();

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

static string ConvertPostgresUrlToConnectionString(string postgresUrl)
{
    var uri = new Uri(postgresUrl);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo.Length > 0 ? userInfo[0] : "";
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
}
