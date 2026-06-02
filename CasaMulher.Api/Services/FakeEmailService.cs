using CasaMulher.Api.Data;
using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services;

public class FakeEmailService : IEmailService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(AppDbContext dbContext, ILogger<FakeEmailService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string assunto, string corpoHtml, string tipo)
    {
        _logger.LogInformation(
            "E-mail simulado para {Destinatario}. Tipo: {Tipo}. Assunto: {Assunto}.",
            destinatario,
            tipo,
            assunto);

        _dbContext.EmailEventos.Add(new EmailEvento
        {
            Destinatario = destinatario.Trim(),
            Assunto = assunto.Trim(),
            Tipo = tipo.Trim(),
            Status = "Simulado",
            CriadoEm = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }
}
