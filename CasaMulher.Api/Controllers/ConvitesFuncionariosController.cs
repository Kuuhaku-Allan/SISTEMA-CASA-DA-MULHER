using System.Net;
using System.Security.Claims;
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
    private readonly IFuncionarioIdentificadorService _identificadorService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IMasterUserService _masterUserService;

    public ConvitesFuncionariosController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConviteCodigoService codigoService,
        IFuncionarioIdentificadorService identificadorService,
        IAuditoriaService auditoriaService,
        IEmailService emailService,
        IConfiguration configuration,
        IMasterUserService masterUserService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _codigoService = codigoService;
        _identificadorService = identificadorService;
        _auditoriaService = auditoriaService;
        _emailService = emailService;
        _configuration = configuration;
        _masterUserService = masterUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioConviteResponse>>> Listar()
    {
        if (!await PodeGerenciarConvitesInstitucionaisAsync())
        {
            return Forbid();
        }

        var agora = DateTime.UtcNow;
        var convites = await _dbContext.FuncionariosConvites
            .OrderByDescending(convite => convite.CriadoEm)
            .ToListAsync();

        return Ok(convites.Select(convite => MapearConvite(convite, agora)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FuncionarioConviteResponse>> ObterPorId(int id)
    {
        if (!await PodeGerenciarConvitesInstitucionaisAsync())
        {
            return Forbid();
        }

        var convite = await _dbContext.FuncionariosConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite não encontrado." });
        }

        return Ok(MapearConvite(convite, DateTime.UtcNow));
    }

    [HttpPost]
    public async Task<ActionResult<CriarFuncionarioConviteResponse>> Criar(CriarFuncionarioConviteRequest request)
    {
        if (!await PodeGerenciarConvitesInstitucionaisAsync())
        {
            return Forbid();
        }

        var nomeCompleto = request.NomeCompleto.Trim();
        var email = request.Email.Trim();
        var confirmarEmail = request.ConfirmarEmail.Trim();
        var perfil = request.Perfil.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(nomeCompleto))
        {
            return BadRequest(new { mensagem = "Informe o nome completo do funcionário." });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { mensagem = "Informe o e-mail do funcionário." });
        }

        if (!string.Equals(email, confirmarEmail, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Os e-mails não conferem." });
        }

        if (!PerfisAcesso.EhFuncionarioInstitucionalValido(perfil))
        {
            return BadRequest(new { mensagem = "Perfil inválido para convite." });
        }

        if (perfil == "professor")
        {
            if (string.IsNullOrWhiteSpace(request.ProfessorCurso))
            {
                return BadRequest(new { mensagem = "O curso/interesse vinculado é obrigatório para professores." });
            }
        }
        else
        {
            request.ProfessorCurso = null; // Ignora se não for professor
        }

        var usuarioExistente = await _userManager.FindByEmailAsync(email);

        if (usuarioExistente is not null)
        {
            return BadRequest(new { mensagem = "Já existe usuário cadastrado com este e-mail." });
        }

        if (await ExisteConvitePendenteParaEmail(email))
        {
            return BadRequest(new { mensagem = "Já existe convite pendente para este e-mail." });
        }

        var codigoCadastro = await GerarCodigoUnico();
        var identificadorFuncionario = await _identificadorService.GerarProximoAsync(perfil);
        var convite = new FuncionarioConvite
        {
            NomeCompleto = nomeCompleto,
            Email = email,
            Perfil = perfil,
            ProfessorCurso = request.ProfessorCurso?.Trim(),
            IdentificadorFuncionario = identificadorFuncionario,
            CodigoHash = _codigoService.GerarHash(codigoCadastro),
            ExpiraEm = DateTime.UtcNow.AddDays(request.DiasParaExpirar)
        };

        _dbContext.FuncionariosConvites.Add(convite);
        await _dbContext.SaveChangesAsync();

        var linkCadastroRelativo = GerarLinkCadastroRelativo(convite.Email, codigoCadastro);
        var linkCadastroAbsoluto = GerarLinkCadastroAbsoluto(linkCadastroRelativo);
        var linkCadastro = linkCadastroAbsoluto ?? linkCadastroRelativo;
        var resultadoEmail = request.EnviarEmail
            ? await EnviarEmailConviteAsync(convite, linkCadastroAbsoluto, request.DiasParaExpirar)
            : ResultadoEmailConvite.NaoSolicitado();

        var response = new CriarFuncionarioConviteResponse
        {
            Id = convite.Id,
            NomeCompleto = convite.NomeCompleto,
            Email = convite.Email,
            Perfil = convite.Perfil,
            IdentificadorFuncionario = convite.IdentificadorFuncionario,
            CodigoCadastro = codigoCadastro,
            LinkCadastro = linkCadastro,
            ExpiraEm = convite.ExpiraEm,
            EmailEnviado = resultadoEmail.EmailEnviado,
            StatusEmail = resultadoEmail.StatusEmail,
            AvisoEmail = resultadoEmail.AvisoEmail,
            AvisoEmailAlias = ObterAvisoEmailAlias(convite.Email)
        };

        var descricaoAuditoria = request.EnviarEmail
            ? $"Criou convite para {convite.Email} com perfil {convite.Perfil} e ID {convite.IdentificadorFuncionario}. Envio de e-mail solicitado: {resultadoEmail.StatusEmail ?? "Não informado"}."
            : $"Criou convite para {convite.Email} com perfil {convite.Perfil} e ID {convite.IdentificadorFuncionario}. Envio de e-mail não solicitado.";

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
        if (!await PodeGerenciarConvitesInstitucionaisAsync())
        {
            return Forbid();
        }

        var convite = await _dbContext.FuncionariosConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite não encontrado." });
        }

        if (convite.Usado)
        {
            return BadRequest(new { mensagem = "Convite já utilizado não pode ser cancelado." });
        }

        if (convite.Cancelado)
        {
            return BadRequest(new { mensagem = "Convite já está cancelado." });
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

    private async Task<bool> PodeGerenciarConvitesInstitucionaisAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        var usuario = await _userManager.FindByIdAsync(usuarioId);

        if (usuario is null || !usuario.Ativo)
        {
            return false;
        }

        if (string.Equals(usuario.Perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(usuario.Perfil, PerfisAcesso.Equipe, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuario.Id
            && membro.Ativo
            && membro.PapelEquipe == EquipePapeis.Owner
            && membro.CodigoEquipe == _masterUserService.EquipeOwnerCodigo);
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

        throw new InvalidOperationException("Não foi possível gerar código único de convite.");
    }

    private static FuncionarioConviteResponse MapearConvite(FuncionarioConvite convite, DateTime agora)
    {
        return new FuncionarioConviteResponse
        {
            Id = convite.Id,
            NomeCompleto = convite.NomeCompleto,
            Email = convite.Email,
            Perfil = convite.Perfil,
            ProfessorCurso = convite.ProfessorCurso,
            IdentificadorFuncionario = convite.IdentificadorFuncionario,
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

    private static string GerarLinkCadastroRelativo(string email, string codigoCadastro)
    {
        return $"cadastro.html?email={Uri.EscapeDataString(email)}&codigo={Uri.EscapeDataString(codigoCadastro)}";
    }

    private async Task<ResultadoEmailConvite> EnviarEmailConviteAsync(
        FuncionarioConvite convite,
        string? linkCadastroAbsoluto,
        int diasParaExpirar)
    {
        if (string.IsNullOrWhiteSpace(linkCadastroAbsoluto))
        {
            return ResultadoEmailConvite.SemBaseUrl();
        }

        const string assunto = "Convite de acesso - Sistema Casa da Mulher";
        var corpoHtml = MontarCorpoEmailConvite(
            convite.NomeCompleto,
            convite.IdentificadorFuncionario,
            linkCadastroAbsoluto,
            diasParaExpirar);

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
                "Convite criado, mas não foi possível enviar o e-mail. Use o link manual como alternativa.");
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

    private string? GerarLinkCadastroAbsoluto(string linkCadastroRelativo)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            return null;
        }

        return $"{frontendBaseUrl.TrimEnd('/')}/{linkCadastroRelativo}";
    }

    private static string? ObterAvisoEmailAlias(string email)
    {
        var arrobaIndex = email.IndexOf('@');

        if (arrobaIndex <= 0 || !email[..arrobaIndex].Contains('+', StringComparison.Ordinal))
        {
            return null;
        }

        return "Este e-mail contém alias com '+'. Confira se o destinatário deve receber exatamente neste endereço.";
    }

    private static string MontarCorpoEmailConvite(
        string nomeCompleto,
        string identificadorFuncionario,
        string linkCadastro,
        int diasParaExpirar)
    {
        var nome = WebUtility.HtmlEncode(nomeCompleto);
        var identificador = WebUtility.HtmlEncode(identificadorFuncionario);
        var link = WebUtility.HtmlEncode(linkCadastro);

        return $"""
            <div style="text-align: center; margin-bottom: 24px;">
                <img src="https://files.catbox.moe/ovf0uf.png" alt="Casa da Mulher de Itaquaquecetuba" style="height: 80px; width: auto;" />
            </div>
            <p>Olá, {nome}.</p>
            <p>Você recebeu um convite para criar seu acesso ao Sistema Casa da Mulher de Itaquaquecetuba.</p>
            <p>Seu ID de funcionário será:</p>
            <p><strong>{identificador}</strong></p>
            <p>Para finalizar seu cadastro, clique no botão abaixo e crie sua senha de acesso:</p>
            <p>
                <a href="{link}" style="display:inline-block;padding:12px 18px;background:#18726b;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;">
                    Finalizar meu cadastro
                </a>
            </p>
            <p>Se o botão não abrir, copie e cole este link no navegador:</p>
            <p><a href="{link}">{link}</a></p>
            <p>Este convite é individual, de uso único e expira em {diasParaExpirar} dia(s).</p>
            <p>Caso você não reconheça este convite, ignore esta mensagem ou entre em contato com a coordenação da Casa da Mulher.</p>
            <p>Atenciosamente,<br>Casa da Mulher de Itaquaquecetuba</p>
            """;
    }

    private sealed record ResultadoEmailConvite(bool EmailEnviado, string? StatusEmail, string? AvisoEmail)
    {
        public static ResultadoEmailConvite NaoSolicitado()
        {
            return new ResultadoEmailConvite(false, null, null);
        }

        public static ResultadoEmailConvite SemBaseUrl()
        {
            return new ResultadoEmailConvite(
                false,
                "NaoConfigurado",
                "Para enviar convite por e-mail, configure Frontend:BaseUrl.");
        }
    }
}
