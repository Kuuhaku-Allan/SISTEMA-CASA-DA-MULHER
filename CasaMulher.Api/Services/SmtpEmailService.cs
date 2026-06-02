using System.Net;
using System.Net.Mail;
using CasaMulher.Api.Data;
using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml, string tipo)
    {
        try
        {
            await EnviarSmtpAsync(destinatario, assunto, corpoHtml);
            await RegistrarEventoAsync(destinatario, assunto, tipo, "Enviado", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Destinatario}. Tipo: {Tipo}.", destinatario, tipo);
            await RegistrarEventoAsync(destinatario, assunto, tipo, "Falhou", ex.Message);
            throw;
        }
    }

    private async Task EnviarSmtpAsync(string destinatario, string assunto, string corpoHtml)
    {
        var host = _configuration["Email:Smtp:Host"];
        var fromEmail = _configuration["Email:Smtp:FromEmail"];
        var fromName = _configuration["Email:Smtp:FromName"] ?? "Casa da Mulher";

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Configure Email:Smtp:Host e Email:Smtp:FromEmail para envio SMTP.");
        }

        var port = _configuration.GetValue("Email:Smtp:Port", 587);
        var enableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true);
        var user = _configuration["Email:Smtp:User"];
        var password = _configuration["Email:Smtp:Password"];

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = assunto,
            Body = corpoHtml,
            IsBodyHtml = true
        };
        message.To.Add(destinatario);

#pragma warning disable SYSLIB0014
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };
#pragma warning restore SYSLIB0014

        if (!string.IsNullOrWhiteSpace(user))
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Configure Email:Smtp:Password quando Email:Smtp:User estiver definido.");
            }

            client.Credentials = new NetworkCredential(user, password);
        }

        await client.SendMailAsync(message);
    }

    private async Task RegistrarEventoAsync(string destinatario, string assunto, string tipo, string status, string? erro)
    {
        _dbContext.EmailEventos.Add(new EmailEvento
        {
            Destinatario = destinatario.Trim(),
            Assunto = assunto.Trim(),
            Tipo = tipo.Trim(),
            Status = status,
            Erro = erro,
            CriadoEm = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }
}
