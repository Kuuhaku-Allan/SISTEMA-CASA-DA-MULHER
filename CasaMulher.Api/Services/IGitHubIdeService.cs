using System.Threading.Tasks;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services
{
    public interface IGitHubIdeService
    {
        Task<GitHubIdeStatusDto> ObterStatusAsync();
        Task<GitHubPullRequestResultadoDto> CriarPullRequestAsync(GitHubIdeRevisaoRequest request, ApplicationUser usuario);
    }
}
