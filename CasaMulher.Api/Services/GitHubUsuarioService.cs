using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CasaMulher.Api.Services
{
    public class GitHubUsuarioService : IGitHubUsuarioService
    {
        private readonly AppDbContext _dbContext;
        private readonly GitHubIdeSettings _settings;
        private readonly IDataProtector _protector;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GitHubUsuarioService> _logger;
        private readonly IAuditoriaService _auditoriaService;

        public GitHubUsuarioService(
            AppDbContext dbContext,
            IOptions<GitHubIdeSettings> settings,
            IDataProtectionProvider dataProtectionProvider,
            IHttpClientFactory httpClientFactory,
            ILogger<GitHubUsuarioService> logger,
            IAuditoriaService auditoriaService)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
            // Create a specific protector for GitHub Tokens
            _protector = dataProtectionProvider.CreateProtector("GitHubIde.PersonalTokens");
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public async Task<GitHubConexaoStatusDto> ObterStatusConexaoAsync(ApplicationUser usuario)
        {
            var vinculo = await _dbContext.GitHubUsuarioVinculos
                .FirstOrDefaultAsync(v => v.ApplicationUserId == usuario.Id && v.RevogadoEm == null);

            if (vinculo == null)
            {
                return new GitHubConexaoStatusDto
                {
                    Conectado = false,
                    PodeConectar = _settings.PersonalForkEnabled,
                    Mensagem = "Conecte sua conta GitHub para enviar PRs com sua identidade."
                };
            }

            return new GitHubConexaoStatusDto
            {
                Conectado = true,
                Login = vinculo.GitHubLogin,
                AvatarUrl = vinculo.GitHubAvatarUrl ?? string.Empty,
                ProfileUrl = vinculo.GitHubProfileUrl ?? string.Empty,
                PodeCriarFork = _settings.PersonalForkEnabled
            };
        }

        public async Task<string> CriarUrlAutorizacaoAsync(ApplicationUser usuario, string requestIp, string userAgent)
        {
            // Generate a random secure state
            var stateBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(stateBytes);
            }
            var stateStr = Convert.ToBase64String(stateBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
            
            // Hash the state to store in DB
            using var sha256 = SHA256.Create();
            var stateHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(stateStr));
            var stateHashStr = Convert.ToBase64String(stateHashBytes);

            var oauthState = new GitHubOAuthState
            {
                ApplicationUserId = usuario.Id,
                StateHash = stateHashStr,
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddMinutes(15),
                IpSolicitante = requestIp,
                UserAgent = userAgent
            };

            _dbContext.GitHubOAuthStates.Add(oauthState);
            await _dbContext.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("IDE_GITHUB_STATE_CRIADO", "GitHubIde", null, "State de autorização OAuth criado.", usuario.IdentificadorFuncionario);

            var clientId = _settings.ClientId;
            // Scopes required for forking and creating PRs in public and private repos
            var scopes = "repo read:user";
            
            return $"https://github.com/login/oauth/authorize?client_id={clientId}&scope={Uri.EscapeDataString(scopes)}&state={Uri.EscapeDataString(stateStr)}";
        }

        public async Task ProcessarCallbackAsync(string code, string state)
        {
            // Validate State
            using var sha256 = SHA256.Create();
            var stateHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(state));
            var stateHashStr = Convert.ToBase64String(stateHashBytes);

            var oauthState = await _dbContext.GitHubOAuthStates
                .FirstOrDefaultAsync(s => s.StateHash == stateHashStr);

            if (oauthState == null || oauthState.UsadoEm != null || oauthState.ExpiraEm < DateTime.UtcNow)
            {
                await _auditoriaService.RegistrarAsync("IDE_GITHUB_STATE_INVALIDO", "GitHubIde", null, "State OAuth inválido, expirado ou já utilizado.", oauthState?.ApplicationUserId ?? "Desconhecido");
                throw new Exception("Sessão de autorização inválida ou expirada. Tente conectar novamente.");
            }

            oauthState.UsadoEm = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Load User
            var usuario = await _dbContext.Users.FindAsync(oauthState.ApplicationUserId);
            if (usuario == null) throw new Exception("Usuário não encontrado.");

            // Exchange Code for Token
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _settings.ClientId),
                new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("state", state)
            });

            var response = await client.PostAsync("https://github.com/login/oauth/access_token", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                await _auditoriaService.RegistrarAsync("IDE_GITHUB_CONEXAO_FALHA", "GitHubIde", null, "Falha ao trocar código pelo token.", usuario.IdentificadorFuncionario);
                throw new Exception("Falha na comunicação com o GitHub.");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            if (tokenData.TryGetProperty("error", out var errorElement))
            {
                var errorDesc = tokenData.TryGetProperty("error_description", out var desc) ? desc.GetString() : errorElement.GetString();
                await _auditoriaService.RegistrarAsync("IDE_GITHUB_CONEXAO_FALHA", "GitHubIde", null, $"Erro OAuth: {errorDesc}", usuario.IdentificadorFuncionario);
                throw new Exception("O GitHub recusou a autorização.");
            }

            var accessToken = tokenData.GetProperty("access_token").GetString();
            var tokenType = tokenData.TryGetProperty("token_type", out var type) ? type.GetString() : "bearer";
            var scope = tokenData.TryGetProperty("scope", out var s) ? s.GetString() : string.Empty;
            
            // Get User Profile from GitHub
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CasaMulher", "1.0"));
            
            var userResponse = await client.GetAsync("https://api.github.com/user");
            if (!userResponse.IsSuccessStatusCode)
            {
                throw new Exception("Não foi possível carregar o perfil do GitHub.");
            }

            var userJson = await userResponse.Content.ReadAsStringAsync();
            var githubUser = JsonSerializer.Deserialize<JsonElement>(userJson);
            
            var githubLogin = githubUser.GetProperty("login").GetString();
            var githubId = githubUser.GetProperty("id").GetInt64().ToString();
            var avatarUrl = githubUser.TryGetProperty("avatar_url", out var av) ? av.GetString() : string.Empty;
            var profileUrl = githubUser.TryGetProperty("html_url", out var hu) ? hu.GetString() : string.Empty;

            // Encrypt Token
            var encryptedToken = _protector.Protect(accessToken);

            // Save or Update Vinculo
            var vinculo = await _dbContext.GitHubUsuarioVinculos
                .FirstOrDefaultAsync(v => v.ApplicationUserId == usuario.Id && v.RevogadoEm == null);

            if (vinculo == null)
            {
                vinculo = new GitHubUsuarioVinculo
                {
                    ApplicationUserId = usuario.Id,
                    GitHubUserId = githubId,
                    GitHubLogin = githubLogin,
                    GitHubAvatarUrl = avatarUrl,
                    GitHubProfileUrl = profileUrl,
                    AccessTokenEncrypted = encryptedToken,
                    TokenType = tokenType ?? "bearer",
                    Scopes = scope,
                    Provider = "GitHub",
                    AppMode = "OAuthApp",
                    CriadoEm = DateTime.UtcNow
                };
                _dbContext.GitHubUsuarioVinculos.Add(vinculo);
            }
            else
            {
                vinculo.GitHubUserId = githubId;
                vinculo.GitHubLogin = githubLogin;
                vinculo.GitHubAvatarUrl = avatarUrl;
                vinculo.GitHubProfileUrl = profileUrl;
                vinculo.AccessTokenEncrypted = encryptedToken;
                vinculo.TokenType = tokenType ?? "bearer";
                vinculo.Scopes = scope;
                vinculo.AtualizadoEm = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            await _auditoriaService.RegistrarAsync("IDE_GITHUB_CONEXAO_CONCLUIDA", "GitHubIde", null, $"Conectado com sucesso à conta GitHub: {githubLogin}", usuario.IdentificadorFuncionario);
        }

        public async Task DesconectarAsync(ApplicationUser usuario)
        {
            var vinculo = await _dbContext.GitHubUsuarioVinculos
                .FirstOrDefaultAsync(v => v.ApplicationUserId == usuario.Id && v.RevogadoEm == null);

            if (vinculo != null)
            {
                vinculo.RevogadoEm = DateTime.UtcNow;
                // Delete encrypted token so it can never be retrieved
                vinculo.AccessTokenEncrypted = string.Empty;
                vinculo.RefreshTokenEncrypted = null;
                await _dbContext.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("IDE_GITHUB_CONEXAO_REVOGADA", "GitHubIde", null, $"Vínculo com GitHub ({vinculo.GitHubLogin}) revogado.", usuario.IdentificadorFuncionario);
            }
        }
    }
}
