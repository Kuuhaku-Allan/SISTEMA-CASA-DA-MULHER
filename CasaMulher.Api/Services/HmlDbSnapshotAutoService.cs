using CasaMulher.Api.Services;

namespace CasaMulher.Api.Services;

public sealed class HmlDbSnapshotAutoService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HmlDbSnapshotAutoService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HmlDbStorageInfo _storage;

    public HmlDbSnapshotAutoService(
        IServiceProvider serviceProvider,
        ILogger<HmlDbSnapshotAutoService> logger,
        IConfiguration configuration,
        HmlDbStorageInfo storage)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _storage = storage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue("HML_DB_SNAPSHOT_INTERVAL_MINUTES", 10);
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        
        DateTime lastWriteTime = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var snapshotService = scope.ServiceProvider.GetRequiredService<HmlDbSnapshotService>();
                
                if (!snapshotService.Configured) continue;
                if (!File.Exists(_storage.DatabasePath)) continue;

                var currentWriteTime = new FileInfo(_storage.DatabasePath).LastWriteTimeUtc;
                
                // Primeira execução: apenas registra o estado atual
                if (lastWriteTime == DateTime.MinValue)
                {
                    lastWriteTime = currentWriteTime;
                    continue;
                }

                // Se o arquivo SQLite foi modificado
                if (currentWriteTime > lastWriteTime)
                {
                    _logger.LogInformation("Mudança detectada no banco SQLite. Iniciando auto-snapshot.");
                    await snapshotService.CreateAndUploadAsync(stoppingToken);
                    lastWriteTime = currentWriteTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha na execução do auto-snapshot (pode ser conflito de geração ou erro de rede).");
            }
        }
    }
}
