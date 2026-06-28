using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace CasaMulher.Api.Services;

public sealed record HmlDbStorageInfo(string DatabasePath, bool IsSqlite);

public sealed record HmlDbSnapshotStatus(
    bool Staging,
    bool EnabledRequested,
    bool Configured,
    string Repository,
    string SnapshotPath,
    string Message);

public sealed class HmlDbSnapshotService
{
    private static readonly byte[] SqliteHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");
    private readonly GitHubPrivateFileService _github;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly HmlDbStorageInfo _storage;
    private readonly HmlDbSnapshotState _state;
    private readonly ILogger<HmlDbSnapshotService> _logger;

    public HmlDbSnapshotService(
        GitHubPrivateFileService github,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HmlDbStorageInfo storage,
        HmlDbSnapshotState state,
        ILogger<HmlDbSnapshotService> logger)
    {
        _github = github;
        _configuration = configuration;
        _environment = environment;
        _storage = storage;
        _state = state;
        _logger = logger;
    }

    public bool EnabledRequested => _configuration.GetValue("HML_DB_SNAPSHOT_ENABLED", false);

    /// <summary>Restore exige apenas leitura. Não bloqueia por falta de token de escrita.</summary>
    public bool RestoreConfigured => _environment.IsStaging()
        && _storage.IsSqlite
        && EnabledRequested
        && !string.IsNullOrWhiteSpace(_configuration["HML_DB_SNAPSHOT_KEY"])
        && _github.ReadConfigured;

    /// <summary>Upload exige leitura + escrita (para criar/atualizar o arquivo no GitHub).</summary>
    public bool Configured => RestoreConfigured && _github.WriteConfigured;
    public string SnapshotPath => _configuration["HML_DB_SNAPSHOT_PATH"]
        ?? "data/render-hml-db/latest.sqlite.gz.enc";
    public string ManifestPath => _configuration["HML_DB_SNAPSHOT_MANIFEST_PATH"]
        ?? "data/render-hml-db/manifest.json";

    public HmlDbSnapshotStatus GetStatus()
    {
        var isEfemeral = _storage.DatabasePath.StartsWith("/tmp", StringComparison.OrdinalIgnoreCase) 
            || _storage.DatabasePath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);

        var persistenciaReal = isEfemeral && Configured ? "snapshot-github" : "disco-local";

        string message;
        if (persistenciaReal == "snapshot-github" && _state.UltimoSnapshotSucesso)
        {
            message = "Persistência de homologação ativa via snapshot criptografado no GitHub.";
        }
        else if (isEfemeral && (!Configured || !_state.UltimoSnapshotSucesso))
        {
            message = "Este ambiente usa banco temporário. Alterações de 2FA/passkeys podem ser perdidas se o Render reiniciar.";
        }
        else if (Configured)
        {
            message = "Persistência de homologação ativa. Alterações de segurança serão preservadas pelo snapshot criptografado.";
        }
        else
        {
            message = "Snapshot de segurança não está configurado.";
        }

