using System.Security.Claims;
using System.Text.RegularExpressions;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/equipe")]
public class EquipeController : ControllerBase
{
    private static readonly Regex CodigoEquipeRegex = new(
        @"^EQP-\d{6}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConviteCodigoService _codigoService;
    private readonly IFuncionarioIdentificadorService _identificadorService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IEquipeGithubService _githubService;
    private readonly IMasterUserService _masterUserService;
    private readonly IWebHostEnvironment _environment;

    public EquipeController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConviteCodigoService codigoService,
        IFuncionarioIdentificadorService identificadorService,
        IAuditoriaService auditoriaService,
        IEquipeGithubService githubService,
        IMasterUserService masterUserService,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _codigoService = codigoService;
        _identificadorService = identificadorService;
        _auditoriaService = auditoriaService;
        _githubService = githubService;
        _masterUserService = masterUserService;
        _environment = environment;
    }

    [Authorize(Policy = PoliticasAcesso.AcessoEquipe)]
    [HttpGet("membros")]
    public async Task<ActionResult<IEnumerable<EquipeMembroResponse>>> ListarMembros()
    {
        var usuarioId = ObterUsuarioAtualId();
        var podeGerenciar = await PodeGerenciarMembrosEquipeAsync();
        var membros = await _dbContext.EquipeMembros
            .OrderBy(membro => membro.CodigoEquipe)
            .ToListAsync();

        return Ok(membros.Select(membro => MapearMembro(membro, usuarioId, podeGerenciar)));
    }

    [Authorize(Policy = PoliticasAcesso.AcessoEquipe)]
    [HttpPatch("membros/{id:int}")]
    public async Task<ActionResult<EquipeMembroResponse>> AtualizarMembro(int id, AtualizarEquipeMembroRequest request)
    {
        if (!await PodeGerenciarMembrosEquipeAsync())
        {
            return Forbid();
        }

        var membro = await _dbContext.EquipeMembros.FindAsync(id);

        if (membro is null)
        {
            return NotFound(new { mensagem = "Membro da equipe não encontrado." });
        }

        if (_masterUserService.EhEquipeOwnerPrincipal(membro.CodigoEquipe) && !await UsuarioAtualEhSuperAdminInstitucionalAsync())
        {
            return BadRequest(new { mensagem = "Somente super admin institucional pode alterar o owner principal da equipe." });
        }

        var papel = NormalizarPapel(request.PapelEquipe);
        var fluxo = NormalizarFluxo(request.FluxoTrabalho);

        if (!EquipePapeis.Todos.Contains(papel, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Papel de equipe inválido." });
        }

        if (!EquipeFluxosTrabalho.Todos.Contains(fluxo, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Fluxo de trabalho inválido." });
        }

        if (request.PodeCriarConvitesEquipe && !EquipePapeis.PodeGerenciarConvites(papel))
        {
            return BadRequest(new { mensagem = "Somente owner ou maintainer pode receber permissão para criar convites da equipe." });
        }

        var usuarioMembro = await _userManager.FindByIdAsync(membro.UserId);

        if (usuarioMembro is null)
        {
            return BadRequest(new { mensagem = "Usuário vinculado ao membro da equipe não encontrado." });
        }

        if (membro.PapelEquipe == EquipePapeis.Owner
            && (!request.Ativo || papel != EquipePapeis.Owner)
            && !await ExisteOutroOwnerOuAdmAtivoAsync(membro.UserId, membro.Id))
        {
            return BadRequest(new { mensagem = "Não é possível remover o último owner/super admin ativo." });
        }

        membro.PapelEquipe = papel;
        membro.PrecisaFork = request.PrecisaFork;
        membro.UsaCodespaces = request.UsaCodespaces;
        membro.FluxoTrabalho = fluxo;
        membro.GitHubUsername = NormalizarOpcional(request.GitHubUsername);
        membro.GitHubId = NormalizarOpcional(request.GitHubId);
        membro.ForkUrl = NormalizarOpcional(request.ForkUrl);
        membro.PodeCriarConvitesEquipe = request.PodeCriarConvitesEquipe;
        membro.Ativo = request.Ativo;
        membro.AtualizadoEm = DateTime.UtcNow;
        usuarioMembro.Ativo = request.Ativo;

        if (!string.IsNullOrWhiteSpace(membro.GitHubUsername) && membro.GitHubVinculadoEm is null)
        {
            membro.GitHubVinculadoEm = DateTime.UtcNow;
        }

        var updateUserResult = await _userManager.UpdateAsync(usuarioMembro);

        if (!updateUserResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível atualizar o usuário vinculado.",
                erros = updateUserResult.Errors.Select(error => error.Description)
            });
        }

        await _dbContext.SaveChangesAsync();
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_MEMBRO_ATUALIZADO",
            "EquipeMembro",
            membro.Id.ToString(),
            $"Atualizou membro {membro.CodigoEquipe} com papel {membro.PapelEquipe} e fluxo {membro.FluxoTrabalho}.");

