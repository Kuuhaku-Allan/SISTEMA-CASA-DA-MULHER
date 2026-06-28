using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Authorize(Policy = PoliticasAcesso.SomenteAdm)]
[Route("api/auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AuditoriaController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditoriaEventoResponse>>> Listar()
    {
        var eventos = await _dbContext.AuditoriaEventos
            .Where(evento => evento.Escopo == AuditoriaEscopos.Institucional)
            .OrderByDescending(evento => evento.CriadoEm)
            .Take(200)
            .ToListAsync();

        return Ok(eventos.Select(Mapear));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditoriaEventoResponse>> ObterPorId(int id)
    {
        var evento = await _dbContext.AuditoriaEventos.FindAsync(id);

        if (evento is null)
        {
            return NotFound(new { mensagem = "Evento de auditoria não encontrado." });
        }

        if (evento.Escopo != AuditoriaEscopos.Institucional)
        {
            return NotFound(new { mensagem = "Evento de auditoria nao encontrado." });
        }

        return Ok(Mapear(evento));
    }

    [HttpGet("funcionarios/{usuarioId}")]
    public async Task<ActionResult<IEnumerable<AuditoriaEventoResponse>>> ListarPorFuncionario(string usuarioId)
    {
        var eventos = await _dbContext.AuditoriaEventos
            .Where(evento => evento.UsuarioId == usuarioId || evento.EntidadeId == usuarioId)
            .Where(evento => evento.Escopo == AuditoriaEscopos.Institucional)
            .OrderByDescending(evento => evento.CriadoEm)
            .Take(200)
            .ToListAsync();

        return Ok(eventos.Select(Mapear));
    }

    private static AuditoriaEventoResponse Mapear(AuditoriaEvento evento)
    {
        return new AuditoriaEventoResponse
        {
            Id = evento.Id,
            UsuarioId = evento.UsuarioId,
            IdentificadorFuncionario = evento.IdentificadorFuncionario,
            NomeFuncionario = evento.NomeFuncionario,
            PerfilFuncionario = evento.PerfilFuncionario,
            Escopo = evento.Escopo,
            Acao = evento.Acao,
            Entidade = evento.Entidade,
            EntidadeId = evento.EntidadeId,
            Descricao = evento.Descricao,
            IpOrigem = evento.IpOrigem,
            UserAgent = evento.UserAgent,
            CriadoEm = evento.CriadoEm
        };
    }
}
