using System.Linq;
using System.Threading.Tasks;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CasaMulher.Api.Controllers
{
    [ApiController]
    [Route("api/equipe-ide/github")]
    [Authorize]
    public class EquipeIdeGitHubController : ControllerBase
    {
        private readonly IGitHubIdeService _githubService;
        private readonly IGitHubUsuarioService _usuarioService;
        private readonly IGitHubForkIdeService _forkService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EquipeIdeGitHubController> _logger;
        private readonly IAuditoriaService _auditoriaService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public EquipeIdeGitHubController(
            IGitHubIdeService githubService, 
            IGitHubUsuarioService usuarioService,
            IGitHubForkIdeService forkService,
            UserManager<ApplicationUser> userManager,
            ILogger<EquipeIdeGitHubController> logger,
            IAuditoriaService auditoriaService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _githubService = githubService;
            _usuarioService = usuarioService;
            _forkService = forkService;
            _userManager = userManager;
            _logger = logger;
            _auditoriaService = auditoriaService;
            _configuration = configuration;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _githubService.ObterStatusAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditoriaService.RegistrarAsync("IDE_GITHUB_STATUS_CONSULTADO", "GitHubIde", null, "Status consultado", user.IdentificadorFuncionario);
            }
            return Ok(status);
        }

        [HttpGet("conexao/status")]
        public async Task<IActionResult> GetConexaoStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var status = await _usuarioService.ObterStatusConexaoAsync(user);
            return Ok(status);
        }

        [HttpGet("conectar")]
        public async Task<IActionResult> Conectar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var url = await _usuarioService.CriarUrlAutorizacaoAsync(user, ip, userAgent);
            return Ok(new { url });
        }

        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5500/projetocasadamulher/telas";
            var urlBase = baseUrl.TrimEnd('/');
            
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Redirect($"{urlBase}/equipe-ide.html?github=erro");
            }

            try
            {
                await _usuarioService.ProcessarCallbackAsync(code, state);
                return Redirect($"{urlBase}/equipe-ide.html?github=conectado");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro no callback do GitHub OAuth");
                return Redirect($"{urlBase}/equipe-ide.html?github=erro");
            }
        }

        [HttpDelete("conexao")]
        public async Task<IActionResult> Desconectar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            await _usuarioService.DesconectarAsync(user);
            return Ok();
        }

        [HttpPost("preparar-revisao")]
        public async Task<IActionResult> PrepararRevisao([FromBody] GitHubIdeRevisaoRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            
            // PASSO 1: Log de diagnóstico do payload recebido do frontend
            foreach (var arq in request.Arquivos)
            {
                var conteudo = arq.Value ?? string.Empty;
                _logger.LogInformation("IDE DEBUG PASSO 1 [Controller] {Arquivo}: Tamanho={Tamanho}, CR={CR}, LF={LF}, literalN={LiteralN}", 
                    arq.Key, 
                    conteudo.Length, 
                    conteudo.Count(c => c == '\r'), 
                    conteudo.Count(c => c == '\n'), 
                    conteudo.Contains("\\n"));
            }

            if (user.Perfil != "equipe" && user.Perfil != "adm")
            {
                await _auditoriaService.RegistrarAsync("IDE_REVISAO_BLOQUEADA", "GitHubIde", null, $"Perfil não autorizado: {user.Perfil}", user.IdentificadorFuncionario);
                return Forbid();
            }

            // Validações
            if (request.Checklist == null || !request.Checklist.PreviewTestado || !request.Checklist.SemDadosSensiveis || !request.Checklist.EscopoConfirmado)
            {
                return BadRequest(new GitHubPullRequestResultadoDto { Sucesso = false, Mensagem = "O checklist de segurança não foi preenchido corretamente." });
            }

            var allowedFiles = new[] { "index.html", "style.css", "script.js" };
            long totalSize = 0;
            foreach (var file in request.Arquivos)
            {
                if (!allowedFiles.Contains(file.Key))
                {
                    return BadRequest(new GitHubPullRequestResultadoDto { Sucesso = false, Mensagem = $"Arquivo não permitido: {file.Key}" });
                }
                
                if (file.Key.Contains("../") || file.Key.Contains("..\\") || file.Key.StartsWith("/") || file.Key.StartsWith("\\"))
                {
                    return BadRequest(new GitHubPullRequestResultadoDto { Sucesso = false, Mensagem = "Caminhos relativos/absolutos não são permitidos." });
                }

                long size = System.Text.Encoding.UTF8.GetByteCount(file.Value);
                if (size > 300 * 1024)
                {
                    return BadRequest(new GitHubPullRequestResultadoDto { Sucesso = false, Mensagem = $"O arquivo {file.Key} excede o limite de 300 KB." });
                }
                totalSize += size;
            }

            if (totalSize > 1024 * 1024)
            {
                return BadRequest(new GitHubPullRequestResultadoDto { Sucesso = false, Mensagem = "O payload total excede o limite de 1 MB." });
            }

            await _auditoriaService.RegistrarAsync("IDE_REVISAO_VALIDADA", "GitHubIde", null, "Arquivos validados", user.IdentificadorFuncionario);

            GitHubPullRequestResultadoDto resultado;
            if (request.Modo == "forkPessoal")
            {
                resultado = await _forkService.CriarPullRequestViaForkAsync(request, user);
            }
            else
            {
                resultado = await _githubService.CriarPullRequestAsync(request, user);
            }

            if (resultado.Sucesso)
            {
                return Ok(resultado);
            }
            else
            {
                return BadRequest(resultado);
            }
        }
    }
}
