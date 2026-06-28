using System;
using System.Linq;
using System.Threading.Tasks;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace CasaMulher.Api.Services
{
    public class ManualTokenGitHubIdeService : IGitHubIdeService
    {
        private readonly GitHubIdeSettings _settings;
        private readonly ILogger<ManualTokenGitHubIdeService> _logger;
        private readonly IAuditoriaService _auditoriaService;

        public ManualTokenGitHubIdeService(IOptions<GitHubIdeSettings> options, ILogger<ManualTokenGitHubIdeService> logger, IAuditoriaService auditoriaService)
        {
            _settings = options.Value;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        public Task<GitHubIdeStatusDto> ObterStatusAsync()
        {
            var status = new GitHubIdeStatusDto
            {
                Enabled = _settings.Enabled,
                Owner = _settings.Owner,
                Repo = _settings.Repo,
                BaseBranch = _settings.BaseBranch,
                Mode = _settings.Mode,
                CanCreatePullRequest = _settings.Enabled && !string.IsNullOrWhiteSpace(_settings.Token)
            };
            return Task.FromResult(status);
        }

        public async Task<GitHubPullRequestResultadoDto> CriarPullRequestAsync(GitHubIdeRevisaoRequest request, ApplicationUser usuario)
        {
            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.Token))
            {
                return new GitHubPullRequestResultadoDto { Sucesso = false, Mensagem = "Integração com GitHub ainda não configurada neste ambiente." };
            }

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("CasaMulherIde"))
                {
                    Credentials = new Credentials(_settings.Token)
                };

                // 1. Obter SHA da branch base
                var baseBranch = await client.Git.Reference.Get(_settings.Owner, _settings.Repo, $"heads/{_settings.BaseBranch}");
                string baseSha = baseBranch.Object.Sha;

                // 2. Definir nome seguro da branch
                string timeString = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                string safeModelName = new string(request.Modelo.ToLower().Where(c => char.IsLetterOrDigit(c) || c == '-' || c == ' ').ToArray()).Replace(" ", "-").Trim('-');
                string identificador = usuario.IdentificadorFuncionario?.ToLower() ?? "desc";
                string branchName = $"ide/{identificador}/{timeString}-{safeModelName}";

                // 3. Criar a ref da nova branch apontando para o baseSha
                await client.Git.Reference.Create(_settings.Owner, _settings.Repo, new NewReference($"refs/heads/{branchName}", baseSha));
                await _auditoriaService.RegistrarAsync("IDE_BRANCH_CRIADA", "GitHubIde", null, $"Branch {branchName} criada", identificador);

                // 4. Montar a Tree com os arquivos
                var newTree = new NewTree { BaseTree = baseSha };
                string rootFolder = _settings.AllowedRoot;
                string destFolder = $"{rootFolder}/{identificador.ToUpper()}/{timeString}-{safeModelName}";

                foreach (var file in request.Arquivos)
                {
                    var base64 = IdeContentSanitizer.SanitizarEConverterParaBase64(file.Value, file.Key, identificador.ToUpper(), _logger, "modoSeguroEquipe");
                    
                    var blob = new NewBlob { Content = base64, Encoding = EncodingType.Base64 };
                    var blobRef = await client.Git.Blob.Create(_settings.Owner, _settings.Repo, blob);

                    newTree.Tree.Add(new NewTreeItem
                    {
                        Path = $"{destFolder}/{file.Key}",
                        Mode = "100644",
                        Type = TreeType.Blob,
                        Sha = blobRef.Sha
                    });
                }

                var safeTitulo = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Titulo ?? "Sem Título", "Titulo", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                var safeDescricao = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Descricao ?? "", "Descricao", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");

                var tarefaSection = "";
                if (request.Tarefa != null)
                {
                    var safeTarefaTipo = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Tarefa.Tipo ?? "", "TarefaTipo", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                    var safeTarefaTitulo = IdeContentSanitizer.SanitizarTextoCurtoIde(request.Tarefa.Titulo ?? "", "TarefaTitulo", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                    
                    tarefaSection = $"\n\n## Tarefa da IDE\n\n- Tarefa: {safeTarefaTitulo}\n- Tipo: {safeTarefaTipo}\n- Modo de envio: Modo seguro da equipe\n- Escopo: ide-rascunhos";
                    
                    if (request.ChecklistTarefa != null && request.ChecklistTarefa.Any())
                    {
                        tarefaSection += "\n\n## Checklist da tarefa\n\n";
                        foreach (var item in request.ChecklistTarefa)
                        {
                            var safeItemTexto = IdeContentSanitizer.SanitizarTextoCurtoIde(item.Texto ?? "", "ChecklistItem", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                            var mark = item.Marcado ? "[x]" : "[ ]";
                            tarefaSection += $"- {mark} {safeItemTexto}\n";
                        }
                    }
                }
                var areaProjetoSection = "";
                if (request.AreaProjeto != null)
                {
                    var safeAreaNome = IdeContentSanitizer.SanitizarTextoCurtoIde(request.AreaProjeto.Nome ?? "", "AreaNome", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                    var safeAreaPerfil = IdeContentSanitizer.SanitizarTextoCurtoIde(request.AreaProjeto.Perfil ?? "", "AreaPerfil", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                    var safeAreaStatus = IdeContentSanitizer.SanitizarTextoCurtoIde(request.AreaProjeto.Status ?? "", "AreaStatus", usuario.IdentificadorFuncionario ?? "SYS", _logger, "manualToken");
                    
                    areaProjetoSection = $"\n\n## Area relacionada\n\n- Area: {safeAreaNome}\n- Perfil: {safeAreaPerfil}\n- Status: {safeAreaStatus}";
                }
                else
                {
                    areaProjetoSection = "\n\n## Area relacionada\n\n- Area: Nao informada";
                }

                var validacoesSection = "";
                if (request.Validacoes != null && request.Validacoes.Count > 0)
                {
                    var bloqueios = request.Validacoes.Count(v => v.Severidade == "bloqueio");
                    var avisos = request.Validacoes.Count(v => v.Severidade == "aviso");
                    var infos = request.Validacoes.Count(v => v.Severidade == "info");

                    validacoesSection = $"\n\n## Validacao automatica\n\n- Bloqueios: {bloqueios}\n- Avisos: {avisos}\n- Informacoes: {infos}";
                    
                    if (avisos > 0 || bloqueios > 0)
                    {
                        validacoesSection += "\n\n### Avisos e Bloqueios\n";
                        foreach (var v in request.Validacoes.Where(x => x.Severidade == "aviso" || x.Severidade == "bloqueio"))
                        {
                            var sFile = IdeContentSanitizer.SanitizarTextoCurtoIde(v.Arquivo, "ValidacaoArquivo", identificador, _logger, "Seguro");
                            var sTitle = IdeContentSanitizer.SanitizarTextoCurtoIde(v.Titulo, "ValidacaoTitulo", identificador, _logger, "Seguro");
                            validacoesSection += $"\n- `{sFile}`: {sTitle}";
                        }
                    }
                }

                // Criar README dinamicamente
                string readmeContent = $@"# Protótipo enviado pela IDE da Equipe

## Autor
- Nome: {usuario.NomeCompleto}
- Perfil: {usuario.Perfil}
- ID: {identificador.ToUpper()}

## Modelo
{request.Modelo}

## Descrição
{safeDescricao}
{tarefaSection}
{areaProjetoSection}
{validacoesSection}

## Arquivos
{string.Join(Environment.NewLine, request.Arquivos.Keys.Select(k => $"- {k}"))}

## Checklist
- [{(request.Checklist.PreviewTestado ? "x" : " ")}] Preview testado
- [{(request.Checklist.SemDadosSensiveis ? "x" : " ")}] Sem dados sensíveis
- [{(request.Checklist.EscopoConfirmado ? "x" : " ")}] Escopo confirmado

## Observação
> Este rascunho foi gerado automaticamente pela ferramenta de Design Seguro da Equipe.";

                // Cria Pull Request
                var prBody = $@"Protótipo gerado pela IDE da Equipe.
                
Autor: {usuario.NomeCompleto} ({usuario.Perfil})
Descrição: {safeDescricao}
{tarefaSection}
{areaProjetoSection}
{validacoesSection}

[✓] Preview validado visualmente na máquina local
[✓] Nenhuma informação sensível/real foi inserida nos arquivos
[✓] Escopo limitado aos arquivos de tela no diretório rascunhos

## Arquivos
{string.Join(Environment.NewLine, request.Arquivos.Keys.Select(k => $"- {k}"))}

## Checklist
- [{(request.Checklist.PreviewTestado ? "x" : " ")}] Preview testado
- [{(request.Checklist.SemDadosSensiveis ? "x" : " ")}] Sem dados sensíveis
- [{(request.Checklist.EscopoConfirmado ? "x" : " ")}] Escopo confirmado

## Observação
Este protótipo foi gerado pela IDE da Equipe e salvo em pasta segura para revisão.";

                var readmeBase64 = IdeContentSanitizer.SanitizarEConverterParaBase64(readmeContent, "README.md", identificador.ToUpper(), _logger, "modoSeguroEquipe");
                
                var readmeBlob = new NewBlob { Content = readmeBase64, Encoding = EncodingType.Base64 };
                var readmeRef = await client.Git.Blob.Create(_settings.Owner, _settings.Repo, readmeBlob);

                newTree.Tree.Add(new NewTreeItem
                {
                    Path = $"{destFolder}/README.md",
                    Mode = "100644",
                    Type = TreeType.Blob,
                    Sha = readmeRef.Sha
                });

                var createdTree = await client.Git.Tree.Create(_settings.Owner, _settings.Repo, newTree);

                // 5. Criar Commit (as variáveis safeTitulo e safeDescricao já foram criadas)

                // 6. Fazer o commit
                var commitMsg = $"Rascunho via IDE: {safeTitulo}";
                var commit = new NewCommit(commitMsg, createdTree.Sha, baseSha);
                var createdCommit = await client.Git.Commit.Create(_settings.Owner, _settings.Repo, commit);
                await _auditoriaService.RegistrarAsync("IDE_ARQUIVOS_COMMITADOS", "GitHubIde", null, $"Commit {createdCommit.Sha} criado em {branchName}", identificador);

                // 7. Atualizar a branch
                await client.Git.Reference.Update(_settings.Owner, _settings.Repo, $"heads/{branchName}", new ReferenceUpdate(createdCommit.Sha));

                // 8. Abrir Pull Request
                var pr = new NewPullRequest(commitMsg, branchName, _settings.BaseBranch)
                {
                    Body = $"## {safeTitulo}\n\n{safeDescricao}{tarefaSection}\n\n---\n*Enviado via IDE Segura*"
                };

                var createdPr = await client.PullRequest.Create(_settings.Owner, _settings.Repo, pr);
                await _auditoriaService.RegistrarAsync("IDE_PR_ABERTO", "GitHubIde", null, $"PR #{createdPr.Number} criado em {branchName}", identificador);

                return new GitHubPullRequestResultadoDto
                {
                    Sucesso = true,
                    Branch = branchName,
                    PullRequestUrl = createdPr.HtmlUrl,
                    Mensagem = "Pull Request criado com sucesso."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar Pull Request no GitHub.");
                await _auditoriaService.RegistrarAsync("IDE_GITHUB_ERRO", "GitHubIde", null, $"Erro: {ex.Message}", usuario.IdentificadorFuncionario);
                return new GitHubPullRequestResultadoDto
                {
                    Sucesso = false,
                    Mensagem = "Não foi possível criar o Pull Request agora. Verifique a configuração da integração GitHub."
                };
            }
        }
    }
}
