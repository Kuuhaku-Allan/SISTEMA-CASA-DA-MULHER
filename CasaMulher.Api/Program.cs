using System.Text;
using System.Threading.RateLimiting;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Fido2NetLib;
using Microsoft.AspNetCore.RateLimiting;
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

// Contas EQP sao amplas apenas em desenvolvimento/homologacao para testes do projeto.
// Em producao, as politicas abaixo voltam aos perfis institucionais reais.
var permitirEquipeDev = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
var rolesSomenteAdm = IncluirEquipeDev(permitirEquipeDev, PerfisAcesso.Adm);
var rolesRecepcao = IncluirEquipeDev(permitirEquipeDev, PerfisAcesso.Adm, PerfisAcesso.Recepcao);
var rolesCursos = IncluirEquipeDev(permitirEquipeDev, PerfisAcesso.Adm, PerfisAcesso.Professor);
var rolesProntuarioSocial = IncluirEquipeDev(permitirEquipeDev, PerfisAcesso.Adm, PerfisAcesso.AssistenteSocial);
var rolesJuridico = IncluirEquipeDev(permitirEquipeDev, PerfisAcesso.Adm, PerfisAcesso.Juridico);
var rolesRelatorios = IncluirEquipeDev(permitirEquipeDev, PerfisAcesso.Adm, PerfisAcesso.AssistenteSocial, PerfisAcesso.Juridico);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PoliticasAcesso.SomenteAdm, policy =>
        policy.RequireRole(rolesSomenteAdm));

    options.AddPolicy(PoliticasAcesso.AcessoRecepcao, policy =>
        policy.RequireRole(rolesRecepcao));

    options.AddPolicy(PoliticasAcesso.AcessoCursos, policy =>
        policy.RequireRole(rolesCursos));

    options.AddPolicy(PoliticasAcesso.AcessoProntuarioSocial, policy =>
        policy.RequireRole(rolesProntuarioSocial));

    options.AddPolicy(PoliticasAcesso.AcessoJuridico, policy =>
        policy.RequireRole(rolesJuridico));

    options.AddPolicy(PoliticasAcesso.AcessoRelatorios, policy =>
        policy.RequireRole(rolesRelatorios));

    options.AddPolicy(PoliticasAcesso.AcessoEquipe, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.Equipe));

    options.AddPolicy(PoliticasAcesso.GerenciarConvitesEquipe, policy =>
        policy.RequireRole(PerfisAcesso.Adm, PerfisAcesso.Equipe));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "{\"mensagem\":\"Muitas tentativas. Aguarde alguns minutos e tente novamente.\"}",
            cancellationToken);
    };

    options.AddPolicy(RateLimitPolicies.Login, context =>
        CriarLimitadorPorIp(context, permitLimit: 5, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.LoginDoisFatores, context =>
        CriarLimitadorPorIp(context, permitLimit: 5, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.ConvitePublico, context =>
        CriarLimitadorPorIp(context, permitLimit: 10, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.EquipeAtivacao, context =>
        CriarLimitadorPorIp(context, permitLimit: 5, TimeSpan.FromMinutes(5)));

    options.AddPolicy(RateLimitPolicies.SolicitarRedefinicaoSenha, context =>
        CriarLimitadorPorIp(context, permitLimit: 3, TimeSpan.FromMinutes(15)));

    options.AddPolicy(RateLimitPolicies.RedefinirSenha, context =>
        CriarLimitadorPorIp(context, permitLimit: 5, TimeSpan.FromMinutes(15)));

    options.AddPolicy(RateLimitPolicies.PasskeyLoginIniciar, context =>
        CriarLimitadorPorIp(context, permitLimit: 10, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.PasskeyLoginConcluir, context =>
        CriarLimitadorPorIp(context, permitLimit: 10, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.PasskeyReconfirmar, context =>
        CriarLimitadorPorIp(context, permitLimit: 5, TimeSpan.FromMinutes(1)));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendLocal", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .SetIsOriginAllowed(OrigemDesenvolvimentoPermitida)
                .AllowAnyHeader()
                .AllowAnyMethod();

            return;
        }

        var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            policy
                .WithOrigins(frontendBaseUrl.TrimEnd('/'))
                .AllowAnyHeader()
                .AllowAnyMethod();

            return;
        }

        policy
            .SetIsOriginAllowed(_ => false)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IConviteCodigoService, ConviteCodigoService>();
builder.Services.AddScoped<IFuncionarioIdentificadorService, GeradorIdentificadorFuncionarioService>();
builder.Services.AddScoped<IMasterUserService, MasterUserService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IRedefinicaoSenhaEmailService, RedefinicaoSenhaEmailService>();
builder.Services.AddScoped<IEmailRecuperacaoEmailService, EmailRecuperacaoEmailService>();
builder.Services.AddSingleton<IRedefinicaoSenhaThrottleService, InMemoryRedefinicaoSenhaThrottleService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IEquipeGithubService, EquipeGithubService>();

// WebAuthn / Passkey — Fido2 v3 é instanciado diretamente (sem extension method)
var fido2Origin = builder.Environment.IsDevelopment()
    ? "http://localhost:5500"
    : builder.Configuration["Fido2:Origin"] ?? "http://localhost:5500";

var fido2RpId = builder.Environment.IsDevelopment()
    ? "localhost"
    : builder.Configuration["Fido2:RpId"] ?? "localhost";

var fido2Config = new Fido2Configuration
{
    ServerName = "Casa da Mulher",
    ServerDomain = fido2RpId,
    Origins = new HashSet<string> { fido2Origin },
    TimestampDriftTolerance = 300000 // 5 min em ms
};

builder.Services.AddSingleton<IFido2>(new Fido2(fido2Config));

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

app.UseRouting();
app.UseCors("FrontendLocal");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static RateLimitPartition<string> CriarLimitadorPorIp(HttpContext context, int permitLimit, TimeSpan window)
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido";

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ip,
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        });
}

static bool OrigemDesenvolvimentoPermitida(string origin)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (string.Equals(origin, "http://localhost:5500", StringComparison.OrdinalIgnoreCase)
        || string.Equals(origin, "http://127.0.0.1:5500", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    return string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)
        && uri.Host.EndsWith(".app.github.dev", StringComparison.OrdinalIgnoreCase);
}

static string[] IncluirEquipeDev(bool permitirEquipeDev, params string[] perfis)
{
    if (!permitirEquipeDev)
    {
        return perfis;
    }

    return perfis
        .Append(PerfisAcesso.Equipe)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
