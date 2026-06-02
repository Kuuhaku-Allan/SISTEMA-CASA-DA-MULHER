using System.Text;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.

var databaseProvider = builder.Configuration.GetValue("Database:Provider", "Sqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection para o ambiente atual.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
        || string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
        return;
    }

    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase)
        || string.Equals(databaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    throw new InvalidOperationException($"Database:Provider invalido: {databaseProvider}.");
});

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Configure Jwt:Key em appsettings.json.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PoliticasAcesso.SomenteAdm, policy =>
        policy.RequireRole(PerfisAcesso.Adm));

    options.AddPolicy(PoliticasAcesso.AcessoRecepcao, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.Recepcao));

    options.AddPolicy(PoliticasAcesso.AcessoCursos, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.Professor));

    options.AddPolicy(PoliticasAcesso.AcessoProntuarioSocial, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.AssistenteSocial));

    options.AddPolicy(PoliticasAcesso.AcessoJuridico, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.Juridico));

    options.AddPolicy(PoliticasAcesso.AcessoRelatorios, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.AssistenteSocial, PerfisAcesso.Juridico));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendLocal", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IConviteCodigoService, ConviteCodigoService>();
builder.Services.AddScoped<IFuncionarioIdentificadorService, GeradorIdentificadorFuncionarioService>();
builder.Services.AddScoped<ISenhaTemporariaService, SenhaTemporariaService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

var emailProvider = builder.Configuration.GetValue("Email:Provider", builder.Environment.IsDevelopment() ? "Fake" : "Smtp");

if (string.Equals(emailProvider, "Fake", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, FakeEmailService>();
}
else if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    throw new InvalidOperationException($"Email:Provider invalido: {emailProvider}.");
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Casa da Mulher API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato Bearer."
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var runDemoSeed = app.Configuration.GetValue("Seed:RunDemoData", app.Environment.IsDevelopment());

if (runDemoSeed)
{
    await AuthDbSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseCors("FrontendLocal");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
