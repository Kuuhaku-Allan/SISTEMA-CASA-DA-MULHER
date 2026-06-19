using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Middleware;
using CasaMulher.Api.Models;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/portal-eqp")]
public class PortalEqpController : ControllerBase
{
    private const string AuthCookieName = RenderAccessGateMiddleware.AuthCookieName;
    private const string StateCookieName = "CasaMulher.PortalEqp.State";
    private static readonly TimeSpan AuthCookieLifetime = TimeSpan.FromHours(12);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEquipeDbGitHubService _equipeDbService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _protector;
    private readonly GitHubPortalSessionStore _sessionStore;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PortalEqpController> _logger;

    public PortalEqpController(
        IEquipeDbGitHubService equipeDbService,
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IDataProtectionProvider dataProtectionProvider,
        GitHubPortalSessionStore sessionStore,
        IWebHostEnvironment environment,
        ILogger<PortalEqpController> logger)
    {
        _equipeDbService = equipeDbService;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _protector = dataProtectionProvider.CreateProtector(RenderAccessGateMiddleware.ProtectorPurpose);
        _sessionStore = sessionStore;
        _environment = environment;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<PortalEqpStatusResponse> Status()
    {
        var oauthConfigurado = OAuthConfigurado();
        return Ok(new PortalEqpStatusResponse
        {
            Environment = _environment.EnvironmentName,
            GitHubGateAtivo = GitHubGateAtivo(),
            OAuthConfigurado = oauthConfigurado,
            EscritaConfigurada = _equipeDbService.EscritaConfigurada,
            Organization = ObterConfig("GitHub:Org", "GitHub:Organization", "GITHUB_ORG") ?? "Sistema-Casa-da-Mulher",
            OwnerGitHub = ObterConfig("GitHub:OwnerLogin", "GitHub:OwnerUsername", "GITHUB_OWNER_LOGIN") ?? "Kuuhaku-Allan",
            DbRepository = _equipeDbService.RepositoryLabel,
            DbPath = _equipeDbService.DbPath,
            Mensagem = CriarMensagemStatus(oauthConfigurado, _equipeDbService.EscritaConfigurada)
        });
    }

    [AllowAnonymous]
    [HttpGet("github/login")]
    public IActionResult GitHubLogin()
    {
        if (!OAuthConfigurado())
        {
            return BadRequest(new { mensagem = "OAuth GitHub não configurado para o portal EQP." });
        }

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        Response.Cookies.Append(StateCookieName, state, CriarCookieOptions(httpOnly: true, AuthCookieLifetime));

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = ObterClientId(),
            ["redirect_uri"] = ObterCallbackUrl(),
            ["scope"] = "read:org",
            ["state"] = state
        };

        var url = "https://github.com/login/oauth/authorize?" + string.Join(
            "&",
            query.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));

        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        var expectedState = Request.Cookies[StateCookieName];
        Response.Cookies.Delete(StateCookieName);

        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(expectedState)
            || state.Length != expectedState.Length
            || !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(state),
                System.Text.Encoding.UTF8.GetBytes(expectedState)))
        {
            return Redirect("/equipe-ativar.html?erro=oauth_estado_invalido");
        }

        if (!OAuthConfigurado())
        {
            return Redirect("/equipe-ativar.html?erro=oauth_nao_configurado");
        }

        var accessToken = await TrocarCodigoPorTokenAsync(code, cancellationToken);
        var githubUser = await ObterUsuarioGitHubAsync(accessToken, cancellationToken);

        if (githubUser is null || githubUser.Id <= 0 || string.IsNullOrWhiteSpace(githubUser.Login))
        {
            return Redirect("/equipe-ativar.html?erro=github_usuario_invalido");
        }

        var sessionId = _sessionStore.Create(githubUser.Id.ToString(), githubUser.Login, accessToken);

        Response.Cookies.Append(
            AuthCookieName,
            _protector.Protect(sessionId),
            CriarCookieOptions(httpOnly: true, AuthCookieLifetime));

        return Redirect("/equipe-ativar.html");
    }

    [AllowAnonymous]
    [HttpPost("github/logout")]
    public IActionResult GitHubLogout()
    {
        var sessionId = ObterSessionId();

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _sessionStore.Remove(sessionId);
        }

        Response.Cookies.Delete(AuthCookieName);
        return Ok(new { mensagem = "Sessão GitHub encerrada." });
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<ActionResult<PortalEqpMeResponse>> Me(CancellationToken cancellationToken)
    {
        var ticket = ObterTicket();

        if (ticket is null)
        {
            return Ok(new PortalEqpMeResponse());
        }

        var arquivo = await _equipeDbService.LerAsync(cancellationToken);
        var document = arquivo.Document;
        var autorizado = await UsuarioAutorizadoAsync(ticket, document, cancellationToken);
        var membro = EncontrarMembroDoGitHub(document, ticket);

        return Ok(new PortalEqpMeResponse
        {
            Logado = true,
            GitHubId = ticket.GitHubId,
            GitHubUsername = ticket.GitHubUsername,
            Autorizado = autorizado,
            EhOwner = EhOwner(ticket, document),
            Membro = membro is null ? null : MapearMembro(membro)
        });
    }

    [AllowAnonymous]
    [HttpGet("convites-disponiveis")]
    public async Task<ActionResult<IEnumerable<PortalEqpConviteResponse>>> ConvitesDisponiveis(CancellationToken cancellationToken)
    {
        var acesso = await ObterAcessoAutorizadoAsync(cancellationToken);

        if (acesso.Result is not null)
        {
            return acesso.Result;
        }

        if (EncontrarMembroDoGitHub(acesso.Document!, acesso.Ticket!) is not null)
        {
            return Ok(Array.Empty<PortalEqpConviteResponse>());
        }

        var username = acesso.Ticket!.GitHubUsername;
        var convites = acesso.Document!.Convites
            .Where(convite => ConvitePodeAparecerParaUsuario(convite, username))
            .OrderBy(convite => convite.EqpId)
            .Select(MapearConvite)
            .ToList();

        return Ok(convites);
    }

    [AllowAnonymous]
    [EnableRateLimiting("rate-equipe-ativacao")]
    [HttpPost("ativar")]
    public async Task<ActionResult<PortalEqpMembroResponse>> Ativar(PortalEqpAtivarRequest request, CancellationToken cancellationToken)
    {
        if (request.Senha != request.ConfirmarSenha)
        {
            return BadRequest(new { mensagem = "Senha e confirmação não conferem." });
        }

        var acesso = await ObterAcessoAutorizadoAsync(cancellationToken);

        if (acesso.Result is not null)
        {
            return acesso.Result;
        }

        var erroSenha = await ValidarSenhaAsync(request.Nome, acesso.Ticket!.GitHubUsername, request.Senha);

        if (erroSenha is not null)
        {
            return erroSenha;
        }

        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            var arquivo = await _equipeDbService.LerAsync(cancellationToken);
            var document = arquivo.Document;

            if (!await UsuarioAutorizadoAsync(acesso.Ticket!, document, cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "GitHub não autorizado para ativar EQP." });
            }

            if (EncontrarMembroDoGitHub(document, acesso.Ticket!) is not null)
            {
                return BadRequest(new { mensagem = "Você já ativou seu EQP." });
            }

            var eqpId = NormalizarId(request.EqpId);
            var convite = document.Convites.FirstOrDefault(item =>
                string.Equals(item.EqpId, eqpId, StringComparison.OrdinalIgnoreCase));

            if (convite is null || !ConvitePodeAparecerParaUsuario(convite, acesso.Ticket.GitHubUsername))
            {
                return BadRequest(new { mensagem = "Convite EQP indisponível para este GitHub." });
            }

            if (document.Membros.Any(membro =>
                    string.Equals(membro.EqpId, convite.EqpId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(membro.AdmId, convite.AdmId, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { mensagem = "Este EQP/ADM já foi ativado." });
            }

            var agora = DateTime.UtcNow;
            var usuarioHash = CriarUsuarioParaHash(request.Nome, convite.EqpId);
            var membro = new EquipeDbMembro
            {
                EqpId = convite.EqpId,
                AdmId = convite.AdmId,
                Nome = request.Nome.Trim(),
                GitHubId = acesso.Ticket.GitHubId,
                GitHubUsername = acesso.Ticket.GitHubUsername,
                PapelEquipe = convite.PapelEquipe,
                FluxoTrabalho = convite.FluxoTrabalho,
                Status = "ativo",
                PasswordHash = _passwordHasher.HashPassword(usuarioHash, request.Senha),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                SenhaAtualizadaEm = agora,
                PasswordVersion = 1,
                AtivadoEm = agora,
                AtualizadoEm = agora
            };

            convite.Status = EquipeDbStatusConvite.Usado;
            convite.UsadoEm = agora;
            document.Membros.Add(membro);

            try
            {
                await _equipeDbService.SalvarAsync(
                    document,
                    arquivo.Sha,
                    $"Ativa {convite.EqpId} para {acesso.Ticket.GitHubUsername}",
                    cancellationToken);

                await RegistrarEventoAsync(new EquipeDbEvent
                {
                    Timestamp = agora,
                    Tipo = "ativacao",
                    EqpId = convite.EqpId,
                    AdmId = convite.AdmId,
                    GitHubId = acesso.Ticket.GitHubId,
                    GitHubUsername = acesso.Ticket.GitHubUsername,
                    Descricao = $"Ativou {convite.EqpId} com ADM pareado."
                }, $"Registra evento de ativação {convite.EqpId}", cancellationToken);

                return Ok(MapearMembro(membro));
            }
            catch (EquipeDbGitHubException ex) when (ex.StatusCode == 409 && tentativa < 2)
            {
                _logger.LogInformation("Conflito ao ativar {EqpId}; tentando novamente.", convite.EqpId);
            }
        }

        return StatusCode(StatusCodes.Status409Conflict, new { mensagem = "O banco da equipe mudou agora. Tente ativar novamente." });
    }

    [AllowAnonymous]
    [EnableRateLimiting("rate-equipe-ativacao")]
    [HttpPost("redefinir-minha-senha")]
    public async Task<ActionResult<PortalEqpMembroResponse>> RedefinirMinhaSenha(PortalEqpRedefinirSenhaRequest request, CancellationToken cancellationToken)
    {
        if (request.NovaSenha != request.ConfirmarSenha)
        {
            return BadRequest(new { mensagem = "Nova senha e confirmação não conferem." });
        }

        var ticket = ObterTicket();

        if (ticket is null)
        {
            return Unauthorized(new { mensagem = "Entre com GitHub para redefinir sua senha." });
        }

        var erroSenha = await ValidarSenhaAsync(ticket.GitHubUsername, ticket.GitHubUsername, request.NovaSenha);

        if (erroSenha is not null)
        {
            return erroSenha;
        }

        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            var arquivo = await _equipeDbService.LerAsync(cancellationToken);
            var document = arquivo.Document;
            var membro = EncontrarMembroDoGitHub(document, ticket);

            if (membro is null)
            {
                return BadRequest(new { mensagem = "Seu GitHub ainda não possui EQP ativado." });
            }

            var agora = DateTime.UtcNow;
            membro.PasswordHash = _passwordHasher.HashPassword(CriarUsuarioParaHash(membro.Nome, membro.EqpId), request.NovaSenha);
            membro.SecurityStamp = Guid.NewGuid().ToString("N");
            membro.SenhaAtualizadaEm = agora;
            membro.PasswordVersion = Math.Max(membro.PasswordVersion ?? 0, 0) + 1;
            membro.AtualizadoEm = agora;

            try
            {
                await _equipeDbService.SalvarAsync(
                    document,
                    arquivo.Sha,
                    $"Redefine senha de {membro.EqpId}",
                    cancellationToken);

                await RegistrarEventoAsync(new EquipeDbEvent
                {
                    Timestamp = agora,
                    Tipo = "senha_redefinida",
                    EqpId = membro.EqpId,
                    AdmId = membro.AdmId,
                    GitHubId = ticket.GitHubId,
                    GitHubUsername = ticket.GitHubUsername,
                    Descricao = "Usuário redefiniu a própria senha no portal EQP."
                }, $"Registra redefinição de senha {membro.EqpId}", cancellationToken);

                return Ok(MapearMembro(membro));
            }
            catch (EquipeDbGitHubException ex) when (ex.StatusCode == 409 && tentativa < 2)
            {
                _logger.LogInformation("Conflito ao redefinir senha de {EqpId}; tentando novamente.", membro.EqpId);
            }
        }

        return StatusCode(StatusCodes.Status409Conflict, new { mensagem = "O banco da equipe mudou agora. Tente novamente." });
    }

    [AllowAnonymous]
    [HttpPost("admin/criar-convite")]
    public async Task<ActionResult<PortalEqpConviteResponse>> CriarConvite(PortalEqpCriarConviteRequest request, CancellationToken cancellationToken)
    {
        var result = await CriarConvitesAdminAsync(request, quantidade: 1, cancellationToken);

        if (result.Result is ObjectResult objectResult
            && objectResult.Value is IReadOnlyCollection<PortalEqpConviteResponse> convites
            && convites.Count > 0)
        {
            return Ok(convites.First());
        }

        return result.Result is not null
            ? new ActionResult<PortalEqpConviteResponse>(result.Result)
            : StatusCode(StatusCodes.Status500InternalServerError, new { mensagem = "Não foi possível criar convite." });
    }

    [AllowAnonymous]
    [HttpPost("admin/criar-lote")]
    public async Task<ActionResult<IReadOnlyCollection<PortalEqpConviteResponse>>> CriarLote(PortalEqpCriarLoteRequest request, CancellationToken cancellationToken)
    {
        var quantidade = Math.Clamp(request.Quantidade, 1, 20);
        var result = await CriarConvitesAdminAsync(request, quantidade, cancellationToken);

        if (result.Result is ObjectResult objectResult
            && objectResult.Value is IReadOnlyCollection<PortalEqpConviteResponse> convites)
        {
            return Ok(convites);
        }

        return result.Result is not null
            ? new ActionResult<IReadOnlyCollection<PortalEqpConviteResponse>>(result.Result)
            : Ok(Array.Empty<PortalEqpConviteResponse>());
    }

    [AllowAnonymous]
    [HttpGet("admin/db")]
    public async Task<ActionResult<PortalEqpAdminDbResponse>> AdminDb(CancellationToken cancellationToken)
    {
        var acesso = await ObterAcessoOwnerAsync(cancellationToken);

        if (acesso.Result is not null)
        {
            return acesso.Result;
        }

        var document = acesso.Document!;
        return Ok(new PortalEqpAdminDbResponse
        {
            Convites = document.Convites.Count,
            Membros = document.Membros.Count,
            Settings = document.Settings,
            ConvitesResumo = document.Convites.OrderBy(item => item.EqpId).Select(MapearConvite).ToList(),
            MembrosResumo = document.Membros.OrderBy(item => item.EqpId).Select(MapearMembro).ToList()
        });
    }

    private async Task<ActionResult<IReadOnlyCollection<PortalEqpConviteResponse>>> CriarConvitesAdminAsync(
        PortalEqpCriarConviteRequest request,
        int quantidade,
        CancellationToken cancellationToken)
    {
        var acesso = await ObterAcessoOwnerAsync(cancellationToken);

        if (acesso.Result is not null)
        {
            return acesso.Result;
        }

        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            var arquivo = await _equipeDbService.LerAsync(cancellationToken);
            var document = arquivo.Document;

            if (!EhOwner(acesso.Ticket!, document))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "Somente o owner pode criar convites." });
            }

            var criados = new List<EquipeDbConvite>();

            for (var index = 0; index < quantidade; index++)
            {
                criados.Add(CriarProximoConvite(document, request));
            }

            try
            {
                var descricao = quantidade == 1
                    ? $"Cria convite {criados[0].EqpId}"
                    : $"Cria lote de {quantidade} convites EQP";

                await _equipeDbService.SalvarAsync(document, arquivo.Sha, descricao, cancellationToken);

                foreach (var convite in criados)
                {
                    await RegistrarEventoAsync(new EquipeDbEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Tipo = "convite_criado",
                        EqpId = convite.EqpId,
                        AdmId = convite.AdmId,
                        GitHubId = acesso.Ticket!.GitHubId,
                        GitHubUsername = acesso.Ticket.GitHubUsername,
                        Descricao = $"Convite {convite.EqpId} criado pelo owner."
                    }, $"Registra criação de convite {convite.EqpId}", cancellationToken);
                }

                return Ok(criados.Select(MapearConvite).ToList());
            }
            catch (EquipeDbGitHubException ex) when (ex.StatusCode == 409 && tentativa < 2)
            {
                _logger.LogInformation("Conflito ao criar convite; tentando novamente.");
            }
        }

        return StatusCode(StatusCodes.Status409Conflict, new { mensagem = "O banco da equipe mudou agora. Tente novamente." });
    }

    private async Task<(ActionResult? Result, GitHubPortalTicket? Ticket, EquipeDbDocument? Document)> ObterAcessoAutorizadoAsync(
        CancellationToken cancellationToken)
    {
        var ticket = ObterTicket();

        if (ticket is null)
        {
            return (Unauthorized(new { mensagem = "Entre com GitHub para acessar o portal EQP." }), null, null);
        }

        var arquivo = await _equipeDbService.LerAsync(cancellationToken);

        if (!await UsuarioAutorizadoAsync(ticket, arquivo.Document, cancellationToken))
        {
            return (StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "GitHub não autorizado para o portal EQP." }), ticket, arquivo.Document);
        }

        return (null, ticket, arquivo.Document);
    }

    private async Task<(ActionResult? Result, GitHubPortalTicket? Ticket, EquipeDbDocument? Document)> ObterAcessoOwnerAsync(
        CancellationToken cancellationToken)
    {
        var ticket = ObterTicket();

        if (ticket is null)
        {
            return (Unauthorized(new { mensagem = "Entre com GitHub para acessar a área do owner." }), null, null);
        }

        var arquivo = await _equipeDbService.LerAsync(cancellationToken);

        if (!EhOwner(ticket, arquivo.Document))
        {
            return (StatusCode(StatusCodes.Status403Forbidden, new { mensagem = "Somente o owner pode executar esta ação." }), ticket, arquivo.Document);
        }

        return (null, ticket, arquivo.Document);
    }

    private async Task<ActionResult?> ValidarSenhaAsync(string nome, string username, string senha)
    {
        var usuario = CriarUsuarioParaHash(nome, username);

        foreach (var validator in _userManager.PasswordValidators)
        {
            var resultado = await validator.ValidateAsync(_userManager, usuario, senha);

            if (!resultado.Succeeded)
            {
                return BadRequest(new
                {
                    mensagem = "A senha não atende à política do sistema.",
                    erros = resultado.Errors.Select(error => error.Description)
                });
            }
        }

        return null;
    }

    private ApplicationUser CriarUsuarioParaHash(string nome, string identificador)
    {
        var id = string.IsNullOrWhiteSpace(identificador)
            ? Guid.NewGuid().ToString("N")
            : identificador.Trim();

        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = id,
            IdentificadorFuncionario = id,
            NomeCompleto = string.IsNullOrWhiteSpace(nome) ? id : nome.Trim(),
            Email = $"{id.ToLowerInvariant().Replace("@", "_", StringComparison.Ordinal)}@portal-eqp.local"
        };
    }

    private async Task<string> TrocarCodigoPorTokenAsync(string code, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ObterClientId()!,
            ["client_secret"] = ObterClientSecret()!,
            ["code"] = code,
            ["redirect_uri"] = ObterCallbackUrl()
        });

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Resposta OAuth GitHub vazia.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("O GitHub não retornou o access_token.");
        }

        return payload.AccessToken;
    }

    private async Task<GitHubUserResponse?> ObterUsuarioGitHubAsync(string accessToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        using var request = CriarGitHubRequest(HttpMethod.Get, "https://api.github.com/user", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GitHubUserResponse>(JsonOptions, cancellationToken);
    }

    private async Task<bool> UsuarioAutorizadoAsync(
        GitHubPortalTicket ticket,
        EquipeDbDocument document,
        CancellationToken cancellationToken)
    {
        if (EhOwner(ticket, document))
        {
            return true;
        }

        var allowlist = document.AllowlistGitHub
            .Concat(ObterAllowlistAmbiente())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowlist.Contains(ticket.GitHubUsername))
        {
            _logger.LogInformation("Portal EQP autorizou {GitHub} por allowlist.", ticket.GitHubUsername);
            return true;
        }

        var org = ObterOrg(document);
        var pertenceOrg = await VerificarOrganizacaoAsync(ticket, org, cancellationToken);

        _logger.LogInformation(
            "Autorização GitHub no portal EQP para {GitHub}: organização {Org}, autorizado={Autorizado}.",
            ticket.GitHubUsername,
            org,
            pertenceOrg);

        return pertenceOrg;
    }

    private async Task<bool> VerificarOrganizacaoAsync(
        GitHubPortalTicket ticket,
        string org,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticket.AccessToken) || string.IsNullOrWhiteSpace(org))
        {
            return false;
        }

        var client = _httpClientFactory.CreateClient();
        var membershipUrl = $"https://api.github.com/user/memberships/orgs/{Uri.EscapeDataString(org)}";

        using (var request = CriarGitHubRequest(HttpMethod.Get, membershipUrl, ticket.AccessToken))
        using (var response = await client.SendAsync(request, cancellationToken))
        {
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("GitHub retornou {Status} ao verificar membership da org.", response.StatusCode);
            }
        }

        var publicMemberUrl =
            $"https://api.github.com/orgs/{Uri.EscapeDataString(org)}/members/{Uri.EscapeDataString(ticket.GitHubUsername)}";
        using var publicRequest = CriarGitHubRequest(HttpMethod.Get, publicMemberUrl, ticket.AccessToken);
        using var publicResponse = await client.SendAsync(publicRequest, cancellationToken);
        return publicResponse.IsSuccessStatusCode;
    }

    private HttpRequestMessage CriarGitHubRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("CasaMulherPortalEqp/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private GitHubPortalTicket? ObterTicket()
    {
        var sessionId = ObterSessionId();

        if (string.IsNullOrWhiteSpace(sessionId)
            || !_sessionStore.TryGet(sessionId, out var session)
            || session is null)
        {
            return null;
        }

        return new GitHubPortalTicket(
            session.GitHubId,
            session.GitHubUsername,
            session.AccessToken,
            session.EmitidoEm);
    }

    private string? ObterSessionId()
    {
        var cookie = Request.Cookies[AuthCookieName];

        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(cookie);
        }
        catch
        {
            return null;
        }
    }

    private CookieOptions CriarCookieOptions(bool httpOnly, TimeSpan lifetime)
    {
        return new CookieOptions
        {
            HttpOnly = httpOnly,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = RequisicaoHttps(),
            Expires = DateTimeOffset.UtcNow.Add(lifetime)
        };
    }

    private bool RequisicaoHttps()
    {
        return Request.IsHttps
            || string.Equals(Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase);
    }

    private string ObterCallbackUrl()
    {
        var baseUrl = ObterConfig("PortalEqp:BaseUrl", "PORTAL_EQP_BASE_URL", "RENDER_EXTERNAL_URL");

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            return $"{baseUrl.TrimEnd('/')}/api/portal-eqp/github/callback";
        }

        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
        return $"{scheme}://{Request.Host}/api/portal-eqp/github/callback";
    }

    private string? ObterClientId()
    {
        return ObterConfig("GitHub:OAuthClientId", "GitHub:ClientId", "GITHUB_OAUTH_CLIENT_ID");
    }

    private string? ObterClientSecret()
    {
        return ObterConfig("GitHub:OAuthClientSecret", "GitHub:ClientSecret", "GITHUB_OAUTH_CLIENT_SECRET");
    }

    private bool OAuthConfigurado()
    {
        return !string.IsNullOrWhiteSpace(ObterClientId())
            && !string.IsNullOrWhiteSpace(ObterClientSecret());
    }

    private bool GitHubGateAtivo()
    {
        if (bool.TryParse(_configuration["ENABLE_RENDER_GITHUB_GATE"], out var explicitValue))
        {
            return explicitValue;
        }

        return _configuration.GetValue<bool?>("RenderAccessGate:Enabled")
            ?? _environment.IsStaging();
    }

    private string? ObterConfig(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private IEnumerable<string> ObterAllowlistAmbiente()
    {
        var value = ObterConfig("GitHub:EqpAllowlist", "GITHUB_EQP_ALLOWLIST");

        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private string ObterOrg(EquipeDbDocument document)
    {
        return ObterConfig("GitHub:Org", "GitHub:Organization", "GITHUB_ORG")
            ?? document.Settings.Org
            ?? "Sistema-Casa-da-Mulher";
    }

    private bool EhOwner(GitHubPortalTicket ticket, EquipeDbDocument document)
    {
        var owner = ObterConfig("GitHub:OwnerLogin", "GitHub:OwnerUsername", "GITHUB_OWNER_LOGIN")
            ?? document.Settings.OwnerGitHub
            ?? "Kuuhaku-Allan";

        return string.Equals(ticket.GitHubUsername, owner, StringComparison.OrdinalIgnoreCase);
    }

    private EquipeDbMembro? EncontrarMembroDoGitHub(EquipeDbDocument document, GitHubPortalTicket ticket)
    {
        return document.Membros.FirstOrDefault(membro =>
            string.Equals(membro.Status, "ativo", StringComparison.OrdinalIgnoreCase)
            && (
                string.Equals(membro.GitHubId, ticket.GitHubId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(membro.GitHubUsername, ticket.GitHubUsername, StringComparison.OrdinalIgnoreCase)
            ));
    }

    private static bool ConvitePodeAparecerParaUsuario(EquipeDbConvite convite, string username)
    {
        if (string.Equals(convite.Status, EquipeDbStatusConvite.Disponivel, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(convite.ReservadoParaGitHub))
        {
            return true;
        }

        return string.Equals(convite.Status, EquipeDbStatusConvite.Reservado, StringComparison.OrdinalIgnoreCase)
            && string.Equals(convite.ReservadoParaGitHub, username, StringComparison.OrdinalIgnoreCase);
    }

    private EquipeDbConvite CriarProximoConvite(EquipeDbDocument document, PortalEqpCriarConviteRequest request)
    {
        var eqpNumero = Math.Max(document.Settings.NextEqpNumber, 1);
        var admNumero = Math.Max(document.Settings.NextAdmNumber, 1);

        string eqpId;
        string admId;

        do
        {
            eqpId = $"EQP-{eqpNumero:000000}";
            admId = $"ADM-{admNumero:000000}";
            eqpNumero++;
            admNumero++;
        }
        while (document.Convites.Any(item =>
                   string.Equals(item.EqpId, eqpId, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(item.AdmId, admId, StringComparison.OrdinalIgnoreCase))
               || document.Membros.Any(item =>
                   string.Equals(item.EqpId, eqpId, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(item.AdmId, admId, StringComparison.OrdinalIgnoreCase)));

        document.Settings.NextEqpNumber = eqpNumero;
        document.Settings.NextAdmNumber = admNumero;

        var convite = new EquipeDbConvite
        {
            EqpId = eqpId,
            AdmId = admId,
            Status = string.IsNullOrWhiteSpace(request.ReservadoParaGitHub)
                ? EquipeDbStatusConvite.Disponivel
                : EquipeDbStatusConvite.Reservado,
            ReservadoParaGitHub = string.IsNullOrWhiteSpace(request.ReservadoParaGitHub)
                ? null
                : request.ReservadoParaGitHub.Trim(),
            PapelEquipe = string.IsNullOrWhiteSpace(request.PapelEquipe) ? "contributor" : request.PapelEquipe.Trim().ToLowerInvariant(),
            FluxoTrabalho = string.IsNullOrWhiteSpace(request.FluxoTrabalho) ? "fork_codespaces" : request.FluxoTrabalho.Trim().ToLowerInvariant(),
            CriadoEm = DateTime.UtcNow
        };

        document.Convites.Add(convite);
        return convite;
    }

    private async Task RegistrarEventoAsync(EquipeDbEvent evento, string commitMessage, CancellationToken cancellationToken)
    {
        try
        {
            await _equipeDbService.AcrescentarEventoAsync(evento, commitMessage, cancellationToken);
        }
        catch (EquipeDbGitHubException ex)
        {
            _logger.LogWarning(ex, "Não foi possível registrar o evento {Tipo} para {EqpId}.", evento.Tipo, evento.EqpId);
        }
    }

    private static PortalEqpConviteResponse MapearConvite(EquipeDbConvite convite)
    {
        return new PortalEqpConviteResponse
        {
            EqpId = convite.EqpId,
            AdmId = convite.AdmId,
            Status = convite.Status,
            ReservadoParaGitHub = convite.ReservadoParaGitHub,
            PapelEquipe = convite.PapelEquipe,
            FluxoTrabalho = convite.FluxoTrabalho
        };
    }

    private static PortalEqpMembroResponse MapearMembro(EquipeDbMembro membro)
    {
        return new PortalEqpMembroResponse
        {
            EqpId = membro.EqpId,
            AdmId = membro.AdmId,
            Nome = membro.Nome,
            GitHubUsername = membro.GitHubUsername,
            PapelEquipe = membro.PapelEquipe,
            FluxoTrabalho = membro.FluxoTrabalho,
            AtivadoEm = membro.AtivadoEm
        };
    }

    private static string NormalizarId(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string CriarMensagemStatus(bool oauthConfigurado, bool escritaConfigurada)
    {
        if (!oauthConfigurado)
        {
            return "OAuth GitHub não configurado. O portal está em modo de diagnóstico.";
        }

        if (!escritaConfigurada)
        {
            return "Token de escrita não configurado. A ativação e a redefinição de senha ficam bloqueadas.";
        }

        return "Portal EQP configurado.";
    }

    private sealed record GitHubPortalTicket(
        string GitHubId,
        string GitHubUsername,
        string AccessToken,
        DateTimeOffset EmitidoEm);

    private sealed class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class GitHubUserResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }
}
