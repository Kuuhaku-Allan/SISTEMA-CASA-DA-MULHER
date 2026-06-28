using System.Collections.Generic;

namespace CasaMulher.Api.DTOs
{
    public class GitHubIdeStatusDto
    {
        public bool Enabled { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string Repo { get; set; } = string.Empty;
        public string BaseBranch { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public bool CanCreatePullRequest { get; set; }
    }

    public class GitHubIdeChecklist
    {
        public bool PreviewTestado { get; set; }
        public bool SemDadosSensiveis { get; set; }
        public bool EscopoConfirmado { get; set; }
    }

    public class GitHubIdeTarefaDto
    {
        public string Id { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }

    public sealed class GitHubIdeAreaProjetoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class GitHubIdeChecklistItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public bool Marcado { get; set; }
    }

    public class GitHubIdeRevisaoRequest
    {
        public string Modo { get; set; } = "modoSeguroEquipe";
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public GitHubIdeTarefaDto? Tarefa { get; set; }
        public GitHubIdeAreaProjetoDto? AreaProjeto { get; set; }
        public List<GitHubIdeChecklistItemDto> ChecklistTarefa { get; set; } = new();
        public Dictionary<string, string> Arquivos { get; set; } = new();
        public GitHubIdeChecklist Checklist { get; set; } = new();
    }

    public class GitHubPullRequestResultadoDto
    {
        public bool Sucesso { get; set; }
        public string Branch { get; set; } = string.Empty;
        public string PullRequestUrl { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }

    public class GitHubConexaoStatusDto
    {
        public bool Conectado { get; set; }
        public bool PodeConectar { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        
        public string Login { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty;
        public bool PodeCriarFork { get; set; }
    }
}
