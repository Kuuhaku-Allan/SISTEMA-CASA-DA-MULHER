namespace CasaMulher.Api.Services;

public class EquipeDbAutoSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EquipeDbAutoSyncHostedService> _logger;

    public EquipeDbAutoSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EquipeDbAutoSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(
            _configuration.GetValue("EquipeSync:IntervalSeconds", 60),
            30);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SincronizarAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task SincronizarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<EquipeDbSyncService>();
            var response = await syncService.SincronizarAsync(null, cancellationToken);

            _logger.LogInformation(
                "Sincronização automática EQP concluída: {Membros} membro(s), {Criados} usuário(s) criado(s), {Atualizados} atualizado(s).",
                response.MembrosImportados,
                response.UsuariosCriados,
                response.UsuariosAtualizados);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "A sincronização automática EQP não foi concluída. O sistema tentará novamente no próximo ciclo.");
        }
    }
}
