using System.Net.Http.Headers;
using System.Text.Json;
using CasaMulher.Api.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CasaMulher.Api.Services;

public class EquipeGithubService : IEquipeGithubService
{
    private const string CacheKey = "equipe-github-atividade";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public EquipeGithubService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
    }

    public EquipeGithubStatusResponse ObterStatus()
    {
        var (owner, repo, ownerUsername) = ObterConfiguracaoRepositorio();
        var oauthConfigurado = !string.IsNullOrWhiteSpace(_configuration["GitHub:ClientId"])
            && !string.IsNullOrWhiteSpace(_configuration["GitHub:ClientSecret"]);

        return new EquipeGithubStatusResponse
        {
            OAuthConfigurado = oauthConfigurado,
            Organization = owner,
            Repository = repo,
            OwnerUsername = ownerUsername,
            Mensagem = oauthConfigurado
                ? "OAuth GitHub configurado para ambiente atual."
                : "OAuth GitHub não configurado. O login por ID e senha continua disponível."
        };
    }

    public async Task<EquipeGithubAtividadeResponse> ObterAtividadeAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out EquipeGithubAtividadeResponse? cached) && cached is not null)
        {
            return cached;
        }

        var (owner, repo, _) = ObterConfiguracaoRepositorio();
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/pulls?state=all&per_page=20&sort=updated&direction=desc");

        request.Headers.UserAgent.ParseAdd("CasaMulherEquipe/1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var token = _configuration["GitHub:ReadToken"]
            ?? Environment.GetEnvironmentVariable("GITHUB_READ_TOKEN");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return CriarFallback($"GitHub retornou {(int)response.StatusCode}. Tente novamente mais tarde ou configure um token de leitura.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var pulls = await JsonSerializer.DeserializeAsync<List<GithubPullResponse>>(stream, JsonOptions, cancellationToken)
                ?? [];

            var result = new EquipeGithubAtividadeResponse
            {
                Disponivel = true,
                Mensagem = "Atividade carregada pela API publica do GitHub.",
                AtualizadoEm = DateTime.UtcNow,
                PullRequests = pulls.Select(MapearPullRequest).ToList()
            };

            _cache.Set(CacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch
        {
            return CriarFallback("Não foi possível consultar o GitHub agora. O painel continua disponível com login por ID e senha.");
        }
    }

    private (string Owner, string Repo, string OwnerUsername) ObterConfiguracaoRepositorio()
    {
        return (
            _configuration["GitHub:Organization"] ?? "Sistema-Casa-da-Mulher",
            _configuration["GitHub:Repository"] ?? "SISTEMA-CASA-DA-MULHER",
            _configuration["GitHub:OwnerUsername"] ?? "Kuuhaku-Allan"
        );
    }

    private static EquipeGithubAtividadeResponse CriarFallback(string mensagem)
    {
        return new EquipeGithubAtividadeResponse
        {
            Disponivel = false,
            Mensagem = mensagem,
            AtualizadoEm = DateTime.UtcNow,
            PullRequests = []
        };
    }

    private static EquipeGithubPullRequestResponse MapearPullRequest(GithubPullResponse pull)
    {
        return new EquipeGithubPullRequestResponse
        {
            Numero = pull.Number,
            Titulo = pull.Title ?? string.Empty,
            Estado = pull.MergedAt is not null ? "merged" : pull.State ?? string.Empty,
            Autor = pull.User?.Login ?? string.Empty,
            Branch = pull.Head?.Ref ?? string.Empty,
            VeioDeFork = !string.Equals(pull.Head?.Repo?.FullName, pull.Base?.Repo?.FullName, StringComparison.OrdinalIgnoreCase),
            CriadoEm = pull.CreatedAt,
            FechadoEm = pull.ClosedAt,
            MergeadoEm = pull.MergedAt,
            Url = pull.HtmlUrl ?? string.Empty
        };
    }

    private sealed class GithubPullResponse
    {
        public int Number { get; set; }
        public string? Title { get; set; }
        public string? State { get; set; }
        public GithubUser? User { get; set; }
        public GithubBranch? Head { get; set; }
        public GithubBranch? Base { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? MergedAt { get; set; }
        public string? HtmlUrl { get; set; }
    }

    private sealed class GithubUser
    {
        public string? Login { get; set; }
    }

    private sealed class GithubBranch
    {
        public string? Ref { get; set; }
        public GithubRepo? Repo { get; set; }
    }

    private sealed class GithubRepo
    {
        public string? FullName { get; set; }
    }
}
