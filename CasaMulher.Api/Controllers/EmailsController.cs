using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAcesso.SomenteAdm)]
[Route("api/emails")]
public class EmailsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public EmailsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailEventoResponse>>> Listar()
    {
        var eventos = await _dbContext.EmailEventos
            .Where(evento =>
                !evento.Tipo.StartsWith("Equipe")
                && !evento.Destinatario.EndsWith("@equipe.local"))
            .OrderByDescending(evento => evento.CriadoEm)
            .Take(200)
            .ToListAsync();

        return Ok(eventos.Select(Mapear));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmailEventoResponse>> ObterPorId(int id)
    {
        var evento = await _dbContext.EmailEventos.FindAsync(id);

        if (evento is null)
        {
            return NotFound(new { mensagem = "Evento de e-mail não encontrado." });
        }

        if (evento.Tipo.StartsWith("Equipe") || evento.Destinatario.EndsWith("@equipe.local"))
        {
            return NotFound(new { mensagem = "Evento de e-mail não encontrado." });
        }

        return Ok(Mapear(evento));
    }

    private static EmailEventoResponse Mapear(EmailEvento evento)
    {
        return new EmailEventoResponse
        {
            Id = evento.Id,
            Destinatario = evento.Destinatario,
            Assunto = evento.Assunto,
            Tipo = evento.Tipo,
            Status = evento.Status,
            Erro = evento.Erro,
            CriadoEm = evento.CriadoEm
        };
    }
}
