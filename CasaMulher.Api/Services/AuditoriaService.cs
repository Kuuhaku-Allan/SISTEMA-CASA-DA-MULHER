using System.Security.Claims;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
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

    public async Task RegistrarAsync(string acao, string entidade, string? entidadeId, string descricao)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var usuarioId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        ApplicationUser? usuario = null;

        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            usuario = await _userManager.FindByIdAsync(usuarioId);
        }

        _dbContext.AuditoriaEventos.Add(new AuditoriaEvento
        {
            UsuarioId = usuarioId,
            IdentificadorFuncionario = usuario?.IdentificadorFuncionario ?? string.Empty,
            NomeFuncionario = usuario?.NomeCompleto ?? string.Empty,
            PerfilFuncionario = usuario?.Perfil ?? string.Empty,
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
}
