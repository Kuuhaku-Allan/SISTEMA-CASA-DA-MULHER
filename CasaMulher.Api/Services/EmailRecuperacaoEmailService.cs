using System.Net;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Services;

public class EmailRecuperacaoEmailService : IEmailRecuperacaoEmailService
{
    private const string TipoEmail = "ConfirmacaoEmailRecuperacao";
    private const string Assunto = "Confirmação de e-mail de recuperação - Sistema Casa da Mulher";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public EmailRecuperacaoEmailService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<ResultadoEmailRecuperacao> EnviarConfirmacaoAsync(ApplicationUser funcionario)
    {
        if (string.IsNullOrWhiteSpace(funcionario.EmailRecuperacao))
        {
            return ResultadoEmailRecuperacao.SemEmail();
        }

        var emailRecuperacao = funcionario.EmailRecuperacao.Trim();
        var token = await _userManager.GenerateUserTokenAsync(
            funcionario,
            TokenOptions.DefaultProvider,
            EmailRecuperacaoTokenPurpose.Criar(emailRecuperacao));

        var linkRelativo = GerarLinkConfirmacaoRelativo(emailRecuperacao, token);
        var linkAbsoluto = GerarLinkAbsoluto(linkRelativo);

        if (string.IsNullOrWhiteSpace(linkAbsoluto))
        {
            return ResultadoEmailRecuperacao.SemBaseUrl();
        }

        var corpoHtml = MontarCorpoEmail(funcionario.NomeCompleto, linkAbsoluto);

        try
        {
            await _emailService.EnviarAsync(emailRecuperacao, Assunto, corpoHtml, TipoEmail);
            var status = await ObterUltimoStatusEmailAsync(emailRecuperacao, Assunto, TipoEmail);

            return new ResultadoEmailRecuperacao(
                true,
                status ?? "Enviado",
                null,
                _environment.IsDevelopment() ? linkAbsoluto : null);
        }
        catch
        {
            var status = await ObterUltimoStatusEmailAsync(emailRecuperacao, Assunto, TipoEmail) ?? "Falhou";

            return new ResultadoEmailRecuperacao(
                false,
                status,
                "Não foi possível enviar o link de confirmação. Confira a configuração de e-mail.",
                _environment.IsDevelopment() ? linkAbsoluto : null);
        }
    }

    private static string GerarLinkConfirmacaoRelativo(string emailRecuperacao, string token)
    {
        return $"confirmar-email-recuperacao.html?email={Uri.EscapeDataString(emailRecuperacao)}&token={Uri.EscapeDataString(token)}";
    }

    private string? GerarLinkAbsoluto(string linkRelativo)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            return null;
        }

        return $"{frontendBaseUrl.TrimEnd('/')}/{linkRelativo}";
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

    private static string MontarCorpoEmail(string nomeCompleto, string linkConfirmacao)
    {
        var nome = WebUtility.HtmlEncode(nomeCompleto);
        var link = WebUtility.HtmlEncode(linkConfirmacao);

        return $"""
            <p>Olá, {nome}.</p>
            <p>Foi solicitado o cadastro deste e-mail como e-mail de recuperação no Sistema Casa da Mulher.</p>
            <p>Depois de confirmado, ele poderá ser usado em fluxos futuros de recuperação de acesso.</p>
            <p>Para confirmar, clique no botão abaixo:</p>
            <p>
                <a href="{link}" style="display:inline-block;padding:12px 18px;background:#18726b;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;">
                    Confirmar e-mail de recuperação
                </a>
            </p>
            <p>Se o botão não abrir, copie e cole este link no navegador:</p>
            <p><a href="{link}">{link}</a></p>
            <p>Se você não solicitou esse cadastro, ignore esta mensagem ou entre em contato com a coordenação.</p>
            <p>Atenciosamente,<br>Casa da Mulher de Itaquaquecetuba</p>
            """;
    }
}
