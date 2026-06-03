using System.Collections.Concurrent;

namespace CasaMulher.Api.Services;

public class InMemoryRedefinicaoSenhaThrottleService : IRedefinicaoSenhaThrottleService
{
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, JanelaTentativas> _janelas = new();

    public bool PermitirSolicitacao(string usuarioId, string ipOrigem, out string motivo, out DateTimeOffset bloqueadoAte)
    {
        if (!PermitirChave($"usuario:{usuarioId}", 3, out bloqueadoAte))
        {
            motivo = "usuario";
            return false;
        }

        if (!PermitirChave($"ip:{ipOrigem}", 5, out bloqueadoAte))
        {
            motivo = "ip";
            return false;
        }

        motivo = string.Empty;
        return true;
    }

    private bool PermitirChave(string chave, int limite, out DateTimeOffset bloqueadoAte)
    {
        var agora = DateTimeOffset.UtcNow;
        var janela = _janelas.GetOrAdd(chave, _ => new JanelaTentativas(agora));

        lock (janela)
        {
            if (agora - janela.IniciadaEm >= Janela)
            {
                janela.IniciadaEm = agora;
                janela.Tentativas = 0;
            }

            bloqueadoAte = janela.IniciadaEm.Add(Janela);

            if (janela.Tentativas >= limite)
            {
                return false;
            }

            janela.Tentativas++;
            return true;
        }
    }

    private sealed class JanelaTentativas
    {
        public JanelaTentativas(DateTimeOffset iniciadaEm)
        {
            IniciadaEm = iniciadaEm;
        }

        public DateTimeOffset IniciadaEm { get; set; }

        public int Tentativas { get; set; }
    }
}