        return new(_environment.IsStaging(), EnabledRequested, Configured, _github.RepositoryLabel, SnapshotPath, message);
    }

    public long LoadedGeneration => _state.LoadedGeneration;

    public async Task<HmlDbSnapshotDiagnostic> GetDiagnosticAsync(CancellationToken cancellationToken = default)
    {
        HmlDbSnapshotManifest? manifest = null;
        string? dbHash = null;

        try
        {
            if (RestoreConfigured)
            {
                var manifestFile = await _github.ReadAsync(ManifestPath, cancellationToken);
                if (manifestFile is not null) manifest = DeserializeManifest(manifestFile.Content);
            }

            if (HasValidLocalDatabase())
            {
                dbHash = await ComputeConsistentDatabaseHashAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _state.MarkError(ex.Message);
        }

        var isEfemeral = _storage.DatabasePath.StartsWith("/tmp", StringComparison.OrdinalIgnoreCase) 
            || _storage.DatabasePath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
        var persistenciaReal = isEfemeral && RestoreConfigured ? "snapshot-github" : "disco-local";

        var file = File.Exists(_storage.DatabasePath) ? new FileInfo(_storage.DatabasePath) : null;
        return new HmlDbSnapshotDiagnostic(
            Configured,
            Configured && _configuration.GetValue("HML_DB_SNAPSHOT_AUTO_ENABLED", true),
            _state.RestoreChamadoNoStartup,
            _state.RestoreConfigurado,
            _state.RestoreExecutado,
            _state.MotivoRestoreNaoExecutado,
            _storage.DatabasePath,
            file is not null,
            isEfemeral,
            persistenciaReal,
            file?.LastWriteTimeUtc,
            dbHash,
            manifest?.Current.Generation ?? 0,
            manifest?.Current.SnapshotId,
            manifest?.Current.DatabaseHash,
            _state.UltimoSnapshotSucesso,
            _state.LastSnapshotAt,
            _state.LastSnapshotSource,
            _state.LastError,
            _state.UltimoRestoreSucesso,
            _state.UltimoRestoreEm,
            _state.UltimoErroRestore,
            EnabledRequested,
            _configuration.GetValue("HML_DB_SNAPSHOT_AUTO_ENABLED", true),
            _environment.EnvironmentName);
    }

    public async Task<bool> TryRestoreAtStartupAsync(CancellationToken cancellationToken = default)
    {
        // Diagnóstico de configuração — sem expor valores dos tokens
        var keyPresente = !string.IsNullOrWhiteSpace(_configuration["HML_DB_SNAPSHOT_KEY"]);
        var readTokenPresente = _github.ReadConfigured;
        var writeTokenPresente = _github.WriteConfigured;
        _logger.LogInformation(
            "HML snapshot restore: chamado. HML_DB_SNAPSHOT_ENABLED={Enabled} KEY_PRESENTE={Key} READ_TOKEN_PRESENTE={Read} WRITE_TOKEN_PRESENTE={Write} DB_PATH={Path} MANIFEST_PATH={Manifest}",
            EnabledRequested, keyPresente, readTokenPresente, writeTokenPresente,
            _storage.DatabasePath, ManifestPath);

        if (!RestoreConfigured)
        {
            // Montar motivo detalhado para o diagnóstico
            var motivos = new List<string>();
            if (!_environment.IsStaging()) motivos.Add("ambiente não é Staging");
            if (!_storage.IsSqlite) motivos.Add("banco não é SQLite");
            if (!EnabledRequested) motivos.Add("HML_DB_SNAPSHOT_ENABLED não está true");
            if (!keyPresente) motivos.Add("HML_DB_SNAPSHOT_KEY ausente");
            if (!readTokenPresente) motivos.Add("GITHUB_EQP_READ_TOKEN e GITHUB_EQP_WRITE_TOKEN ausentes");
            var motivo = string.Join("; ", motivos);

            _state.MarkRestoreChamado(configurado: false, motivoNaoExecutado: motivo);
            _logger.LogWarning("HML snapshot restore: não configurado para restore. Motivo: {Motivo}", motivo);
            return false;
        }

        _state.MarkRestoreChamado(configurado: true);
        _logger.LogInformation("HML snapshot restore: lendo manifest em {Path}", ManifestPath);
        var remoteManifestFile = await _github.ReadAsync(ManifestPath, cancellationToken);
        if (remoteManifestFile is null)
        {
            _logger.LogWarning("HML snapshot restore: manifesto não existe em {Path}; banco novo será criado.", ManifestPath);
            return false;
        }

        var manifest = DeserializeManifest(remoteManifestFile.Content);
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Current.File))
        {
            _logger.LogWarning("HML snapshot restore: manifesto inválido ou arquivo ausente.");
            return false;
        }

        _logger.LogInformation(
            "HML snapshot restore: manifesto encontrado. Generation={Gen} Arquivo={File}",
            manifest.Current.Generation, manifest.Current.File);

        if (HasValidLocalDatabase())
        {
            _state.LoadedGeneration = manifest.Current.Generation;
            var localHash = await ComputeConsistentDatabaseHashAsync(cancellationToken);
            if (!string.Equals(localHash, manifest.Current.DatabaseHash, StringComparison.OrdinalIgnoreCase))
            {
                var message = $"Banco local preservado: o hash local difere do snapshot remoto da geração {manifest.Current.Generation}.";
                _state.MarkConflict(message);
                _logger.LogWarning("HML snapshot restore: {Message} Nenhum restore foi executado por cima do banco existente.", message);
            }
            else
            {
                _state.MarkSuccess(manifest.Current.CreatedAt, manifest.Current.Source);
                _logger.LogInformation("HML snapshot restore: banco local já está sincronizado com a generation {Gen}.", manifest.Current.Generation);
            }
            return false;
        }

        _logger.LogInformation("HML snapshot restore: restaurando arquivo {File} para {DbPath}", manifest.Current.File, _storage.DatabasePath);
        var remote = await _github.ReadAsync($"data/render-hml-db/{manifest.Current.File}", cancellationToken);
        if (remote is null)
        {
            var msg = $"HML snapshot restore: arquivo de snapshot {manifest.Current.File} não encontrado no repositório.";
            _state.MarkRestoreError(msg);
            _logger.LogError(msg);
            return false;
        }

        try
        {
            var encryptedHash = Convert.ToHexString(SHA256.HashData(remote.Content)).ToLowerInvariant();
            if (encryptedHash != manifest.Current.EncryptedHash)
            {
                var msg = "Hash do snapshot criptografado não confere. Cancelando restore.";
                _state.MarkRestoreError(msg);
                _logger.LogError(msg);
                return false;
            }

            var key = HmlDbSnapshotCrypto.ParseKey(_configuration["HML_DB_SNAPSHOT_KEY"]!);
            try
            {
                var database = HmlDbSnapshotCrypto.DecryptDecompressed(remote.Content, key);
                try
                {
                    var dbHash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant();
                    if (dbHash != manifest.Current.DatabaseHash)
                    {
                        var msg = "Hash do SQLite descriptografado não confere. Cancelando restore.";
                        _state.MarkRestoreError(msg);
                        _logger.LogError(msg);
                        return false;
                    }

                    if (!database.AsSpan().StartsWith(SqliteHeader))
                    {
                        throw new InvalidDataException("Snapshot descriptografado não é um banco SQLite.");
                    }

                    var directory = Path.GetDirectoryName(_storage.DatabasePath);
                    if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                    var tempPath = _storage.DatabasePath + ".restore";
                    await File.WriteAllBytesAsync(tempPath, database, cancellationToken);
                    File.Move(tempPath, _storage.DatabasePath, overwrite: true);
                    
                    _state.LoadedGeneration = manifest.Current.Generation;
                    _state.MarkRestoreSuccess(DateTimeOffset.UtcNow);
                    _state.MarkSuccess(manifest.Current.CreatedAt, "startup_restore");
                    _logger.LogInformation("Snapshot de homologação restaurado (Geração {Gen}).", LoadedGeneration);
                    return true;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(database);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
        catch (Exception ex)
        {
            _state.MarkRestoreError(ex.Message);
            _logger.LogError(ex, "Falha ao restaurar snapshot no startup.");
            return false;
        }
    }

    public async Task CreateAndUploadAsync(CancellationToken cancellationToken = default, string snapshotSource = "manual")
    {
        await _state.OperationLock.WaitAsync(cancellationToken);
        try
        {
            await CreateAndUploadCoreAsync(cancellationToken, snapshotSource);
        }
        finally
        {
            _state.OperationLock.Release();
        }
    }

    private async Task CreateAndUploadCoreAsync(CancellationToken cancellationToken, string snapshotSource)
    {
        if (!Configured) throw new InvalidOperationException(GetStatus().Message);
        if (!HasValidLocalDatabase()) throw new InvalidOperationException("Banco SQLite de homologação ainda não existe.");
        if (_state.HasConflict) throw new InvalidOperationException(_state.LastError ?? "Conflito de snapshot pendente; faça pull/redeploy antes de enviar.");

        var remoteManifestFile = await _github.ReadAsync(ManifestPath, cancellationToken);
        HmlDbSnapshotManifest? remoteManifest = null;
        if (remoteManifestFile is not null)
        {
            remoteManifest = DeserializeManifest(remoteManifestFile.Content);
        }

        long remoteGen = remoteManifest?.Current.Generation ?? 0;
        if (remoteGen > _state.LoadedGeneration)
        {
            var message = $"Conflito: o remoto está na geração {remoteGen}, mas o ambiente atual está baseado na {_state.LoadedGeneration}. Faça pull/redeploy antes de enviar.";
            _state.MarkConflict(message);
            throw new InvalidOperationException(message);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"casa-mulher-snapshot-{Guid.NewGuid():N}.db");
        try
        {
            await using (var source = new SqliteConnection($"Data Source={_storage.DatabasePath};Mode=ReadOnly"))
            await using (var destination = new SqliteConnection($"Data Source={tempPath}"))
            {
                await source.OpenAsync(cancellationToken);
                await destination.OpenAsync(cancellationToken);
                source.BackupDatabase(destination);
            }

            var database = await File.ReadAllBytesAsync(tempPath, cancellationToken);
            var dbHash = Convert.ToHexString(SHA256.HashData(database)).ToLowerInvariant();
            
            // Se o hash for igual ao remoto, não precisa fazer upload (ignora silenciosamente ou lança exceção)
            if (remoteManifest != null && remoteManifest.Current.DatabaseHash == dbHash)
            {
                _logger.LogInformation("Banco de dados não foi modificado. Upload ignorado.");
                _state.LoadedGeneration = remoteManifest.Current.Generation;
                _state.MarkSuccess(remoteManifest.Current.CreatedAt, remoteManifest.Current.Source);
                return;
            }

            var key = HmlDbSnapshotCrypto.ParseKey(_configuration["HML_DB_SNAPSHOT_KEY"]!);
            try
            {
                var encrypted = HmlDbSnapshotCrypto.EncryptCompressed(database, key);
                var encHash = Convert.ToHexString(SHA256.HashData(encrypted)).ToLowerInvariant();
                var snapshotId = Guid.NewGuid().ToString("N");
                var historyFile = $"history/{snapshotId}.sqlite.gz.enc";

                // 1. Upload do history
                await _github.WriteAsync($"data/render-hml-db/{historyFile}", encrypted, $"Salva snapshot histórico de homologação (Gen {_state.LoadedGeneration + 1})", cancellationToken);
                
                // 2. Upload do latest
                await _github.WriteAsync(SnapshotPath, encrypted, $"Atualiza latest.sqlite.gz.enc", cancellationToken);

                // 3. Atualiza Manifest
                var newManifest = new HmlDbSnapshotManifest
                {
                    SchemaVersion = 1,
                    Current = new HmlDbSnapshotManifestCurrent
                    {
                        SnapshotId = snapshotId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        Source = snapshotSource,
                        SourceMachine = Environment.MachineName,
                        AppCommit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "",
                        DatabaseHash = dbHash,
                        EncryptedHash = encHash,
                        Generation = _state.LoadedGeneration + 1,
                        BaseGeneration = _state.LoadedGeneration,
                        File = historyFile,
                        LatestFile = "latest.sqlite.gz.enc"
                    }
                };

                var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(newManifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
                await _github.WriteAsync(ManifestPath, manifestBytes, $"Atualiza manifesto para geração {newManifest.Current.Generation}", cancellationToken);
                
                _state.LoadedGeneration = newManifest.Current.Generation;
                _state.MarkSuccess(newManifest.Current.CreatedAt, snapshotSource);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(database);
                CryptographicOperations.ZeroMemory(key);
            }
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private bool HasValidLocalDatabase()
    {
        if (!File.Exists(_storage.DatabasePath)) return false;
        using var stream = File.OpenRead(_storage.DatabasePath);
        if (stream.Length < SqliteHeader.Length) return false;
        Span<byte> header = stackalloc byte[SqliteHeader.Length];
        return stream.Read(header) == header.Length && header.SequenceEqual(SqliteHeader);
    }

    private static HmlDbSnapshotManifest? DeserializeManifest(byte[] content) =>
        JsonSerializer.Deserialize<HmlDbSnapshotManifest>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

    private async Task<string> ComputeConsistentDatabaseHashAsync(CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"casa-mulher-hash-{Guid.NewGuid():N}.db");
        try
        {
            await using var source = new SqliteConnection($"Data Source={_storage.DatabasePath};Mode=ReadOnly");
            await using var destination = new SqliteConnection($"Data Source={tempPath}");
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);
            var bytes = await File.ReadAllBytesAsync(tempPath, cancellationToken);
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}

public sealed class HmlDbSnapshotManifest
{
    public int SchemaVersion { get; set; } = 1;
    public HmlDbSnapshotManifestCurrent Current { get; set; } = new();
}

public sealed class HmlDbSnapshotManifestCurrent
{
    public string SnapshotId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceMachine { get; set; } = string.Empty;
    public string AppCommit { get; set; } = string.Empty;
    public string DatabaseHash { get; set; } = string.Empty;
    public string EncryptedHash { get; set; } = string.Empty;
    public long Generation { get; set; }
    public long BaseGeneration { get; set; }
    public string File { get; set; } = string.Empty;
    public string LatestFile { get; set; } = string.Empty;
}
