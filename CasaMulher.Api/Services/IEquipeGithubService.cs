using CasaMulher.Api.DTOs;

namespace CasaMulher.Api.Services;

public interface IEquipeGithubService
{
    EquipeGithubStatusResponse ObterStatus();

    Task<EquipeGithubAtividadeResponse> ObterAtividadeAsync(CancellationToken cancellationToken);
}
