namespace CasaMulher.Api.Services;

public sealed record SecuritySnapshotPersistenceResult(
    bool SnapshotNecessario,
    bool SnapshotPersistido,
    string? AvisoSnapshot);

public sealed class SecuritySnapshotPersistenceService
{
    public const string FailureWarning = "A alteração foi aplicada no banco atual, mas ainda não foi persistida no snapshot. Gere snapshot antes de reiniciar o Render.";

    private readonly HmlDbSnapshotService _snapshot;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SecuritySnapshotPersistenceService> _logger;

    public SecuritySnapshotPersistenceService(
        HmlDbSnapshotService snapshot,
        IWebHostEnvironment environment,
        ILogger<SecuritySnapshotPersistenceService> logger)
    {
        _snapshot = snapshot;
        _environment = environment;
        _logger = logger;
    }

    public async Task<SecuritySnapshotPersistenceResult> PersistAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsStaging()) return new(false, false, null);
        if (!_snapshot.Configured) return new(true, false, FailureWarning);

        try
        {
            await _snapshot.CreateAndUploadAsync(cancellationToken, source);
            return new(true, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alteração crítica {Source} foi salva no SQLite, mas o snapshot falhou.", source);
            return new(true, false, FailureWarning);
        }
    }
}
