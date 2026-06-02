using System.Net;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAcesso.SomenteAdm)]
[Route("api/convites-funcionarios")]
public class ConvitesFuncionariosController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConviteCodigoService _codigoService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ConvitesFuncionariosController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConviteCodigoService codigoService,
        IAuditoriaService auditoriaService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _codigoService = codigoService;
        _auditoriaService = auditoriaService;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioConviteResponse>>> Listar()
    {
        var agora = DateTime.UtcNow;
        var convites = await _dbContext.FuncionariosConvites
            .OrderByDescending(convite => convite.CriadoEm)
            .ToListAsync();

        return Ok(convites.Select(convite => MapearConvite(convite, agora)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FuncionarioConviteResponse>> ObterPorId(int id)
    {
        var convite = await _dbContext.FuncionariosConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite nao encontrado." });
        }

        return Ok(MapearConvite(convite, DateTime.UtcNow));
    }

    [HttpPost]
    public async Task<ActionResult<CriarFuncionarioConviteResponse>> Criar(CriarFuncionarioConviteRequest request)
    {
        var email = request.Email.Trim();
        var perfil = request.Perfil.Trim().ToLowerInvariant();

        if (!PerfisAcesso.EhValido(perfil))
        {
            return BadRequest(new { mensagem = "Perfil invalido para convite." });
        }

        var usuarioExistente = await _userManager.FindByEmailAsync(email);

        if (usuarioExistente is not null)
        {
            return BadRequest(new { mensagem = "Ja existe usuario cadastrado com este e-mail." });
        }

        if (await ExisteConvitePendenteParaEmail(email))
        {
            return BadRequest(new { mensagem = "Ja existe convite pendente para este e-mail." });
        }

        var codigoCadastro = await GerarCodigoUnico();
        var convite = new FuncionarioConvite
        {
            NomeCompleto = request.NomeCompleto.Trim(),
            Email = email,
            Perfil = perfil,
            CodigoHash = _codigoService.GerarHash(codigoCadastro),
            ExpiraEm = DateTime.UtcNow.AddDays(request.DiasParaExpirar)
        };

        _dbContext.FuncionariosConvites.Add(convite);
        await _dbContext.SaveChangesAsync();

        var linkCadastro = GerarLinkCadastro(convite.Email, codigoCadastro);
        var resultadoEmail = request.EnviarEmail
            ? await EnviarEmailConviteAsync(convite, linkCadastro, request.DiasParaExpirar)
            : ResultadoEmailConvite.NaoSolicitado();

        var response = new CriarFuncionarioConviteResponse
        {
            Id = convite.Id,
            NomeCompleto = convite.NomeCompleto,
            Email = convite.Email,
            Perfil = convite.Perfil,
            CodigoCadastro = codigoCadastro,
            LinkCadastro = linkCadastro,
            ExpiraEm = convite.ExpiraEm,
            EmailEnviado = resultadoEmail.EmailEnviado,
            StatusEmail = resultadoEmail.StatusEmail,
            AvisoEmail = resultadoEmail.AvisoEmail
        };

        var descricaoAuditoria = request.EnviarEmail
            ? $"Criou convite para {convite.Email} com perfil {convite.Perfil}. Envio de e-mail solicitado: {resultadoEmail.StatusEmail ?? "Nao informado"}."
            : $"Criou convite para {convite.Email} com perfil {convite.Perfil}. Envio de e-mail nao solicitado.";

        await _auditoriaService.RegistrarAsync(
            "CONVITE_CRIADO",
            "FuncionarioConvite",
            convite.Id.ToString(),
            descricaoAuditoria);

        return CreatedAtAction(nameof(ObterPorId), new { id = convite.Id }, response);
    }

    [HttpPatch("{id:int}/cancelar")]
    public async Task<ActionResult<FuncionarioConviteResponse>> Cancelar(int id)
    {
        var convite = await _dbContext.FuncionariosConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite nao encontrado." });
        }

        if (convite.Usado)
        {
            return BadRequest(new { mensagem = "Convite ja utilizado nao pode ser cancelado." });
        }

        if (convite.Cancelado)
        {
            return BadRequest(new { mensagem = "Convite ja esta cancelado." });
        }

        convite.Cancelado = true;
        convite.CanceladoEm = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditoriaService.RegistrarAsync(
            "CONVITE_CANCELADO",
            "FuncionarioConvite",
            convite.Id.ToString(),
            $"Cancelou convite para {convite.Email} com perfil {convite.Perfil}.");

        return Ok(MapearConvite(convite, DateTime.UtcNow));
    }

    private async Task<bool> ExisteConvitePendenteParaEmail(string email)
    {
        var agora = DateTime.UtcNow;
        var emailNormalizado = email.Trim().ToUpperInvariant();

        return await _dbContext.FuncionariosConvites.AnyAsync(convite =>
            convite.Email.ToUpper() == emailNormalizado
            && !convite.Usado
            && !convite.Cancelado
            && convite.ExpiraEm >= agora);
    }

    private async Task<string> GerarCodigoUnico()
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var codigo = _codigoService.GerarCodigoCadastro();
            var codigoHash = _codigoService.GerarHash(codigo);
            var existe = await _dbContext.FuncionariosConvites.AnyAsync(convite => convite.CodigoHash == codigoHash);

            if (!existe)
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("Nao foi possivel gerar codigo unico de convite.");
    }

    private static FuncionarioConviteResponse MapearConvite(FuncionarioConvite convite, DateTime agora)
    {
        return new FuncionarioConviteResponse
        {
            Id = convite.Id,
            NomeCompleto = convite.NomeCompleto,
            Email = convite.Email,
            Perfil = convite.Perfil,
            Status = ObterStatus(convite, agora),
            CriadoEm = convite.CriadoEm,
            ExpiraEm = convite.ExpiraEm,
            UsadoEm = convite.UsadoEm,
            CanceladoEm = convite.CanceladoEm
        };
    }

    private static string ObterStatus(FuncionarioConvite convite, DateTime agora)
    {
        if (convite.Cancelado)
        {
            return "Cancelado";
        }

        if (convite.Usado)
        {
            return "Usado";
        }

        if (convite.ExpiraEm < agora)
        {
            return "Expirado";
        }

        return "Pendente";
    }

    private static string GerarLinkCadastro(string email, string codigoCadastro)
    {
        return $"cadastro.html?email={Uri.EscapeDataString(email)}&codigo={Uri.EscapeDataString(codigoCadastro)}";
    }

    private async Task<ResultadoEmailConvite> EnviarEmailConviteAsync(
        FuncionarioConvite convite,
        string linkCadastro,
        int diasParaExpirar)
    {
        const string assunto = "Convite de acesso - Sistema Casa da Mulher";
        var linkEmail = GerarLinkCadastroParaEmail(linkCadastro);
        var corpoHtml = MontarCorpoEmailConvite(convite.NomeCompleto, linkEmail, diasParaExpirar);

        try
        {
            await _emailService.EnviarAsync(convite.Email, assunto, corpoHtml, "ConviteFuncionario");
            var status = await ObterUltimoStatusEmailAsync(convite.Email, assunto, "ConviteFuncionario");

            return new ResultadoEmailConvite(true, status ?? "Enviado", null);
        }
        catch
        {
            var status = await ObterUltimoStatusEmailAsync(convite.Email, assunto, "ConviteFuncionario") ?? "Falhou";

            return new ResultadoEmailConvite(
                false,
                status,
                "Convite criado, mas nao foi possivel enviar o e-mail. Use o link manual como alternativa.");
        }
    }

    private async Task<string?> ObterUltimoStatusEmailAsync(string destinatario, string assunto, string tipo)
    {
        return await _dbContext.EmailEventos
            .Where(evento =>
                evento.Destinatario == destinatario
                && evento.Assunto == assunto
                && evento.Tipo == tipo)
            .OrderByDescending(evento => evento.CriadoEm)
            .Select(evento => evento.Status)
            .FirstOrDefaultAsync();
    }

    private string GerarLinkCadastroParaEmail(string linkCadastro)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            var origin = Request.Headers.Origin.ToString();

            if (!string.IsNullOrWhiteSpace(origin) && !string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase))
            {
                frontendBaseUrl = origin;
            }
        }

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            return linkCadastro;
        }

        return $"{frontendBaseUrl.TrimEnd('/')}/{linkCadastro}";
    }

    private static string MontarCorpoEmailConvite(string nomeCompleto, string linkCadastro, int diasParaExpirar)
    {
        var nome = WebUtility.HtmlEncode(nomeCompleto);
        var link = WebUtility.HtmlEncode(linkCadastro);

        return $"""
            <p>Ola, {nome}.</p>
            <p>Voce foi convidada para criar seu acesso ao Sistema Casa da Mulher.</p>
            <p><a href="{link}">Clique aqui para concluir seu cadastro</a>.</p>
            <p>Este convite expira em {diasParaExpirar} dia(s).</p>
            <p>Se voce nao esperava este convite, ignore esta mensagem.</p>
            """;
    }

    private sealed record ResultadoEmailConvite(bool EmailEnviado, string? StatusEmail, string? AvisoEmail)
    {
        public static ResultadoEmailConvite NaoSolicitado()
        {
            return new ResultadoEmailConvite(false, null, null);
        }
    }
}
