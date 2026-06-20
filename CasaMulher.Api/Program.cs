using System.Text;
using System.Threading.RateLimiting;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using CasaMulher.Api.Middleware;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Fido2NetLib;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.

var databaseProvider = builder.Configuration.GetValue("Database:Provider", "Sqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsStaging()
    && string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    var explicitConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    var sqlitePath = builder.Configuration["SQLITE_DB_PATH"];

    if (!string.IsNullOrWhiteSpace(explicitConnectionString))
    {
        connectionString = explicitConnectionString;
    }
    else
    {
        if (string.IsNullOrWhiteSpace(sqlitePath))
        {
            sqlitePath = Path.Combine(Path.GetTempPath(), "casa_mulher_hml.db");
        }

        var sqliteDirectory = Path.GetDirectoryName(sqlitePath);

        if (!string.IsNullOrWhiteSpace(sqliteDirectory))
        {
            Directory.CreateDirectory(sqliteDirectory);
        }

        connectionString = $"Data Source={sqlitePath}";
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection para o ambiente atual.");
}

var isSqlite = string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase)
    || string.Equals(databaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase);
var sqliteDatabasePath = isSqlite
    ? ResolverSqliteDatabasePath(connectionString, builder.Environment.ContentRootPath)
    : string.Empty;
builder.Services.AddSingleton(new HmlDbStorageInfo(sqliteDatabasePath, isSqlite));
builder.Services.AddSingleton<HmlDbSnapshotState>();

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
    .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
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
builder.Services.AddHttpClient<IEquipeDbGitHubService, EquipeDbGitHubService>();
builder.Services.AddHttpClient<GitHubPrivateFileService>();
builder.Services.AddScoped<EquipeDbSyncService>();
builder.Services.AddScoped<HmlDbSnapshotService>();
builder.Services.AddScoped<SecuritySnapshotPersistenceService>();
builder.Services.AddScoped<HomologacaoSeedService>();
builder.Services.AddScoped<ContaEquipeSincronizadaService>();
builder.Services.AddSingleton<GitHubPortalSessionStore>();
builder.Services.AddScoped<PortalEqpGateAuthorizationService>();
builder.Services.AddScoped<OwnerRecoveryService>();

