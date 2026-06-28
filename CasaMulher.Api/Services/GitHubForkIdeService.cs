using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Utils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace CasaMulher.Api.Services
{
    public class GitHubForkIdeService : IGitHubForkIdeService
    {
        private readonly AppDbContext _dbContext;
        private readonly GitHubIdeSettings _settings;
        private readonly IDataProtector _protector;
        private readonly ILogger<GitHubForkIdeService> _logger;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IGitHubIdeService _fallbackCentralService;

        public GitHubForkIdeService(
            AppDbContext dbContext,
            IOptions<GitHubIdeSettings> settings,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<GitHubForkIdeService> logger,
            IAuditoriaService auditoriaService,
            IGitHubIdeService fallbackCentralService)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
            _protector = dataProtectionProvider.CreateProtector("GitHubIde.PersonalTokens");
            _logger = logger;
            _auditoriaService = auditoriaService;
            _fallbackCentralService = fallbackCentralService;
        }

        public async Task<GitHubPullRequestResultadoDto> CriarPullRequestViaForkAsync(GitHubIdeRevisaoRequest request, ApplicationUser usuario)
        {
            var vinculo = await _dbContext.GitHubUsuarioVinculos
                .FirstOrDefaultAsync(v => v.ApplicationUserId == usuario.Id && v.RevogadoEm == null);

            if (vinculo == null || string.IsNullOrEmpty(vinculo.AccessTokenEncrypted))
            {
                await _auditoriaService.RegistrarAsync("IDE_PR_FALLBACK_MODO_EQUIPE", "GitHubIde", null, "Usuário sem vínculo GitHub Pessoal. Caiu no modo seguro.", usuario.IdentificadorFuncionario);
                return await _fallbackCentralService.CriarPullRequestAsync(request, usuario);
            }

            string personalToken;
            try
            {
                personalToken = _protector.Unprotect(vinculo.AccessTokenEncrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao descriptografar token pessoal do usuário {UserId}", usuario.Id);
                await _auditoriaService.RegistrarAsync("IDE_GITHUB_TOKEN_DESCRIPTOGRAFIA_FALHOU", "GitHubIde", null, "Falha de descriptografia. Caiu no modo seguro.", usuario.IdentificadorFuncionario);
                return await _fallbackCentralService.CriarPullRequestAsync(request, usuario);
            }

            var client = new GitHubClient(new ProductHeaderValue("CasaMulher-Ide"))
            {
                Credentials = new Credentials(personalToken)
            };

            try
            {
                // Verify user access to the main repo
                var mainRepo = await client.Repository.Get(_settings.Owner, _settings.Repo);

                // Check or Create Fork
                var existingForks = await client.Repository.Forks.GetAll(_settings.Owner, _settings.Repo);
                var personalFork = existingForks.FirstOrDefault(f => string.Equals(f.Owner.Login, vinculo.GitHubLogin, StringComparison.OrdinalIgnoreCase));

                if (personalFork == null)
                {
                    await _auditoriaService.RegistrarAsync("IDE_FORK_PESSOAL_VERIFICADO", "GitHubIde", null, "Fork não encontrado. Tentando criar.", usuario.IdentificadorFuncionario);
                    personalFork = await client.Repository.Forks.Create(_settings.Owner, _settings.Repo, new NewRepositoryFork());
                    
                    // Wait a bit for GitHub to prepare the fork
                    await Task.Delay(5000); 
                    await _auditoriaService.RegistrarAsync("IDE_FORK_PESSOAL_CRIADO", "GitHubIde", null, $"Fork criado: {personalFork.FullName}", usuario.IdentificadorFuncionario);
                }

                // Retry mechanism to ensure fork is accessible
                Reference baseRef = null;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        baseRef = await client.Git.Reference.Get(personalFork.Owner.Login, personalFork.Name, $"heads/{_settings.BaseBranch}");
                        break; // Success
                    }
                    catch (NotFoundException)
                    {
                        if (i == 2) throw;
                        await Task.Delay(3000); // wait and retry
                    }
                }

                var basePath = _settings.AllowedRoot;
                var dataHora = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var folderName = $"{usuario.IdentificadorFuncionario}-{request.Modelo.Replace(" ", "-")}-{dataHora}".ToLower();
                var branchName = $"refs/heads/ide/{folderName}";

                // Create branch on Fork
                await client.Git.Reference.Create(personalFork.Owner.Login, personalFork.Name, new NewReference(branchName, baseRef.Object.Sha));

                // Create Blobs and Trees
                var tree = new NewTree { BaseTree = baseRef.Object.Sha };
                
                foreach (var arquivo in request.Arquivos)
                {
                    var conteudoFinal = arquivo.Value ?? string.Empty;
                    var previewStr = conteudoFinal.Length > 80 ? conteudoFinal[..80].Replace("\n", "\\n") : conteudoFinal.Replace("\n", "\\n");
                    
                    _logger.LogDebug("IDE ANTES_BLOB {Arquivo}: Tamanho={Tamanho}, CR={CR}, LF={LF}, Preview={Preview}",
                        arquivo.Key, 
                        conteudoFinal.Length, 
                        conteudoFinal.Count(c => c == '\r'), 
                        conteudoFinal.Count(c => c == '\n'), 
                        previewStr);

                    var base64 = IdeContentSanitizer.SanitizarEConverterParaBase64(conteudoFinal, arquivo.Key, usuario.IdentificadorFuncionario, _logger, "forkPessoal");
                    var blob = new NewBlob { Content = base64, Encoding = EncodingType.Base64 };
                    var blobRef = await client.Git.Blob.Create(personalFork.Owner.Login, personalFork.Name, blob);
                    tree.Tree.Add(new NewTreeItem { Path = $"{basePath}/{folderName}/{arquivo.Key}", Mode = "100644", Type = TreeType.Blob, Sha = blobRef.Sha });
                }

                var safeTitulo = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Titulo ?? "Sem Título", "Titulo", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                var safeDescricao = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Descricao ?? "", "Descricao", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");

                var tarefaSection = "";
                if (request.Tarefa != null)
                {
                    var safeTarefaTipo = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Tarefa.Tipo ?? "", "TarefaTipo", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                    var safeTarefaTitulo = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Tarefa.Titulo ?? "", "TarefaTitulo", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                    
                    tarefaSection = $"\n\n## Tarefa da IDE\n\n- Tarefa: {safeTarefaTitulo}\n- Tipo: {safeTarefaTipo}\n- Modo de envio: Fork pessoal\n- Escopo: ide-rascunhos";
                    
                    if (request.ChecklistTarefa != null && request.ChecklistTarefa.Any())
                    {
                        tarefaSection += "\n\n## Checklist da tarefa\n\n";
                        foreach (var item in request.ChecklistTarefa)
                        {
                            var safeItemTexto = IdeContentSanitizer.SanitizarTextoCurtoIde(item.Texto ?? "", "ChecklistItem", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                            var mark = item.Marcado ? "[x]" : "[ ]";
                            tarefaSection += $"- {mark} {safeItemTexto}\n";
                        }
                    }
                }
                var areaProjetoSection = "";
                if (request.AreaProjeto != null)
                {
                    var safeAreaNome = IdeContentSanitizer.SanitizarTextoCurtoIde(request.AreaProjeto.Nome ?? "", "AreaNome", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                    var safeAreaPerfil = IdeContentSanitizer.SanitizarTextoCurtoIde(request.AreaProjeto.Perfil ?? "", "AreaPerfil", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                    var safeAreaStatus = IdeContentSanitizer.SanitizarTextoCurtoIde(request.AreaProjeto.Status ?? "", "AreaStatus", usuario.IdentificadorFuncionario ?? "SYS", _logger, "forkPessoal");
                    
                    areaProjetoSection = $"\n\n## Area relacionada\n\n- Area: {safeAreaNome}\n- Perfil: {safeAreaPerfil}\n- Status: {safeAreaStatus}";
                }
                else
                {
                    areaProjetoSection = "\n\n## Area relacionada\n\n- Area: Nao informada";
                }

                // Add README
                var readmeContent = $"# {safeTitulo}\n\n**Usuário**: {usuario.NomeCompleto} ({usuario.IdentificadorFuncionario})\n**GitHub**: @{vinculo.GitHubLogin}\n**Descrição**: {safeDescricao}{tarefaSection}{areaProjetoSection}";
                var readmeBase64 = IdeContentSanitizer.SanitizarEConverterParaBase64(readmeContent, "README.md", usuario.IdentificadorFuncionario, _logger, "forkPessoal");
                var readmeBlob = new NewBlob { Content = readmeBase64, Encoding = EncodingType.Base64 };
                var readmeRef = await client.Git.Blob.Create(personalFork.Owner.Login, personalFork.Name, readmeBlob);
                tree.Tree.Add(new NewTreeItem { Path = $"{basePath}/{folderName}/README.md", Mode = "100644", Type = TreeType.Blob, Sha = readmeRef.Sha });

                var createdTree = await client.Git.Tree.Create(personalFork.Owner.Login, personalFork.Name, tree);

                // Create Commit
                var commitMsg = $"Rascunho via IDE: {safeTitulo}";
                var commit = new NewCommit(commitMsg, createdTree.Sha, baseRef.Object.Sha);
                var createdCommit = await client.Git.Commit.Create(personalFork.Owner.Login, personalFork.Name, commit);

                // Update Branch ref
                await client.Git.Reference.Update(personalFork.Owner.Login, personalFork.Name, branchName, new ReferenceUpdate(createdCommit.Sha));

                // Create PR from fork to main repo
                var prHead = $"{vinculo.GitHubLogin}:ide/{folderName}";
                var newPr = new NewPullRequest(commitMsg, prHead, _settings.BaseBranch)
                {
                    Body = $"## {safeTitulo}\n\n{safeDescricao}{tarefaSection}{areaProjetoSection}\n\n---\n*Enviado via IDE*"
                };

                var pr = await client.PullRequest.Create(_settings.Owner, _settings.Repo, newPr);

                await _auditoriaService.RegistrarAsync("IDE_PR_PESSOAL_CRIADO", "GitHubIde", null, $"PR Pessoal criado no GitHub: {pr.HtmlUrl}", usuario.IdentificadorFuncionario);

                return new GitHubPullRequestResultadoDto
                {
                    Sucesso = true,
                    Branch = branchName,
                    PullRequestUrl = pr.HtmlUrl,
                    Mensagem = "Pull Request gerado com sucesso pelo seu fork pessoal!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no fluxo de PR via fork para {UserId}. Tentando fallback.", usuario.Id);
                await _auditoriaService.RegistrarAsync("IDE_PR_PESSOAL_FALHA", "GitHubIde", null, $"Falha ao criar PR pessoal: {ex.Message}. Fallback para equipe.", usuario.IdentificadorFuncionario);
                
                if (_settings.FallbackToCentralPr)
                {
                    return await _fallbackCentralService.CriarPullRequestAsync(request, usuario);
                }

                return new GitHubPullRequestResultadoDto
                {
                    Sucesso = false,
                    Mensagem = "Não foi possível criar o Fork/PR e o fallback central está desativado."
                };
            }
        }
    }
}
