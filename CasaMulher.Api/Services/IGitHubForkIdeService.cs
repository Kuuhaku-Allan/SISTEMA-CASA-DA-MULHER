using System.Threading.Tasks;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services
{
    public interface IGitHubForkIdeService
    {
        Task<GitHubPullRequestResultadoDto> CriarPullRequestViaForkAsync(GitHubIdeRevisaoRequest request, ApplicationUser usuario);
    }
}