if ((builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
    && builder.Configuration.GetValue("EquipeSync:Automatico", true))
{
    builder.Services.AddHostedService<EquipeDbAutoSyncHostedService>();
}

if (builder.Environment.IsStaging() && builder.Configuration.GetValue("HML_DB_SNAPSHOT_ENABLED", false))
{
    builder.Services.AddHostedService<HmlDbSnapshotAutoService>();
}

// WebAuthn / Passkey — RP ID e origins são específicos por ambiente/domínio.
var webAuthnInfo = ResolverWebAuthn(builder.Configuration, builder.Environment);

var fido2Config = new Fido2Configuration
{
    ServerName = webAuthnInfo.RpName,
    ServerDomain = webAuthnInfo.RpId,
    Origins = webAuthnInfo.Origins.ToHashSet(StringComparer.OrdinalIgnoreCase),
    TimestampDriftTolerance = 300000 // 5 min em ms
};

builder.Services.AddSingleton(webAuthnInfo);
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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
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

if (args.Contains("--validate-hml-snapshot-crypto", StringComparer.OrdinalIgnoreCase))
{
    ValidarSnapshotCrypto();
    return;
}

if (args.Contains("--repair-owner-security", StringComparer.OrdinalIgnoreCase))
{
    var scope = app.Services.CreateScope();
    var recoveryService = scope.ServiceProvider.GetRequiredService<OwnerRecoveryService>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.WriteLine("[!] Iniciando reparo de segurança do Owner localmente via CLI...");
    
    // Backup antes
    var tempPath = Path.GetTempPath();
    var dbPath = dbContext.Database.GetDbConnection().DataSource;
    if (File.Exists(dbPath))
    {
        var backupPath = Path.Combine(tempPath, $"casamulher_owner_repair_backup_{DateTime.Now:yyyyMMddHHmmss}.db");
        File.Copy(dbPath, backupPath);
        Console.WriteLine($"[i] Backup de segurança salvo em: {backupPath}");
    }

    Console.WriteLine("[i] Aplicando migrações...");
    dbContext.Database.Migrate();

    var result = recoveryService.ExecuteRecoveryAsync("CLI_LOCAL_OWNER").GetAwaiter().GetResult();
    
    if (result.IsSuccess)
    {
        Console.WriteLine("[OK] Reparo concluído com sucesso!");
    }
    else
    {
        Console.WriteLine($"[ERRO] Falha no reparo: {result.ErrorMessage}");
    }
    return;
}

var renderGateEnabled = bool.TryParse(app.Configuration["ENABLE_RENDER_GITHUB_GATE"], out var explicitGateValue)
    ? explicitGateValue
    : app.Configuration.GetValue<bool?>("RenderAccessGate:Enabled") ?? app.Environment.IsStaging();
app.Logger.LogInformation(
    "Inicializando CasaMulher.Api em {Environment}; GitHub Gate ativo={GateAtivo}.",
    app.Environment.EnvironmentName,
    renderGateEnabled);
app.Logger.LogInformation(
    "WebAuthn configurado: ambiente={Environment}; RP ID={RpId}; RP Name={RpName}; Origins={Origins}.",
    webAuthnInfo.EnvironmentName,
    webAuthnInfo.RpId,
    webAuthnInfo.RpName,
    string.Join(", ", webAuthnInfo.Origins));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    if (app.Environment.IsStaging() && isSqlite)
    {
        await using var restoreScope = app.Services.CreateAsyncScope();
        var snapshotService = restoreScope.ServiceProvider.GetRequiredService<HmlDbSnapshotService>();

        // Log de diagnóstico antes do restore — sem expor valores dos tokens
        var cfg = app.Configuration;
        app.Logger.LogInformation(
            "HML Snapshot PRÉ-RESTORE: ENABLED={Enabled} KEY_PRESENTE={Key} READ_TOKEN_PRESENTE={Read} WRITE_TOKEN_PRESENTE={Write} SQLITE_DB_PATH={DbPath}",
            cfg.GetValue<bool>("HML_DB_SNAPSHOT_ENABLED"),
            !string.IsNullOrWhiteSpace(cfg["HML_DB_SNAPSHOT_KEY"]),
            !string.IsNullOrWhiteSpace(cfg["GITHUB_EQP_READ_TOKEN"]),
            !string.IsNullOrWhiteSpace(cfg["GITHUB_EQP_WRITE_TOKEN"]),
            cfg["SQLITE_DB_PATH"] ?? "(não definido — usando padrão do código)");

        await snapshotService.TryRestoreAtStartupAsync();
        var snapshotStatus = snapshotService.GetStatus();
        app.Logger.LogInformation(
            "HML Snapshot PÓS-RESTORE: restoreConfigurado={Configurado}; snapshotAtivo={Ativo}; caminho={Path}.",
            snapshotService.RestoreConfigured,
            snapshotStatus.Configured,
            snapshotStatus.SnapshotPath);
    }

    await using var migrationScope = app.Services.CreateAsyncScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    app.Logger.LogInformation("Migrations aplicadas no ambiente {Environment}.", app.Environment.EnvironmentName);
}

if (app.Environment.IsStaging())
{
    await using var stagingScope = app.Services.CreateAsyncScope();
    try
    {
        var syncService = stagingScope.ServiceProvider.GetRequiredService<EquipeDbSyncService>();
        var syncResult = await syncService.SincronizarAsync(null);
        app.Logger.LogInformation("Sync EQP inicial concluído: {Message}", syncResult.Mensagem);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Sync EQP inicial falhou; o serviço periódico tentará novamente.");
    }

    try
    {
        var seedService = stagingScope.ServiceProvider.GetRequiredService<HomologacaoSeedService>();
        await seedService.ApplyIfNeededAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Seed fictício de homologação não pôde ser aplicado.");
    }
}

var runDemoSeed = app.Configuration.GetValue("Seed:RunDemoData", app.Environment.IsDevelopment());

