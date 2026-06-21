using System.Text.Json;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.DataProtection;

namespace CasaMulher.Api.Middleware;

public sealed class RenderAccessGateMiddleware
{
    public const string AuthCookieName = "CasaMulher.PortalEqp.Auth";
    public const string ProtectorPurpose = "CasaMulher.PortalEqp.GitHub";

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/equipe.html",
        "/equipe-ativar.html",
        "/acesso-negado.html",
        "/api/portal-eqp/status",
        "/api/portal-eqp/github/login",
        "/api/portal-eqp/github/callback",
        "/api/portal-eqp/github/logout",
        "/api/portal-eqp/me",
        "/api/portal-eqp/github/diagnostico",
        "/api/portal-eqp/acesso/solicitar"
    };

    private static readonly HashSet<string> PublicExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".mjs", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp", ".woff", ".woff2"
    };

    private readonly RequestDelegate _next;
    private readonly IDataProtector _protector;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public RenderAccessGateMiddleware(
        RequestDelegate next,
        IDataProtectionProvider dataProtectionProvider,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _environment = environment;
        _configuration = configuration;
    }

    public async Task InvokeAsync(
        HttpContext context,
        GitHubPortalSessionStore sessionStore,
        PortalEqpGateAuthorizationService authorizationService)
    {
        if (!GateAtivo() || RotaPublica(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var session = ObterSessao(context, sessionStore);

        if (session is null)
        {
            await NegarAsync(context, StatusCodes.Status401Unauthorized, "Entre com GitHub para acessar este ambiente.");
            return;
        }

        var authorization = await authorizationService.AutorizarAsync(session, context.RequestAborted);

        if (!authorization.Autorizado)
        {
            await NegarAsync(context, StatusCodes.Status403Forbidden, "Esta conta GitHub não está autorizada para o ambiente de homologação.");
            return;
        }

        await _next(context);
    }

    private bool GateAtivo()
    {
        if (bool.TryParse(_configuration["ENABLE_RENDER_GITHUB_GATE"], out var explicitValue))
        {
            return explicitValue;
        }

        return _configuration.GetValue<bool?>("RenderAccessGate:Enabled")
            ?? _environment.IsStaging();
    }

    private GitHubPortalSession? ObterSessao(HttpContext context, GitHubPortalSessionStore sessionStore)
    {
        var cookie = context.Request.Cookies[AuthCookieName];

        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null;
        }

        try
        {
            var sessionId = _protector.Unprotect(cookie);
            return sessionStore.TryGet(sessionId, out var session) ? session : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool RotaPublica(PathString path)
    {
        var value = path.Value ?? "/";

        if (PublicPaths.Contains(value))
        {
            return true;
        }

        var extension = Path.GetExtension(value);
        return !string.IsNullOrWhiteSpace(extension) && PublicExtensions.Contains(extension);
    }

    private static async Task NegarAsync(HttpContext context, int statusCode, string mensagem)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            var erro = statusCode == StatusCodes.Status401Unauthorized
                ? "GITHUB_GATE_REQUIRED"
                : "GITHUB_GATE_FORBIDDEN";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensagem, erro }));
            return;
        }

        var destination = statusCode == StatusCodes.Status403Forbidden
            ? "/acesso-negado.html"
            : "/equipe.html?gate=login_required";
        context.Response.Redirect(destination);
    }
}