        return Ok(MapearMembro(membro, ObterUsuarioAtualId(), true));
    }

    [Authorize(Policy = PoliticasAcesso.AcessoEquipe)]
    [HttpPost("membros/{id:int}/gerar-redefinicao-senha")]
    public async Task<ActionResult<GerarRedefinicaoSenhaEquipeResponse>> GerarRedefinicaoSenha(int id)
    {
        if (!await PodeGerenciarMembrosEquipeAsync())
        {
            return Forbid();
        }

        var membro = await _dbContext.EquipeMembros.FindAsync(id);

        if (membro is null)
        {
            return NotFound(new { mensagem = "Membro da equipe não encontrado." });
        }

        if (_masterUserService.EhEquipeOwnerPrincipal(membro.CodigoEquipe) && !await UsuarioAtualEhSuperAdminInstitucionalAsync())
        {
            return BadRequest(new { mensagem = "Somente o super admin institucional pode gerar uma redefinição para o owner principal." });
        }

        var codigo = await GerarCodigoRedefinicaoUnicoAsync();
        var reset = new EquipeSenhaReset
        {
            CodigoEquipe = membro.CodigoEquipe,
            CodigoHash = _codigoService.GerarHash(codigo),
            GeradoPorUserId = ObterUsuarioAtualId(),
            CriadoEm = DateTime.UtcNow,
            ExpiraEm = DateTime.UtcNow.AddHours(2)
        };

        _dbContext.EquipeSenhaResets.Add(reset);
        await _dbContext.SaveChangesAsync();
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_SENHA_REDEFINICAO_GERADA",
            "EquipeSenhaReset",
            reset.Id.ToString(),
            $"Gerou código de redefinição de senha para {membro.CodigoEquipe}.");

        return Ok(new GerarRedefinicaoSenhaEquipeResponse
        {
            CodigoEquipe = membro.CodigoEquipe,
            CodigoRedefinicao = codigo,
            ExpiraEm = reset.ExpiraEm
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.EquipeAtivacao)]
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenhaEquipe(RedefinirSenhaEquipeRequest request)
    {
        if (request.NovaSenha != request.ConfirmarNovaSenha)
        {
            return BadRequest(new { mensagem = "Nova senha e confirmação não conferem." });
        }

        var codigoEquipe = NormalizarCodigoEquipe(request.CodigoEquipe);
        var reset = await ObterResetSenhaPorCodigoAsync(codigoEquipe, request.CodigoRedefinicao);

        if (reset is null || reset.Usado || reset.Revogado || reset.ExpiraEm < DateTime.UtcNow)
        {
            await RegistrarAtivacaoFalhaAsync($"Tentativa de redefinição de senha EQP inválida para {codigoEquipe}.");
            return BadRequest(new { mensagem = "Código de redefinição inválido ou expirado." });
        }

        var usuario = await _dbContext.Users.SingleOrDefaultAsync(item =>
            item.IdentificadorFuncionario == codigoEquipe
            && item.Perfil == PerfisAcesso.Equipe);

        if (usuario is null || !usuario.Ativo)
        {
            return BadRequest(new { mensagem = "Conta da equipe não encontrada ou inativa." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var result = await _userManager.ResetPasswordAsync(usuario, token, request.NovaSenha);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível redefinir a senha.",
                erros = result.Errors.Select(error => error.Description)
            });
        }

        reset.Usado = true;
        reset.UsadoEm = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_SENHA_REDEFINIDA",
            "EquipeSenhaReset",
            reset.Id.ToString(),
            $"Senha redefinida para {codigoEquipe} por código de uso único.");

        return Ok(new { mensagem = "Senha da equipe redefinida com sucesso." });
    }

    [Authorize(Policy = PoliticasAcesso.AcessoEquipe)]
    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<AuditoriaEventoResponse>>> ListarLogsEquipe()
    {
        var usuarioId = ObterUsuarioAtualId();
        var podeVerTodos = await PodeVerTodosLogsEquipeAsync();
        var eventos = await _dbContext.AuditoriaEventos
            .Where(evento =>
                evento.PerfilFuncionario == PerfisAcesso.Equipe
                || evento.Acao.StartsWith("EQUIPE_"))
            .Where(evento => podeVerTodos || evento.UsuarioId == usuarioId)
            .OrderByDescending(evento => evento.CriadoEm)
            .Take(200)
            .ToListAsync();

        return Ok(eventos.Select(MapearAuditoria));
    }

    [Authorize(Policy = PoliticasAcesso.AcessoEquipe)]
    [HttpGet("github/status")]
    public ActionResult<EquipeGithubStatusResponse> ObterGithubStatus()
    {
        return Ok(_githubService.ObterStatus());
    }

    [Authorize(Policy = PoliticasAcesso.AcessoEquipe)]
    [HttpGet("github/atividade")]
    public async Task<ActionResult<EquipeGithubAtividadeResponse>> ObterGithubAtividade(CancellationToken cancellationToken)
    {
        return Ok(await _githubService.ObterAtividadeAsync(cancellationToken));
    }

    [Authorize(Policy = PoliticasAcesso.GerenciarConvitesEquipe)]
    [HttpGet("convites")]
    public async Task<ActionResult<IEnumerable<EquipeConviteResponse>>> ListarConvites()
    {
        if (!await PodeGerenciarConvitesEquipeAsync())
        {
            return Forbid();
        }

        var convites = await _dbContext.EquipeConvites
            .OrderByDescending(convite => convite.CriadoEm)
            .ToListAsync();

        return Ok(convites.Select(MapearConvite));
    }

    [Authorize(Policy = PoliticasAcesso.GerenciarConvitesEquipe)]
    [HttpGet("convites/{id:int}")]
    public async Task<ActionResult<EquipeConviteResponse>> ObterConvite(int id)
    {
        if (!await PodeGerenciarConvitesEquipeAsync())
        {
            return Forbid();
        }

        var convite = await _dbContext.EquipeConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite de equipe não encontrado." });
        }

        return Ok(MapearConvite(convite));
    }

    [Authorize(Policy = PoliticasAcesso.GerenciarConvitesEquipe)]
    [HttpPost("convites")]
    public async Task<ActionResult<EquipeConviteCriadoResponse>> CriarConvite(CriarEquipeConviteRequest request)
    {
        if (!await PodeGerenciarConvitesEquipeAsync())
        {
            return Forbid();
        }

        var erro = ValidarConfiguracaoConvite(request);

        if (erro is not null)
        {
            return erro;
        }

        var (convite, codigoAtivacao) = await CriarConvitePersistidoAsync(request);

        await RegistrarConviteCriadoAsync(convite);

        var response = MapearConviteCriado(convite, codigoAtivacao);
        return CreatedAtAction(nameof(ObterConvite), new { id = convite.Id }, response);
    }

    [Authorize(Policy = PoliticasAcesso.GerenciarConvitesEquipe)]
    [HttpPost("convites/lote")]
    public async Task<ActionResult<EquipeConvitesLoteResponse>> CriarConvitesLote(CriarEquipeConvitesLoteRequest request)
    {
        if (!await PodeGerenciarConvitesEquipeAsync())
        {
            return Forbid();
        }

        var erro = ValidarConfiguracaoConvite(request);

        if (erro is not null)
        {
            return erro;
        }

        var convites = new List<EquipeConviteCriadoResponse>();

        for (var index = 0; index < request.Quantidade; index++)
        {
            var (convite, codigoAtivacao) = await CriarConvitePersistidoAsync(request);
            convites.Add(MapearConviteCriado(convite, codigoAtivacao));

            await RegistrarConviteCriadoAsync(convite);
        }

        await _auditoriaService.RegistrarAsync(
            "EQUIPE_CONVITE_LOTE_CRIADO",
            "EquipeConvite",
            null,
            $"Criou lote com {convites.Count} convite(s) de equipe.");

        return Ok(new EquipeConvitesLoteResponse
        {
            Convites = convites
        });
    }

    [AllowAnonymous]
    [HttpPost("convites/bootstrap")]
    public async Task<ActionResult<BootstrapEquipeResponse>> BootstrapConvites(BootstrapEquipeRequest request)
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return NotFound(new { mensagem = "Bootstrap da equipe disponível apenas em Development/Staging." });
        }

        var quantidadeIntegrantes = Math.Clamp(request.QuantidadeIntegrantes, 1, 20);
        var respostas = new List<BootstrapEquipeConviteResponse>
        {
            await PrepararConviteBootstrapAsync(
                _masterUserService.EquipeOwnerCodigo,
                EquipePapeis.Owner,
                precisaFork: false,
                usaCodespaces: false,
                fluxoTrabalho: EquipeFluxosTrabalho.LocalOwner,
                podeCriarConvitesEquipe: true,
                observacao: "Allan/mantenedor",
                regenerarDisponiveis: request.RegenerarCodigosDisponiveis)
        };

        for (var index = 2; index <= quantidadeIntegrantes + 1; index++)
        {
            respostas.Add(await PrepararConviteBootstrapAsync(
                $"EQP-{index:000000}",
                EquipePapeis.Contributor,
                precisaFork: true,
                usaCodespaces: true,
                fluxoTrabalho: EquipeFluxosTrabalho.ForkCodespaces,
                podeCriarConvitesEquipe: false,
                observacao: $"integrante {index - 1}",
                regenerarDisponiveis: request.RegenerarCodigosDisponiveis));
        }

        await _auditoriaService.RegistrarAsync(
            "EQUIPE_BOOTSTRAP_CONVITES",
            "EquipeConvite",
            null,
            $"Executou bootstrap de convites da equipe em {_environment.EnvironmentName}.");

        return Ok(new BootstrapEquipeResponse
        {
            Ambiente = _environment.EnvironmentName,
            Convites = respostas
        });
    }

    [Authorize(Policy = PoliticasAcesso.GerenciarConvitesEquipe)]
    [HttpPost("convites/{id:int}/revogar")]
    public async Task<ActionResult<EquipeConviteResponse>> RevogarConvite(int id)
    {
        if (!await PodeGerenciarConvitesEquipeAsync())
        {
            return Forbid();
        }

        var convite = await _dbContext.EquipeConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite de equipe não encontrado." });
        }

        if (!string.Equals(convite.Status, EquipeConviteStatus.Disponivel, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Apenas convites disponíveis podem ser revogados." });
        }

        convite.Status = EquipeConviteStatus.Revogado;
        convite.RevogadoEm = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_CONVITE_REVOGADO",
            "EquipeConvite",
            convite.Id.ToString(),
            $"Revogou convite de equipe {convite.CodigoEquipe}.");

        return Ok(MapearConvite(convite));
    }

    [Authorize(Policy = PoliticasAcesso.GerenciarConvitesEquipe)]
    [HttpPost("convites/{id:int}/regenerar-codigo")]
    public async Task<ActionResult<EquipeConviteCriadoResponse>> RegenerarCodigo(int id)
    {
        if (!await PodeGerenciarConvitesEquipeAsync())
        {
            return Forbid();
        }

        var convite = await _dbContext.EquipeConvites.FindAsync(id);

        if (convite is null)
        {
            return NotFound(new { mensagem = "Convite de equipe não encontrado." });
        }

        if (!string.Equals(convite.Status, EquipeConviteStatus.Disponivel, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Apenas convites disponíveis podem ter o código regenerado." });
        }

        var codigoAtivacao = await GerarCodigoAtivacaoUnicoAsync();
        convite.CodigoAtivacaoHash = _codigoService.GerarHash(codigoAtivacao);

        await _dbContext.SaveChangesAsync();
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_CONVITE_CODIGO_REGENERADO",
            "EquipeConvite",
            convite.Id.ToString(),
            $"Regenerou o código de ativação do convite {convite.CodigoEquipe}.");

        return Ok(MapearConviteCriado(convite, codigoAtivacao));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.EquipeAtivacao)]
    [HttpPost("ativar")]
    public async Task<ActionResult<AtivarEquipeConviteResponse>> AtivarConvite(AtivarEquipeConviteRequest request)
    {
        if (request.Senha != request.ConfirmarSenha)
        {
            return BadRequest(new { mensagem = "Senha e confirmação de senha não conferem." });
        }

        var codigoEquipe = NormalizarCodigoEquipe(request.CodigoEquipe);
        var nomeCompleto = request.NomeCompleto.Trim();

        if (!CodigoEquipeRegex.IsMatch(codigoEquipe) || string.IsNullOrWhiteSpace(nomeCompleto))
        {
            await RegistrarAtivacaoFalhaAsync("Tentativa de ativação EQP com dados obrigatórios inválidos.");
            return BadRequest(new { mensagem = "Convite de equipe inválido ou indisponível." });
        }

        var convite = await _dbContext.EquipeConvites
            .SingleOrDefaultAsync(item => item.CodigoEquipe == codigoEquipe);

        if (!ConvitePodeSerAtivado(convite)
            || !_codigoService.CodigoCorresponde(request.CodigoAtivacao, convite!.CodigoAtivacaoHash))
        {
            await RegistrarAtivacaoFalhaAsync($"Tentativa de ativação EQP recusada para {codigoEquipe}.");
            return BadRequest(new { mensagem = "Convite de equipe inválido ou indisponível." });
        }

        if (await IdentificadorEquipeJaEstaEmUsoAsync(codigoEquipe))
        {
            await RegistrarAtivacaoFalhaAsync($"Tentativa de ativação EQP duplicada para {codigoEquipe}.");
            return BadRequest(new { mensagem = "Convite de equipe inválido ou indisponível." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        if (!await _roleManager.RoleExistsAsync(PerfisAcesso.Equipe))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(PerfisAcesso.Equipe));

            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    mensagem = "Não foi possível preparar o perfil da equipe.",
                    erros = roleResult.Errors.Select(error => error.Description)
                });
            }
        }

        var usuario = new ApplicationUser
        {
            NomeCompleto = nomeCompleto,
            Email = CriarEmailTecnicoEquipe(codigoEquipe),
            UserName = codigoEquipe,
            IdentificadorFuncionario = codigoEquipe,
            Perfil = PerfisAcesso.Equipe,
            EmailConfirmed = true,
            Ativo = true,
            DoisFatoresObrigatorio = false
        };

        var createResult = await _userManager.CreateAsync(usuario, request.Senha);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível ativar a conta da equipe.",
                erros = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleAssignResult = await _userManager.AddToRoleAsync(usuario, PerfisAcesso.Equipe);

        if (!roleAssignResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível vincular o perfil da equipe.",
                erros = roleAssignResult.Errors.Select(error => error.Description)
            });
        }

        _dbContext.EquipeMembros.Add(new EquipeMembro
        {
            UserId = usuario.Id,
            CodigoEquipe = codigoEquipe,
            Nome = nomeCompleto,
            PapelEquipe = convite.PapelEquipe,
            PrecisaFork = convite.PrecisaFork,
            UsaCodespaces = convite.UsaCodespaces,
            FluxoTrabalho = convite.FluxoTrabalho,
            PodeCriarConvitesEquipe = convite.PodeCriarConvitesEquipe,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        });

        convite.Status = EquipeConviteStatus.Usado;
        convite.UsadoPorUserId = usuario.Id;
        convite.NomeInformado = nomeCompleto;
        convite.UsadoEm = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            "EQUIPE_CONVITE_ATIVADO",
            "EquipeConvite",
            convite.Id.ToString(),
            $"Convite de equipe {codigoEquipe} ativado para {nomeCompleto}.");
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_MEMBRO_CRIADO",
            "EquipeMembro",
            usuario.Id,
            $"Membro de equipe {codigoEquipe} criado com papel {convite.PapelEquipe}.");

        await transaction.CommitAsync();

        return Ok(new AtivarEquipeConviteResponse
        {
            Mensagem = "Conta da equipe ativada com sucesso.",
            IdentificadorFuncionario = codigoEquipe,
            Perfil = PerfisAcesso.Equipe
        });
    }

    private async Task<BootstrapEquipeConviteResponse> PrepararConviteBootstrapAsync(
        string codigoEquipe,
        string papelEquipe,
        bool precisaFork,
        bool usaCodespaces,
        string fluxoTrabalho,
        bool podeCriarConvitesEquipe,
        string observacao,
        bool regenerarDisponiveis)
    {
        var codigoNormalizado = NormalizarCodigoEquipe(codigoEquipe);

        if (await ContaEquipeAtivadaAsync(codigoNormalizado))
        {
            return new BootstrapEquipeConviteResponse
            {
                CodigoEquipe = codigoNormalizado,
                PapelEquipe = papelEquipe,
                Status = "Ativado",
                Observacao = $"{observacao}: conta já ativada.",
                Ativado = true
            };
        }

        var convite = await _dbContext.EquipeConvites
            .SingleOrDefaultAsync(item => item.CodigoEquipe == codigoNormalizado);

        if (convite is not null
            && !string.Equals(convite.Status, EquipeConviteStatus.Disponivel, StringComparison.OrdinalIgnoreCase))
        {
            return new BootstrapEquipeConviteResponse
            {
                CodigoEquipe = convite.CodigoEquipe,
                PapelEquipe = convite.PapelEquipe,
                Status = convite.Status,
                Observacao = $"{observacao}: convite existente com status {convite.Status}.",
                Ativado = string.Equals(convite.Status, EquipeConviteStatus.Usado, StringComparison.OrdinalIgnoreCase)
            };
        }

        var codigoAtivacao = convite is null || regenerarDisponiveis
            ? await GerarCodigoAtivacaoUnicoAsync()
            : null;

        if (convite is null)
        {
            convite = new EquipeConvite
            {
                CodigoEquipe = codigoNormalizado,
                CodigoAtivacaoHash = _codigoService.GerarHash(codigoAtivacao!),
                Status = EquipeConviteStatus.Disponivel,
                PapelEquipe = NormalizarPapel(papelEquipe),
                PrecisaFork = precisaFork,
                UsaCodespaces = usaCodespaces,
                FluxoTrabalho = NormalizarFluxo(fluxoTrabalho),
                PodeCriarConvitesEquipe = podeCriarConvitesEquipe,
                Observacao = observacao,
                CriadoEm = DateTime.UtcNow
            };

            AplicarPadraoOwnerPrincipal(convite);
            _dbContext.EquipeConvites.Add(convite);
            await _dbContext.SaveChangesAsync();

            return MapearConviteBootstrap(convite, codigoAtivacao, observacao, criado: true, regenerado: false);
        }

        convite.PapelEquipe = NormalizarPapel(papelEquipe);
        convite.PrecisaFork = precisaFork;
        convite.UsaCodespaces = usaCodespaces;
        convite.FluxoTrabalho = NormalizarFluxo(fluxoTrabalho);
        convite.PodeCriarConvitesEquipe = podeCriarConvitesEquipe;
        convite.Observacao = observacao;
        AplicarPadraoOwnerPrincipal(convite);

        if (!string.IsNullOrWhiteSpace(codigoAtivacao))
        {
            convite.CodigoAtivacaoHash = _codigoService.GerarHash(codigoAtivacao);
        }

        await _dbContext.SaveChangesAsync();

        return MapearConviteBootstrap(
            convite,
            codigoAtivacao,
            observacao,
            criado: false,
            regenerado: !string.IsNullOrWhiteSpace(codigoAtivacao));
    }

    private async Task<(EquipeConvite Convite, string CodigoAtivacao)> CriarConvitePersistidoAsync(
        CriarEquipeConviteRequest request)
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var codigoEquipe = await _identificadorService.GerarProximoAsync(PerfisAcesso.Equipe);
            var codigoAtivacao = await GerarCodigoAtivacaoUnicoAsync();
            var convite = new EquipeConvite
            {
                CodigoEquipe = codigoEquipe,
                CodigoAtivacaoHash = _codigoService.GerarHash(codigoAtivacao),
                Status = EquipeConviteStatus.Disponivel,
                CriadoPorUserId = ObterUsuarioAtualId(),
                PapelEquipe = NormalizarPapel(request.PapelEquipe),
                PrecisaFork = request.PrecisaFork,
                UsaCodespaces = request.UsaCodespaces,
                FluxoTrabalho = NormalizarFluxo(request.FluxoTrabalho),
                PodeCriarConvitesEquipe = request.PodeCriarConvitesEquipe,
                Observacao = NormalizarObservacao(request.Observacao),
                CriadoEm = DateTime.UtcNow
            };

            _dbContext.EquipeConvites.Add(convite);
            AplicarPadraoOwnerPrincipal(convite);

            try
            {
                await _dbContext.SaveChangesAsync();
                return (convite, codigoAtivacao);
            }
            catch (DbUpdateException)
            {
                _dbContext.Entry(convite).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um convite de equipe único.");
    }

    private async Task<string> GerarCodigoAtivacaoUnicoAsync()
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var codigo = _codigoService.GerarCodigoAtivacaoEquipe();
            var hash = _codigoService.GerarHash(codigo);
            var existe = await _dbContext.EquipeConvites.AnyAsync(convite => convite.CodigoAtivacaoHash == hash);

            if (!existe)
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código de ativação único.");
    }

    private async Task RegistrarConviteCriadoAsync(EquipeConvite convite)
    {
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_CONVITE_CRIADO",
            "EquipeConvite",
            convite.Id.ToString(),
            $"Criou convite de equipe {convite.CodigoEquipe} com papel {convite.PapelEquipe}.");
    }

    private async Task<bool> PodeGerenciarConvitesEquipeAsync()
    {
        if (await UsuarioAtualEhMasterAsync())
        {
            return true;
        }

        var usuarioId = ObterUsuarioAtualId();

        if (string.IsNullOrWhiteSpace(usuarioId) || !User.IsInRole(PerfisAcesso.Equipe))
        {
            return false;
        }

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuarioId
            && membro.Ativo
            && membro.PodeCriarConvitesEquipe
            && (membro.PapelEquipe == EquipePapeis.Owner || membro.PapelEquipe == EquipePapeis.Maintainer));
    }

    private async Task<bool> PodeGerenciarMembrosEquipeAsync()
    {
        if (await UsuarioAtualEhMasterAsync())
        {
            return true;
        }

        var usuarioId = ObterUsuarioAtualId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuarioId
            && membro.Ativo
            && membro.PapelEquipe == EquipePapeis.Owner);
    }

    private async Task<bool> PodeVerTodosLogsEquipeAsync()
    {
        if (await UsuarioAtualEhMasterAsync())
        {
            return true;
        }

        var usuarioId = ObterUsuarioAtualId();

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuarioId
            && membro.Ativo
            && (membro.PapelEquipe == EquipePapeis.Owner || membro.PapelEquipe == EquipePapeis.Maintainer));
    }

    private async Task<bool> UsuarioAtualEhSuperAdminInstitucionalAsync()
    {
        var usuarioId = ObterUsuarioAtualId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        var usuario = await _userManager.FindByIdAsync(usuarioId);
        return _masterUserService.EhSuperAdminInstitucional(usuario);
    }

    private async Task<bool> UsuarioAtualEhMasterAsync()
    {
        if (await UsuarioAtualEhSuperAdminInstitucionalAsync())
        {
            return true;
        }

        var usuarioId = ObterUsuarioAtualId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        return await _dbContext.EquipeMembros.AnyAsync(membro =>
            membro.UserId == usuarioId
            && membro.Ativo
            && membro.PapelEquipe == EquipePapeis.Owner
            && membro.CodigoEquipe == _masterUserService.EquipeOwnerCodigo);
    }

    private async Task<bool> ExisteOutroOwnerOuAdmAtivoAsync(string userId, int membroId)
    {
        var usuariosAdmAtivos = await _dbContext.Users
            .Where(usuario =>
            usuario.Id != userId
            && usuario.Ativo
            && usuario.Perfil == PerfisAcesso.Adm)
            .ToListAsync();

        var existeAdmAtivo = usuariosAdmAtivos.Any(_masterUserService.EhSuperAdminInstitucional);

        if (existeAdmAtivo)
        {
            return true;
        }

        return await _dbContext.EquipeMembros
            .Where(membro =>
                membro.Id != membroId
                && membro.Ativo
                && membro.PapelEquipe == EquipePapeis.Owner)
            .Join(
                _dbContext.Users,
                membro => membro.UserId,
                usuario => usuario.Id,
                (_, usuario) => usuario)
            .AnyAsync(usuario => usuario.Ativo);
    }

    private async Task<bool> IdentificadorEquipeJaEstaEmUsoAsync(string codigoEquipe)
    {
        return await _dbContext.Users.AnyAsync(usuario =>
                usuario.IdentificadorFuncionario == codigoEquipe
                || usuario.NormalizedUserName == codigoEquipe)
            || await _dbContext.EquipeMembros.AnyAsync(membro => membro.CodigoEquipe == codigoEquipe);
    }

    private async Task<bool> ContaEquipeAtivadaAsync(string codigoEquipe)
    {
        return await _dbContext.Users.AnyAsync(usuario =>
                usuario.IdentificadorFuncionario == codigoEquipe
                && usuario.Perfil == PerfisAcesso.Equipe)
            || await _dbContext.EquipeMembros.AnyAsync(membro => membro.CodigoEquipe == codigoEquipe);
    }

    private async Task<string> GerarCodigoRedefinicaoUnicoAsync()
    {
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var codigo = _codigoService.GerarCodigoAtivacaoEquipe();
            var hash = _codigoService.GerarHash(codigo);
            var existe = await _dbContext.EquipeSenhaResets.AnyAsync(reset => reset.CodigoHash == hash);

            if (!existe)
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código único de redefinição.");
    }

    private async Task<EquipeSenhaReset?> ObterResetSenhaPorCodigoAsync(string codigoEquipe, string codigoRedefinicao)
    {
        if (string.IsNullOrWhiteSpace(codigoEquipe) || string.IsNullOrWhiteSpace(codigoRedefinicao))
        {
            return null;
        }

        var hash = _codigoService.GerarHash(codigoRedefinicao);
        var reset = await _dbContext.EquipeSenhaResets
            .SingleOrDefaultAsync(item => item.CodigoEquipe == codigoEquipe && item.CodigoHash == hash);

        if (reset is null || !_codigoService.CodigoCorresponde(codigoRedefinicao, reset.CodigoHash))
        {
            return null;
        }

        return reset;
    }

    private static bool ConvitePodeSerAtivado(EquipeConvite? convite)
    {
        return convite is not null
            && string.Equals(convite.Status, EquipeConviteStatus.Disponivel, StringComparison.OrdinalIgnoreCase)
            && convite.UsadoEm is null
            && convite.RevogadoEm is null;
    }

    private void AplicarPadraoOwnerPrincipal(EquipeConvite convite)
    {
        if (!_masterUserService.EhEquipeOwnerPrincipal(convite.CodigoEquipe))
        {
            return;
        }

        convite.PapelEquipe = EquipePapeis.Owner;
        convite.PrecisaFork = false;
        convite.UsaCodespaces = false;
        convite.FluxoTrabalho = EquipeFluxosTrabalho.LocalOwner;
        convite.PodeCriarConvitesEquipe = true;
    }

    private ActionResult? ValidarConfiguracaoConvite(CriarEquipeConviteRequest request)
    {
        var papel = NormalizarPapel(request.PapelEquipe);

        if (!EquipePapeis.Todos.Contains(papel, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Papel de equipe inválido." });
        }

        if (request.PodeCriarConvitesEquipe && !EquipePapeis.PodeGerenciarConvites(papel))
        {
            return BadRequest(new { mensagem = "Somente owner ou maintainer pode receber permissão para criar convites da equipe." });
        }

        var fluxo = NormalizarFluxo(request.FluxoTrabalho);

        if (!EquipeFluxosTrabalho.Todos.Contains(fluxo, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "Fluxo de trabalho inválido." });
        }

        return null;
    }

    private async Task RegistrarAtivacaoFalhaAsync(string descricao)
    {
        await _auditoriaService.RegistrarAsync(
            "EQUIPE_CONVITE_ATIVACAO_FALHA",
            "EquipeConvite",
            null,
            descricao);
    }

    private string ObterUsuarioAtualId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private static string NormalizarCodigoEquipe(string codigoEquipe)
    {
        return codigoEquipe.Trim().ToUpperInvariant();
    }

    private static string NormalizarPapel(string papel)
    {
        return string.IsNullOrWhiteSpace(papel)
            ? EquipePapeis.Contributor
            : papel.Trim().ToLowerInvariant();
    }

    private static string NormalizarFluxo(string fluxo)
    {
        return string.IsNullOrWhiteSpace(fluxo)
            ? EquipeFluxosTrabalho.ForkCodespaces
            : fluxo.Trim().ToLowerInvariant();
    }

    private static string? NormalizarOpcional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizarObservacao(string? observacao)
    {
        return string.IsNullOrWhiteSpace(observacao)
            ? null
            : observacao.Trim();
    }

    private static string CriarEmailTecnicoEquipe(string codigoEquipe)
    {
        return $"{codigoEquipe.ToLowerInvariant()}@equipe.local";
    }

    private static AuditoriaEventoResponse MapearAuditoria(AuditoriaEvento evento)
    {
        return new AuditoriaEventoResponse
        {
            Id = evento.Id,
            UsuarioId = evento.UsuarioId,
            IdentificadorFuncionario = evento.IdentificadorFuncionario,
            NomeFuncionario = evento.NomeFuncionario,
            PerfilFuncionario = evento.PerfilFuncionario,
            Acao = evento.Acao,
            Entidade = evento.Entidade,
            EntidadeId = evento.EntidadeId,
            Descricao = evento.Descricao,
            IpOrigem = evento.IpOrigem,
            UserAgent = evento.UserAgent,
            CriadoEm = evento.CriadoEm
        };
    }

    private static EquipeMembroResponse MapearMembro(EquipeMembro membro, string usuarioAtualId, bool podeGerenciar)
    {
        var ehVoce = string.Equals(membro.UserId, usuarioAtualId, StringComparison.Ordinal);

        return new EquipeMembroResponse
        {
            Id = membro.Id,
            UserId = membro.UserId,
            CodigoEquipe = membro.CodigoEquipe,
            Nome = membro.Nome,
            PapelEquipe = membro.PapelEquipe,
            PrecisaFork = membro.PrecisaFork,
            UsaCodespaces = membro.UsaCodespaces,
            FluxoTrabalho = membro.FluxoTrabalho,
            GitHubUsername = membro.GitHubUsername,
            GitHubId = podeGerenciar || ehVoce ? membro.GitHubId : null,
            GitHubVinculadoEm = membro.GitHubVinculadoEm,
            ForkUrl = membro.ForkUrl,
            UltimaVerificacaoGitHubEm = membro.UltimaVerificacaoGitHubEm,
            PodeCriarConvitesEquipe = membro.PodeCriarConvitesEquipe,
            Ativo = membro.Ativo,
            CriadoEm = membro.CriadoEm,
            AtualizadoEm = membro.AtualizadoEm,
            PodeEditar = podeGerenciar,
            PodeGerarResetSenha = podeGerenciar,
            EhVoce = ehVoce
        };
    }

    private static EquipeConviteResponse MapearConvite(EquipeConvite convite)
    {
        return new EquipeConviteResponse
        {
            Id = convite.Id,
            CodigoEquipe = convite.CodigoEquipe,
            Status = convite.Status,
            NomeInformado = convite.NomeInformado,
            PapelEquipe = convite.PapelEquipe,
            PrecisaFork = convite.PrecisaFork,
            UsaCodespaces = convite.UsaCodespaces,
            FluxoTrabalho = convite.FluxoTrabalho,
            PodeCriarConvitesEquipe = convite.PodeCriarConvitesEquipe,
            CriadoEm = convite.CriadoEm,
            UsadoEm = convite.UsadoEm,
            RevogadoEm = convite.RevogadoEm,
            Observacao = convite.Observacao
        };
    }

    private static EquipeConviteCriadoResponse MapearConviteCriado(EquipeConvite convite, string codigoAtivacao)
    {
        return new EquipeConviteCriadoResponse
        {
            Id = convite.Id,
            CodigoEquipe = convite.CodigoEquipe,
            Status = convite.Status,
            NomeInformado = convite.NomeInformado,
            PapelEquipe = convite.PapelEquipe,
            PrecisaFork = convite.PrecisaFork,
            UsaCodespaces = convite.UsaCodespaces,
            FluxoTrabalho = convite.FluxoTrabalho,
            PodeCriarConvitesEquipe = convite.PodeCriarConvitesEquipe,
            CriadoEm = convite.CriadoEm,
            UsadoEm = convite.UsadoEm,
            RevogadoEm = convite.RevogadoEm,
            Observacao = convite.Observacao,
            CodigoAtivacao = codigoAtivacao
        };
    }

    private static BootstrapEquipeConviteResponse MapearConviteBootstrap(
        EquipeConvite convite,
        string? codigoAtivacao,
        string observacao,
        bool criado,
        bool regenerado)
    {
        return new BootstrapEquipeConviteResponse
        {
            CodigoEquipe = convite.CodigoEquipe,
            CodigoAtivacao = codigoAtivacao,
            PapelEquipe = convite.PapelEquipe,
            Status = convite.Status,
            Observacao = observacao,
            Criado = criado,
            Regenerado = regenerado,
            Ativado = false
        };
    }
}
