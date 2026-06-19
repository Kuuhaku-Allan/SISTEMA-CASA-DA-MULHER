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
    private readonly ILogger<HmlDbSnapshotService> _logger;

    public HmlDbSnapshotService(
        GitHubPrivateFileService github,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HmlDbStorageInfo storage,
        ILogger<HmlDbSnapshotService> logger)
    {
        _github = github;
        _configuration = configuration;
        _environment = environment;
        _storage = storage;
        _logger = logger;
    }

    public bool EnabledRequested => _configuration.GetValue("HML_DB_SNAPSHOT_ENABLED", false);
    public bool Configured => _environment.IsStaging()
        && _storage.IsSqlite
        && EnabledRequested
        && !string.IsNullOrWhiteSpace(_configuration["HML_DB_SNAPSHOT_KEY"])
        && _github.ReadConfigured
        && _github.WriteConfigured;
    public string SnapshotPath => _configuration["HML_DB_SNAPSHOT_PATH"]
        ?? "data/render-hml-db/latest.sqlite.gz.enc";
    public string ManifestPath => _configuration["HML_DB_SNAPSHOT_MANIFEST_PATH"]
        ?? "data/render-hml-db/manifest.json";

    public HmlDbSnapshotStatus GetStatus()
    {
        var message = Configured
            ? "Persistência de homologação ativa. Alterações de segurança serão preservadas pelo snapshot criptografado."
            : "Este ambiente usa banco temporário. Configurações de 2FA e passkeys podem ser perdidas após reinicializações.";
        return new(_environment.IsStaging(), EnabledRequested, Configured, _github.RepositoryLabel, SnapshotPath, message);
    }

    public long LoadedGeneration { get; private set; } = 0;

    public async Task<bool> TryRestoreAtStartupAsync(CancellationToken cancellationToken = default)
    {
        if (!Configured || HasValidLocalDatabase()) return false;
        
        var remoteManifestFile = await _github.ReadAsync(ManifestPath, cancellationToken);
        if (remoteManifestFile is null)
        {
            _logger.LogWarning("Manifesto de homologação não existe em {Path}; banco novo será criado.", ManifestPath);
            return false;
        }

        var manifest = JsonSerializer.Deserialize<HmlDbSnapshotManifest>(remoteManifestFile.Content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Current.File))
        {
            _logger.LogWarning("Manifesto inválido.");
            return false;
        }

        var remote = await _github.ReadAsync($"data/render-hml-db/{manifest.Current.File}", cancellationToken);
        if (remote is null)
        {
            _logger.LogWarning("Arquivo de snapshot {File} não encontrado.", manifest.Current.File);
            return false;
        }

        var encryptedHash = Convert.ToHexString(SHA256.HashData(remote.Content)).ToLowerInvariant();
        if (encryptedHash != manifest.Current.EncryptedHash)
        {
            _logger.LogError("Hash do snapshot criptografado não confere. Cancelando restore.");
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
                    _logger.LogError("Hash do SQLite descriptografado não confere. Cancelando restore.");
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
                
                LoadedGeneration = manifest.Current.Generation;
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

    public async Task CreateAndUploadAsync(CancellationToken cancellationToken = default)
    {
        if (!Configured) throw new InvalidOperationException(GetStatus().Message);
        if (!HasValidLocalDatabase()) throw new InvalidOperationException("Banco SQLite de homologação ainda não existe.");

        var remoteManifestFile = await _github.ReadAsync(ManifestPath, cancellationToken);
        HmlDbSnapshotManifest? remoteManifest = null;
        if (remoteManifestFile is not null)
        {
            remoteManifest = JsonSerializer.Deserialize<HmlDbSnapshotManifest>(remoteManifestFile.Content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        long remoteGen = remoteManifest?.Current.Generation ?? 0;
        if (remoteGen > LoadedGeneration)
        {
            throw new InvalidOperationException($"Conflito: o remoto está na geração {remoteGen}, mas o ambiente atual está baseado na {LoadedGeneration}. Faça pull/redeploy antes de enviar.");
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
                await _github.WriteAsync($"data/render-hml-db/{historyFile}", encrypted, $"Salva snapshot histórico de homologação (Gen {LoadedGeneration + 1})", cancellationToken);
                
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
                        Source = "render",
                        SourceMachine = Environment.MachineName,
                        AppCommit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "",
                        DatabaseHash = dbHash,
                        EncryptedHash = encHash,
                        Generation = LoadedGeneration + 1,
                        BaseGeneration = LoadedGeneration,
                        File = historyFile,
                        LatestFile = "latest.sqlite.gz.enc"
                    }
                };

                var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(newManifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
                await _github.WriteAsync(ManifestPath, manifestBytes, $"Atualiza manifesto para geração {newManifest.Current.Generation}", cancellationToken);
                
                LoadedGeneration = newManifest.Current.Generation;
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