if (runDemoSeed)
{
    await AuthDbSeeder.SeedAsync(app.Services);
}

app.UseForwardedHeaders();
app.UseMiddleware<RenderAccessGateMiddleware>();
app.UseHttpsRedirection();

var telasPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "projetocasadamulher", "telas"));

if (Directory.Exists(telasPath))
{
    var telasFileProvider = new PhysicalFileProvider(telasPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = telasFileProvider
    });
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("FrontendLocal");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/equipe.html"));
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

static WebAuthnEnvironmentInfo ResolverWebAuthn(
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    var defaultRpId = environment.IsStaging()
        ? "casa-mulher-eqp.onrender.com"
        : "localhost";
    var rpId = (configuration["WEBAUTHN_RP_ID"]
        ?? configuration["Fido2:RpId"]
        ?? defaultRpId).Trim();

    if (rpId.Contains("://", StringComparison.Ordinal)
        || rpId.Contains('/', StringComparison.Ordinal)
        || rpId.Contains(':', StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(rpId))
    {
        throw new InvalidOperationException(
            "WEBAUTHN_RP_ID deve conter somente o domínio, sem protocolo, porta ou caminho.");
    }

    var rpName = (configuration["WEBAUTHN_RP_NAME"]
        ?? configuration["Fido2:RpName"]
        ?? (environment.IsStaging()
            ? "Sistema Casa da Mulher - Homologação"
            : "Sistema Casa da Mulher")).Trim();
    var rawOrigins = configuration["WEBAUTHN_ORIGINS"]
        ?? configuration["Fido2:Origins"]
        ?? configuration["Fido2:Origin"];
    var origins = string.IsNullOrWhiteSpace(rawOrigins)
        ? environment.IsStaging()
            ? [$"https://{rpId}"]
            : ["http://localhost:5500", "http://localhost:5001"]
        : rawOrigins
            .Trim()
            .TrimStart('[')
            .TrimEnd(']')
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Trim().Trim('"', '\''))
            .ToArray();

    var normalizedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var origin in origins)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.PathAndQuery) && uri.PathAndQuery != "/")
        {
            throw new InvalidOperationException($"Origem WebAuthn inválida: {origin}.");
        }

        if (environment.IsStaging() && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("WEBAUTHN_ORIGINS deve usar HTTPS em Staging.");
        }

        if (!string.Equals(uri.Host, rpId, StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith($".{rpId}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"A origem WebAuthn {origin} não pertence ao RP ID {rpId}.");
        }

        normalizedOrigins.Add(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'));
    }

    return new WebAuthnEnvironmentInfo(rpId, rpName, normalizedOrigins, environment.EnvironmentName);
}

static string ResolverSqliteDatabasePath(string connectionString, string contentRootPath)
{
    var sqlite = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqlite.DataSource))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection não contém Data Source SQLite.");
    }

    return Path.GetFullPath(Path.IsPathRooted(sqlite.DataSource)
        ? sqlite.DataSource
        : Path.Combine(contentRootPath, sqlite.DataSource));
}

static void ValidarSnapshotCrypto()
{
    var header = Encoding.ASCII.GetBytes("SQLite format 3\0");
    var sample = header.Concat(System.Security.Cryptography.RandomNumberGenerator.GetBytes(2048)).ToArray();
    var key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    var encrypted = HmlDbSnapshotCrypto.EncryptCompressed(sample, key);
    var decrypted = HmlDbSnapshotCrypto.DecryptDecompressed(encrypted, key);
    if (!sample.SequenceEqual(decrypted))
    {
        throw new InvalidOperationException("Round-trip criptográfico do snapshot falhou.");
    }

    encrypted[^1] ^= 0x01;
    try
    {
        HmlDbSnapshotCrypto.DecryptDecompressed(encrypted, key);
        throw new InvalidOperationException("Snapshot adulterado foi aceito indevidamente.");
    }
    catch (System.Security.Cryptography.AuthenticationTagMismatchException)
    {
        Console.WriteLine("[OK] Snapshot AES-256-GCM fez round-trip e rejeitou adulteração.");
    }
    finally
    {
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(sample);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(decrypted);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
    }
}
