using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CasaMulher.Api.Services;

public class EquipeDbGitHubService : IEquipeDbGitHubService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EquipeDbGitHubService> _logger;

    public EquipeDbGitHubService(
        HttpClient httpClient,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<EquipeDbGitHubService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public bool LeituraConfigurada =>
        !string.IsNullOrWhiteSpace(ObterTokenLeitura()) || _environment.IsDevelopment();

    public bool EscritaConfigurada => !string.IsNullOrWhiteSpace(ObterTokenEscrita());

    public string RepositoryLabel => $"{RepoOwner}/{RepoName}";

    public string DbPath => _configuration["GitHub:EqpDbPath"]
        ?? _configuration["GITHUB_EQP_DB_PATH"]
        ?? "data/equipe-db.json";

    public string EventsPath => _configuration["GitHub:EqpEventsPath"]
        ?? _configuration["GITHUB_EQP_EVENTS_PATH"]
        ?? "data/equipe-events.ndjson";

    public string AccessRequestsPath => _configuration["GitHub:EqpAccessRequestsPath"]
        ?? _configuration["GITHUB_EQP_ACCESS_REQUESTS_PATH"]
        ?? "data/access-requests.json";

    private string RepoOwner => _configuration["GitHub:EqpDbRepoOwner"]
        ?? _configuration["GITHUB_EQP_DB_REPO_OWNER"]
        ?? "Sistema-Casa-da-Mulher";

    private string RepoName => _configuration["GitHub:EqpDbRepo"]
        ?? _configuration["GITHUB_EQP_DB_REPO"]
        ?? "ACESSO-EQUIPE";

    public async Task<EquipeDbFile> LerAsync(CancellationToken cancellationToken = default)
    {
        var arquivo = await LerArquivoTextoAsync(DbPath, ObterTokenLeitura(), cancellationToken);

        if (arquivo is null)
        {
            return new EquipeDbFile
            {
                Document = CriarDocumentoInicial(),
                Exists = false
            };
        }

        var document = JsonSerializer.Deserialize<EquipeDbDocument>(arquivo.Value.Content, JsonOptions)
            ?? CriarDocumentoInicial();

        NormalizarDocumento(document);

        return new EquipeDbFile
        {
            Document = document,
            Sha = arquivo.Value.Sha,
            Exists = true
        };
    }

    public async Task SalvarAsync(
        EquipeDbDocument document,
        string? sha,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (!EscritaConfigurada)
        {
            throw new EquipeDbGitHubException(403, "GITHUB_EQP_WRITE_TOKEN não configurado.");
        }

        NormalizarDocumento(document);
        document.UpdatedAt = DateTime.UtcNow;

        var content = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
        await SalvarArquivoTextoAsync(DbPath, content, sha, commitMessage, ObterTokenEscrita(), cancellationToken);
    }

    public async Task AcrescentarEventoAsync(
        EquipeDbEvent evento,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (!EscritaConfigurada)
        {
            throw new EquipeDbGitHubException(403, "GITHUB_EQP_WRITE_TOKEN não configurado.");
        }

        var arquivoAtual = await LerArquivoTextoAsync(EventsPath, ObterTokenEscrita(), cancellationToken);
        var linha = JsonSerializer.Serialize(evento, JsonOptions).ReplaceLineEndings(string.Empty);
        var conteudo = (arquivoAtual?.Content ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(conteudo) && !conteudo.EndsWith('\n'))
        {
            conteudo += Environment.NewLine;
        }

        conteudo += linha + Environment.NewLine;
        await SalvarArquivoTextoAsync(EventsPath, conteudo, arquivoAtual?.Sha, commitMessage, ObterTokenEscrita(), cancellationToken);
    }

    public async Task<EquipeAccessRequestsFile> LerSolicitacoesAcessoAsync(CancellationToken cancellationToken = default)
    {
        var arquivo = await LerArquivoTextoAsync(AccessRequestsPath, ObterTokenLeitura(), cancellationToken);
        if (arquivo is null) return new EquipeAccessRequestsFile { Exists = false };

        var document = JsonSerializer.Deserialize<EquipeAccessRequestsDocument>(arquivo.Value.Content, JsonOptions)
            ?? new EquipeAccessRequestsDocument();
        document.Requests ??= [];
        return new EquipeAccessRequestsFile { Document = document, Sha = arquivo.Value.Sha, Exists = true };
    }

    public async Task SalvarSolicitacoesAcessoAsync(
        EquipeAccessRequestsDocument document,
        string? sha,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (!EscritaConfigurada)
        {
            throw new EquipeDbGitHubException(403, "GITHUB_EQP_WRITE_TOKEN não configurado.");
        }

        document.Requests ??= [];
        var content = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
        await SalvarArquivoTextoAsync(AccessRequestsPath, content, sha, commitMessage, ObterTokenEscrita(), cancellationToken);
    }

    private async Task<(string Content, string Sha)?> LerArquivoTextoAsync(
        string path,
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            if (_environment.IsDevelopment())
            {
                return await LerArquivoTextoViaGhAsync(path, cancellationToken);
            }

            throw new EquipeDbGitHubException(403, "Token GitHub não configurado para leitura.");
        }

        using var request = CriarRequest(HttpMethod.Get, ConteudoUrl(path), token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new EquipeDbGitHubException(
                (int)response.StatusCode,
                $"GitHub retornou {(int)response.StatusCode} ao ler {path}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GitHubContentResponse>(
            JsonOptions,
            cancellationToken);

        if (payload is null || !string.Equals(payload.Type, "file", StringComparison.OrdinalIgnoreCase))
        {
            throw new EquipeDbGitHubException(422, $"Conteúdo GitHub inválido para {path}.");
        }

        var base64 = payload.Content.Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal);
        var bytes = Convert.FromBase64String(base64);
        return (Encoding.UTF8.GetString(bytes), payload.Sha);
    }

    private async Task SalvarArquivoTextoAsync(
        string path,
        string content,
        string? sha,
        string commitMessage,
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new EquipeDbGitHubException(403, "Token GitHub não configurado para escrita.");
        }

        var body = new Dictionary<string, object?>
        {
            ["message"] = commitMessage,
            ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(content))
        };

        if (!string.IsNullOrWhiteSpace(sha))
        {
            body["sha"] = sha;
        }

        using var request = CriarRequest(HttpMethod.Put, ConteudoUrl(path), token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new EquipeDbGitHubException(409, $"Conflito de SHA ao atualizar {path}.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Falha GitHub ao atualizar {Path}: {StatusCode} {Body}", path, response.StatusCode, responseBody);
            throw new EquipeDbGitHubException(
                (int)response.StatusCode,
                $"GitHub retornou {(int)response.StatusCode} ao atualizar {path}.");
        }
    }

    private HttpRequestMessage CriarRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("CasaMulherPortalEqp/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private async Task<(string Content, string Sha)?> LerArquivoTextoViaGhAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "gh",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("api");
        startInfo.ArgumentList.Add($"repos/{RepoOwner}/{RepoName}/contents/{path}");

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Não foi possível iniciar o gh CLI.");
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                if (error.Contains("HTTP 404", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                throw new EquipeDbGitHubException(
                    503,
                    $"O gh CLI não conseguiu ler {path}. Confirme o acesso ao repositório privado.");
            }

            var payload = JsonSerializer.Deserialize<GitHubContentResponse>(output, JsonOptions);

            if (payload is null || !string.Equals(payload.Type, "file", StringComparison.OrdinalIgnoreCase))
            {
                throw new EquipeDbGitHubException(422, $"Conteúdo GitHub inválido para {path}.");
            }

            var base64 = payload.Content.Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal);
            var bytes = Convert.FromBase64String(base64);
            return (Encoding.UTF8.GetString(bytes), payload.Sha);
        }
        catch (EquipeDbGitHubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível ler {Path} pelo gh CLI.", path);
            throw new EquipeDbGitHubException(
                503,
                "O gh CLI não está disponível ou autenticado. Execute gh auth login e tente novamente.");
        }
    }

    private string ConteudoUrl(string path)
    {
        var escapedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        return $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contents/{escapedPath}";
    }

    private string? ObterTokenLeitura()
    {
        return _configuration["GitHub:EqpReadToken"]
            ?? _configuration["GITHUB_EQP_READ_TOKEN"]
            ?? ObterTokenEscrita();
    }

    private string? ObterTokenEscrita()
    {
        return _configuration["GitHub:EqpWriteToken"]
            ?? _configuration["GITHUB_EQP_WRITE_TOKEN"];
    }

    public static EquipeDbDocument CriarDocumentoInicial()
    {
        var agora = DateTime.UtcNow;

        return new EquipeDbDocument
        {
            SchemaVersion = 1,
            UpdatedAt = agora,
            AllowlistGitHub = ["Kuuhaku-Allan"],
            Settings = new EquipeDbSettings(),
            Convites =
            [
                new()
                {
                    EqpId = "EQP-000001",
                    AdmId = "ADM-000003",
                    Status = EquipeDbStatusConvite.Reservado,
                    ReservadoParaGitHub = "Kuuhaku-Allan",
                    PapelEquipe = "owner",
                    FluxoTrabalho = "local_owner",
                    CriadoEm = agora
                },
                new() { EqpId = "EQP-000002", AdmId = "ADM-000004", CriadoEm = agora },
                new() { EqpId = "EQP-000003", AdmId = "ADM-000005", CriadoEm = agora },
                new() { EqpId = "EQP-000004", AdmId = "ADM-000006", CriadoEm = agora },
                new() { EqpId = "EQP-000005", AdmId = "ADM-000007", CriadoEm = agora }
            ]
        };
    }

    public static void NormalizarDocumento(EquipeDbDocument document)
    {
        document.Settings ??= new EquipeDbSettings();
        document.AllowlistGitHub ??= [];
        document.Convites ??= [];
        document.Membros ??= [];
    }
}
