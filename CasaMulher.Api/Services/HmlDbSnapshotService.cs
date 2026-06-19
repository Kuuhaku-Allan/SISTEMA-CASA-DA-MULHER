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
            ? "Persistência manual de homologação configurada. Gere um snapshot após alterações importantes."
            : "Banco temporário: 2FA e passkeys podem ser perdidos em reinicializações até o snapshot ser configurado.";
        return new(_environment.IsStaging(), EnabledRequested, Configured, _github.RepositoryLabel, SnapshotPath, message);
    }

    public async Task<bool> TryRestoreAtStartupAsync(CancellationToken cancellationToken = default)
    {
        if (!Configured || HasValidLocalDatabase()) return false;
        var remote = await _github.ReadAsync(SnapshotPath, cancellationToken);
        if (remote is null)
        {
            _logger.LogWarning("Snapshot de homologação não existe em {Path}; banco novo será criado.", SnapshotPath);
            return false;
        }

        var key = HmlDbSnapshotCrypto.ParseKey(_configuration["HML_DB_SNAPSHOT_KEY"]!);
        try
        {
            var database = HmlDbSnapshotCrypto.DecryptDecompressed(remote.Content, key);
            try
            {
                if (!database.AsSpan().StartsWith(SqliteHeader))
                {
                    throw new InvalidDataException("Snapshot descriptografado não é um banco SQLite.");
                }

                var directory = Path.GetDirectoryName(_storage.DatabasePath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                var tempPath = _storage.DatabasePath + ".restore";
                await File.WriteAllBytesAsync(tempPath, database, cancellationToken);
                File.Move(tempPath, _storage.DatabasePath, overwrite: true);
                _logger.LogInformation("Snapshot de homologação restaurado de {Path}.", SnapshotPath);
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
            var key = HmlDbSnapshotCrypto.ParseKey(_configuration["HML_DB_SNAPSHOT_KEY"]!);
            try
            {
                var encrypted = HmlDbSnapshotCrypto.EncryptCompressed(database, key);
                await _github.WriteAsync(SnapshotPath, encrypted, "Atualiza snapshot de homologação Render", cancellationToken);
                var manifest = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    schemaVersion = 1,
                    createdAt = DateTimeOffset.UtcNow,
                    encryptedSha256 = Convert.ToHexString(SHA256.HashData(encrypted)).ToLowerInvariant(),
                    databaseBytes = database.Length,
                    encryption = "AES-256-GCM",
                    compression = "gzip"
                }, new JsonSerializerOptions { WriteIndented = true });
                await _github.WriteAsync(ManifestPath, manifest, "Atualiza manifesto do snapshot de homologação", cancellationToken);
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
