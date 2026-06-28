using System;

namespace CasaMulher.Api.DTOs
{
    public sealed class EquipeIdeAmbienteStatusDto
    {
        public bool ApiOnline { get; set; } = true;
        public string Ambiente { get; set; } = string.Empty;
        public EquipeIdeUsuarioAtualDto Usuario { get; set; } = new();
        public EquipeIdeGitHubStatusResumoDto GitHubIde { get; set; } = new();
        public EquipeIdeRecursosDto Recursos { get; set; } = new();
        public DateTimeOffset VerificadoEm { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class EquipeIdeUsuarioAtualDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
    }

    public sealed class EquipeIdeGitHubStatusResumoDto
    {
        public bool Enabled { get; set; }
        public bool ModoSeguroEquipe { get; set; }
        public bool ForkPessoal { get; set; }
    }

    public sealed class EquipeIdeRecursosDto
    {
        public bool TarefasGuiadas { get; set; } = true;
        public bool MapaProjeto { get; set; } = true;
        public bool ValidacaoAutomatica { get; set; } = true;
        public bool StatusBackend { get; set; } = true;
        public bool RunnerBackend { get; set; } = false;
        public bool TerminalControlado { get; set; } = false;
    }
}
