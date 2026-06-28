using System.Net;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Services;

public class RedefinicaoSenhaEmailService : IRedefinicaoSenhaEmailService
{
    private const string TipoEmail = "RedefinicaoSenha";
    private const string Assunto = "Redefinição de senha - Sistema Casa da Mulher";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RedefinicaoSenhaEmailService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<ResultadoRedefinicaoSenhaEmail> EnviarAsync(ApplicationUser funcionario)
    {
        if (string.IsNullOrWhiteSpace(funcionario.Email))
        {
            return ResultadoRedefinicaoSenhaEmail.SemEmail();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(funcionario);
        var linkRelativo = GerarLinkRedefinicaoRelativo(funcionario.Email, token);
        var linkAbsoluto = GerarLinkAbsoluto(linkRelativo);

        if (string.IsNullOrWhiteSpace(linkAbsoluto))
        {
            return ResultadoRedefinicaoSenhaEmail.SemBaseUrl();
        }

        var corpoHtml = MontarCorpoEmail(funcionario.NomeCompleto, linkAbsoluto);

        try
        {
            await _emailService.EnviarAsync(funcionario.Email, Assunto, corpoHtml, TipoEmail);
            var status = await ObterUltimoStatusEmailAsync(funcionario.Email, Assunto, TipoEmail);

            return new ResultadoRedefinicaoSenhaEmail(true, status ?? "Enviado", null);
        }
        catch
        {
            var status = await ObterUltimoStatusEmailAsync(funcionario.Email, Assunto, TipoEmail) ?? "Falhou";

            return new ResultadoRedefinicaoSenhaEmail(
                false,
                status,
                "Não foi possível enviar o link de redefinição de senha. Confira a configuração de e-mail.");
        }
    }

    private static string GerarLinkRedefinicaoRelativo(string email, string token)
    {
        return $"redefinir-senha.html?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
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

    private static string MontarCorpoEmail(string nomeCompleto, string linkRedefinicao)
    {
        var nome = WebUtility.HtmlEncode(nomeCompleto);
        var link = WebUtility.HtmlEncode(linkRedefinicao);

        return $"""
            <div style="text-align: center; margin-bottom: 24px;">
                <img src="https://files.catbox.moe/ovf0uf.png" alt="Casa da Mulher de Itaquaquecetuba" style="height: 80px; width: auto;" />
            </div>
            <p>Olá, {nome}.</p>
            <p>Foi solicitada uma redefinição de senha para seu acesso ao Sistema Casa da Mulher.</p>
            <p>Para criar uma nova senha, clique no botão abaixo:</p>
            <p>
                <a href="{link}" style="display:inline-block;padding:12px 18px;background:#18726b;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;">
                    Redefinir minha senha
                </a>
            </p>
            <p>Se o botão não abrir, copie e cole este link no navegador:</p>
            <p><a href="{link}">{link}</a></p>
            <p>Se você não solicitou essa alteração, ignore esta mensagem ou entre em contato com a coordenação.</p>
            <p>Atenciosamente,<br>Casa da Mulher de Itaquaquecetuba</p>
            """;
    }
}
