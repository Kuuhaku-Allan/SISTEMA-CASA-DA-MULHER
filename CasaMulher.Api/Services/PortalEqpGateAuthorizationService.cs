using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CasaMulher.Api.Services;

public sealed record PortalEqpGateAuthorizationResult(bool Autorizado, string Motivo);

public sealed class PortalEqpGateAuthorizationService
{
    private readonly IEquipeDbGitHubService _equipeDbService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PortalEqpGateAuthorizationService> _logger;

    public PortalEqpGateAuthorizationService(
        IEquipeDbGitHubService equipeDbService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<PortalEqpGateAuthorizationService> logger)
    {
        _equipeDbService = equipeDbService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PortalEqpGateAuthorizationResult> AutorizarAsync(
        GitHubPortalSession session,
        CancellationToken cancellationToken)
    {
        var configuredOwner = ObterConfig("GitHub:OwnerLogin", "GitHub:OwnerUsername", "GITHUB_OWNER_LOGIN");

        if (Igual(session.GitHubUsername, configuredOwner))
        {
            return Autorizar(session, "owner");
        }

        if (ObterAllowlistAmbiente().Any(item => Igual(item, session.GitHubUsername)))
        {
            return Autorizar(session, "allowlist_ambiente");
        }

        EquipeDbDocument? document = null;

        try
        {
            document = (await _equipeDbService.LerAsync(cancellationToken)).Document;
            EquipeDbGitHubService.NormalizarDocumento(document);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub Gate não conseguiu ler a base privada ao autorizar {GitHub}.", session.GitHubUsername);
        }

        if (document is not null && Igual(session.GitHubUsername, document.Settings.OwnerGitHub))
        {
            return Autorizar(session, "owner");
        }

        if (document is not null
            && document.AllowlistGitHub.Any(item => Igual(item, session.GitHubUsername)))
        {
            return Autorizar(session, "allowlist");
        }

        if (document is not null
            && document.Membros.Any(membro =>
                Igual(membro.Status, "ativo")
                && (Igual(membro.GitHubId, session.GitHubId) || Igual(membro.GitHubUsername, session.GitHubUsername))))
        {
            return Autorizar(session, "membro_ativo");
        }

        var org = ObterConfig("GitHub:Org", "GitHub:Organization", "GITHUB_ORG")
            ?? document?.Settings.Org;

        if (await VerificarOrganizacaoAsync(session, org, cancellationToken))
        {
            return Autorizar(session, "organizacao");
        }

        _logger.LogWarning("GitHub Gate negou acesso para {GitHub}.", session.GitHubUsername);
        return new(false, "nao_autorizado");
    }

    private PortalEqpGateAuthorizationResult Autorizar(GitHubPortalSession session, string motivo)
    {
        _logger.LogInformation("GitHub Gate autorizou {GitHub} por {Motivo}.", session.GitHubUsername, motivo);
        return new(true, motivo);
    }

    private async Task<bool> VerificarOrganizacaoAsync(
        GitHubPortalSession session,
        string? org,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session.AccessToken) || string.IsNullOrWhiteSpace(org))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var membershipUrl = $"https://api.github.com/user/memberships/orgs/{Uri.EscapeDataString(org)}";
            using var request = CriarRequest(membershipUrl, session.AccessToken);
            using var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var membership = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
                return membership.RootElement.TryGetProperty("state", out var state)
                    && string.Equals(state.GetString(), "active", StringComparison.OrdinalIgnoreCase);
            }

            if (response.StatusCode is not HttpStatusCode.NotFound and not HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("GitHub Gate recebeu {Status} ao verificar a organização.", response.StatusCode);
            }

            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "GitHub Gate não conseguiu verificar a organização; aplicou fallback fechado.");
            return false;
        }
    }

    private static HttpRequestMessage CriarRequest(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("CasaMulherRenderGate/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private IEnumerable<string> ObterAllowlistAmbiente()
    {
        var value = ObterConfig("GitHub:EqpAllowlist", "GITHUB_EQP_ALLOWLIST");
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private string? ObterConfig(params string[] keys)
    {
        return keys.Select(key => _configuration[key]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static bool Igual(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}
