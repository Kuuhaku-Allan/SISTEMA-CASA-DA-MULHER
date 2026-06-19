using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CasaMulher.Api.Services;

public sealed record GitHubPrivateFile(byte[] Content, string Sha);

public sealed class GitHubPrivateFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GitHubPrivateFileService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public bool ReadConfigured => !string.IsNullOrWhiteSpace(ReadToken);
    public bool WriteConfigured => !string.IsNullOrWhiteSpace(WriteToken);
    public string RepositoryLabel => $"{RepoOwner}/{RepoName}";

    public async Task<GitHubPrivateFile?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var token = ReadToken;
        if (string.IsNullOrWhiteSpace(token)) return null;

        using var request = CreateRequest(HttpMethod.Get, ContentUrl(path), token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GitHubContentResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Resposta GitHub inválida para {path}.");
        var base64 = payload.Content.Replace("\n", string.Empty).Replace("\r", string.Empty);
        return new GitHubPrivateFile(Convert.FromBase64String(base64), payload.Sha);
    }

    public async Task WriteAsync(string path, byte[] content, string commitMessage, CancellationToken cancellationToken = default)
    {
        var token = WriteToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("GITHUB_EQP_WRITE_TOKEN não configurado para snapshot.");
        }

        var current = await ReadAsync(path, cancellationToken);
        var body = new Dictionary<string, object?>
        {
            ["message"] = commitMessage,
            ["content"] = Convert.ToBase64String(content)
        };
        if (!string.IsNullOrWhiteSpace(current?.Sha)) body["sha"] = current.Sha;

        using var request = CreateRequest(HttpMethod.Put, ContentUrl(path), token);
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private string RepoOwner => _configuration["HML_DB_SNAPSHOT_REPO_OWNER"]
        ?? _configuration["GitHub:EqpDbRepoOwner"]
        ?? _configuration["GITHUB_EQP_DB_REPO_OWNER"]
        ?? "Sistema-Casa-da-Mulher";
    private string RepoName => _configuration["HML_DB_SNAPSHOT_REPO"]
        ?? _configuration["GitHub:EqpDbRepo"]
        ?? _configuration["GITHUB_EQP_DB_REPO"]
        ?? "ACESSO-EQUIPE";
    private string? ReadToken => _configuration["GITHUB_EQP_READ_TOKEN"] ?? WriteToken;
    private string? WriteToken => _configuration["GITHUB_EQP_WRITE_TOKEN"];

    private string ContentUrl(string path)
    {
        var escapedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        return $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contents/{escapedPath}";
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("CasaMulherHmlSnapshot/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private sealed class GitHubContentResponse
    {
        public string Content { get; set; } = string.Empty;
        public string Sha { get; set; } = string.Empty;
    }
}
