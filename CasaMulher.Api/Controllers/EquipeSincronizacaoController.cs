using System.Text.Json;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/equipe")]
public class EquipeSincronizacaoController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly EquipeDbSyncService _syncService;
    private readonly IWebHostEnvironment _environment;

    public EquipeSincronizacaoController(
        EquipeDbSyncService syncService,
        IWebHostEnvironment environment)
    {
        _syncService = syncService;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost("sincronizar-github-db")]
    public async Task<ActionResult<SincronizarEquipeDbResponse>> SincronizarGithubDb(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return NotFound(new { mensagem = "Sincronização EQP disponível apenas em Development/Staging." });
        }

        var document = await LerDocumentoOpcionalAsync(cancellationToken);
        var response = await _syncService.SincronizarAsync(document, cancellationToken);
        return Ok(response);
    }

    private async Task<EquipeDbDocument?> LerDocumentoOpcionalAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentLength is null or 0)
        {
            return null;
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("equipeDb", out var wrapped)
            && wrapped.ValueKind != JsonValueKind.Null)
        {
            return wrapped.Deserialize<EquipeDbDocument>(JsonOptions);
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("schemaVersion", out _))
        {
            return root.Deserialize<EquipeDbDocument>(JsonOptions);
        }

        return null;
    }
}
