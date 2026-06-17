namespace CasaMulher.Api.Services;

public interface IEquipeDbGitHubService
{
    bool LeituraConfigurada { get; }

    bool EscritaConfigurada { get; }

    string RepositoryLabel { get; }

    string DbPath { get; }

    string EventsPath { get; }

    Task<EquipeDbFile> LerAsync(CancellationToken cancellationToken = default);

    Task SalvarAsync(
        EquipeDbDocument document,
        string? sha,
        string commitMessage,
        CancellationToken cancellationToken = default);

    Task AcrescentarEventoAsync(
        EquipeDbEvent evento,
        string commitMessage,
        CancellationToken cancellationToken = default);
}
