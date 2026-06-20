namespace CasaMulher.Api.Services;

public sealed class HmlDbSnapshotState
{
    private readonly object _sync = new();
    private long _loadedGeneration;
    private DateTimeOffset? _lastSnapshotAt;
    private string? _lastSnapshotSource;
    private string? _lastError;
    private bool _hasConflict;

    private bool _ultimoSnapshotSucesso;
    private bool _ultimoRestoreSucesso;
    private DateTimeOffset? _ultimoRestoreEm;
    private string? _ultimoErroRestore;

    // Rastreamento granular do restore no startup
    private bool _restoreChamadoNoStartup;
    private bool _restoreConfigurado;
    private bool _restoreExecutado;
    private string? _motivoRestoreNaoExecutado;

    public SemaphoreSlim OperationLock { get; } = new(1, 1);

    public long LoadedGeneration
    {
        get { lock (_sync) return _loadedGeneration; }
        set { lock (_sync) _loadedGeneration = value; }
    }

    public DateTimeOffset? LastSnapshotAt
    {
        get { lock (_sync) return _lastSnapshotAt; }
    }

    public string? LastSnapshotSource
    {
        get { lock (_sync) return _lastSnapshotSource; }
    }

    public string? LastError
    {
        get { lock (_sync) return _lastError; }
    }

    public bool HasConflict
    {
        get { lock (_sync) return _hasConflict; }
    }

    public bool UltimoSnapshotSucesso => _ultimoSnapshotSucesso;
    public bool UltimoRestoreSucesso => _ultimoRestoreSucesso;
    public DateTimeOffset? UltimoRestoreEm => _ultimoRestoreEm;
    public string? UltimoErroRestore => _ultimoErroRestore;

    public bool RestoreChamadoNoStartup => _restoreChamadoNoStartup;
    public bool RestoreConfigurado => _restoreConfigurado;
    public bool RestoreExecutado => _restoreExecutado;
    public string? MotivoRestoreNaoExecutado => _motivoRestoreNaoExecutado;

    public void MarkSuccess(DateTimeOffset at, string source)
    {
        lock (_sync)
        {
            _lastSnapshotAt = at;
            _lastSnapshotSource = source;
            _lastError = null;
            _hasConflict = false;
            _ultimoSnapshotSucesso = true;
        }
    }

    public void MarkError(string message)
    {
        lock (_sync)
        {
            _lastError = message;
            _ultimoSnapshotSucesso = false;
        }
    }

    public void MarkConflict(string message)
    {
        lock (_sync)
        {
            _hasConflict = true;
            _lastError = message;
            _ultimoSnapshotSucesso = false;
        }
    }

    public void MarkRestoreSuccess(DateTimeOffset at)
    {
        lock (_sync)
        {
            _ultimoRestoreEm = at;
            _ultimoRestoreSucesso = true;
            _ultimoErroRestore = null;
            _restoreExecutado = true;
        }
    }

    public void MarkRestoreError(string message)
    {
        lock (_sync)
        {
            _ultimoErroRestore = message;
            _ultimoRestoreSucesso = false;
            _restoreExecutado = true;
        }
    }

    public void MarkRestoreChamado(bool configurado, string? motivoNaoExecutado = null)
    {
        lock (_sync)
        {
            _restoreChamadoNoStartup = true;
            _restoreConfigurado = configurado;
            if (!configurado) _motivoRestoreNaoExecutado = motivoNaoExecutado;
        }
    }
}

public sealed record HmlDbSnapshotDiagnostic(
    bool SnapshotAtivo,
    bool AutoSnapshotAtivo,
    bool RestoreChamadoNoStartup,
    bool RestoreConfigurado,
    bool RestoreExecutado,
    string? MotivoRestoreNaoExecutado,
    string DbPathAtual,
    bool DbExiste,
    bool DbEfemero,
    string PersistenciaReal,
    DateTime? DbLastWriteUtc,
    string? DbHashAtual,
    long ManifestGeneration,
    string? ManifestSnapshotId,
    string? ManifestDatabaseHash,
    bool UltimoSnapshotSucesso,
    DateTimeOffset? UltimoSnapshotEm,
    string? UltimoSnapshotSource,
    string? UltimoErroSnapshot,
    bool UltimoRestoreSucesso,
    DateTimeOffset? UltimoRestoreEm,
    string? UltimoErroRestore,
    bool HmlDbSnapshotEnabled,
    bool HmlDbSnapshotAutoEnabled,
    string Ambiente);
