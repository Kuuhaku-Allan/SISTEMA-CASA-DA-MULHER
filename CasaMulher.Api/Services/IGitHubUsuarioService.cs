using System.Threading.Tasks;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services
{
    public interface IGitHubUsuarioService
    {
        Task<GitHubConexaoStatusDto> ObterStatusConexaoAsync(ApplicationUser usuario);
        Task<string> CriarUrlAutorizacaoAsync(ApplicationUser usuario, string requestIp, string userAgent);
        Task ProcessarCallbackAsync(string code, string state);
        Task DesconectarAsync(ApplicationUser usuario);
    }
}
