using System.Security.Claims;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace CasaMulher.Api.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RegistrarAsync(
        string acao,
        string entidade,
        string? entidadeId,
        string descricao,
        string? identificadorReferencia = null,
        string? escopo = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var usuarioId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        ApplicationUser? usuario = null;

        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            usuario = await _userManager.FindByIdAsync(usuarioId);
        }

        var identificadorClaim = httpContext?.User.FindFirstValue("identificadorFuncionario");
        var perfilClaim = httpContext?.User.FindFirstValue("perfil");
        var identificador = identificadorClaim ?? usuario?.IdentificadorFuncionario ?? identificadorReferencia ?? string.Empty;
        var perfil = perfilClaim ?? usuario?.Perfil ?? string.Empty;
        var escopoCalculado = DeterminarEscopo(
            escopo,
            httpContext?.Request.Path,
            acao,
            descricao,
            identificadorReferencia ?? identificador,
            perfil);

        _dbContext.AuditoriaEventos.Add(new AuditoriaEvento
        {
            UsuarioId = usuarioId,
            IdentificadorFuncionario = identificador,
            NomeFuncionario = usuario?.NomeCompleto ?? string.Empty,
            PerfilFuncionario = perfil,
            Escopo = escopoCalculado,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Descricao = descricao,
            IpOrigem = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            CriadoEm = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }

    private static string DeterminarEscopo(
        string? escopoExplicito,
        PathString? requestPath,
        string acao,
        string descricao,
        string? identificador,
        string? perfil)
    {
        if (!string.IsNullOrWhiteSpace(escopoExplicito))
        {
            return escopoExplicito;
        }

        var path = requestPath?.Value ?? string.Empty;
        var equipe = path.StartsWith("/api/portal-eqp", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/equipe", StringComparison.OrdinalIgnoreCase)
            || acao.StartsWith("EQUIPE_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.Equipe, StringComparison.OrdinalIgnoreCase)
            || (identificador?.StartsWith("EQP-", StringComparison.OrdinalIgnoreCase) ?? false)
            || descricao.Contains("EQP-", StringComparison.OrdinalIgnoreCase);

        return equipe ? AuditoriaEscopos.Equipe : AuditoriaEscopos.Institucional;
    }
}
