using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CasaMulher.Api.Services;

public sealed class HomologacaoSeedDocument
{
    public int Version { get; set; } = 1;
    public List<HomologacaoSeedFuncionario> Funcionarios { get; set; } = [];
    public List<HomologacaoSeedConvite> Convites { get; set; } = [];
    public List<HomologacaoSeedAuditoria> Auditoria { get; set; } = [];
    public List<HomologacaoSeedEmail> Emails { get; set; } = [];
    public List<HomologacaoSeedRecepcao> Recepcao { get; set; } = [];
}

public sealed record HomologacaoSeedFuncionario(string Identificador, string Nome, string Email, string Perfil, bool Ativo = false);
public sealed record HomologacaoSeedConvite(string Identificador, string Nome, string Email, string Perfil);
public sealed record HomologacaoSeedAuditoria(string Acao, string Entidade, string Descricao);
public sealed record HomologacaoSeedEmail(string Destinatario, string Assunto, string Tipo, string Status);
public sealed record HomologacaoSeedRecepcao(
    long Id,
    string Nome,
    string Cpf,
    string Telefone,
    string DataNascimento,
    string Curso,
    string Email,
    string Endereco,
    string Atendido,
    string Observacoes);

public sealed class HomologacaoSeedService
{
    private const string CacheKey = "homologacao-seed-document";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly GitHubPrivateFileService _github;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomologacaoSeedService> _logger;

    public HomologacaoSeedService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        GitHubPrivateFileService github,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IMemoryCache cache,
        ILogger<HomologacaoSeedService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _github = github;
        _configuration = configuration;
        _environment = environment;
        _cache = cache;
        _logger = logger;
    }

    public bool Enabled => _environment.IsStaging() && _configuration.GetValue("HML_SEED_ENABLED", true);
    public string SeedPath => _configuration["HML_SEED_PATH"] ?? "data/homologacao-seed.json";

    public async Task<HomologacaoSeedDocument?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Enabled) return null;
        if (_cache.TryGetValue(CacheKey, out HomologacaoSeedDocument? cached)) return cached;
        var localPath = _configuration["HML_SEED_LOCAL_PATH"];
        byte[]? content = null;

        if (!string.IsNullOrWhiteSpace(localPath))
        {
            content = await File.ReadAllBytesAsync(localPath, cancellationToken);
        }
        else if (_github.ReadConfigured)
        {
            content = (await _github.ReadAsync(SeedPath, cancellationToken))?.Content;
        }

        if (content is null) return null;
        var document = JsonSerializer.Deserialize<HomologacaoSeedDocument>(content, JsonOptions);
        if (document is not null) _cache.Set(CacheKey, document, TimeSpan.FromMinutes(10));
        return document;
    }

    public async Task<bool> ApplyIfNeededAsync(CancellationToken cancellationToken = default)
    {
        var document = await LoadAsync(cancellationToken);
        if (document is null) return false;
        var marker = document.Version.ToString();
        if (await _dbContext.AuditoriaEventos.AnyAsync(
                item => item.Acao == "HML_SEED_APLICADO" && item.EntidadeId == marker,
                cancellationToken))
        {
            return false;
        }

        foreach (var profile in document.Funcionarios.Select(item => item.Perfil)
                     .Concat(document.Convites.Select(item => item.Perfil))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!PerfisAcesso.EhFuncionarioInstitucionalValido(profile)) continue;
            if (!await _roleManager.RoleExistsAsync(profile))
            {
                await _roleManager.CreateAsync(new IdentityRole(profile));
            }
        }

        foreach (var item in document.Funcionarios)
        {
            if (!PerfisAcesso.EhFuncionarioInstitucionalValido(item.Perfil)) continue;
            var normalizedId = item.Identificador.Trim().ToUpperInvariant();
            var user = await _dbContext.Users.SingleOrDefaultAsync(
                current => current.IdentificadorFuncionario == normalizedId,
                cancellationToken);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    NomeCompleto = item.Nome.Trim(),
                    UserName = normalizedId,
                    IdentificadorFuncionario = normalizedId,
                    Email = item.Email.Trim(),
                    EmailConfirmed = true,
                    Perfil = item.Perfil.Trim().ToLowerInvariant(),
                    Ativo = item.Ativo,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    CriadoEm = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Seed não criou {normalizedId}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
                await _userManager.AddToRoleAsync(user, user.Perfil);
            }
        }

        foreach (var item in document.Convites)
        {
            var normalizedId = item.Identificador.Trim().ToUpperInvariant();
            if (await _dbContext.FuncionariosConvites.AnyAsync(
                    current => current.IdentificadorFuncionario == normalizedId,
                    cancellationToken)) continue;
            _dbContext.FuncionariosConvites.Add(new FuncionarioConvite
            {
                NomeCompleto = item.Nome,
                Email = item.Email,
                Perfil = item.Perfil,
                IdentificadorFuncionario = normalizedId,
                CodigoHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"HML-SEED:{normalizedId}"))),
                ExpiraEm = DateTime.UtcNow.AddMonths(6)
            });
        }

        foreach (var item in document.Auditoria)
        {
            _dbContext.AuditoriaEventos.Add(new AuditoriaEvento
            {
                Escopo = AuditoriaEscopos.Institucional,
                Acao = item.Acao,
                Entidade = item.Entidade,
                Descricao = item.Descricao,
                CriadoEm = DateTime.UtcNow
            });
        }

        foreach (var item in document.Emails)
        {
            _dbContext.EmailEventos.Add(new EmailEvento
            {
                Destinatario = item.Destinatario,
                Assunto = item.Assunto,
                Tipo = item.Tipo,
                Status = item.Status,
                CriadoEm = DateTime.UtcNow
            });
        }

        _dbContext.AuditoriaEventos.Add(new AuditoriaEvento
        {
            Escopo = AuditoriaEscopos.Sistema,
            Acao = "HML_SEED_APLICADO",
            Entidade = "HomologacaoSeed",
            EntidadeId = marker,
            Descricao = $"Seed fictício de homologação v{document.Version} aplicado.",
            CriadoEm = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed fictício de homologação v{Version} aplicado.", document.Version);
        return true;
    }
}
