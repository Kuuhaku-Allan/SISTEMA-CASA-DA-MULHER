namespace CasaMulher.Api.Services;

public interface IRedefinicaoSenhaThrottleService
{
    bool PermitirSolicitacao(string usuarioId, string ipOrigem, out string motivo, out DateTimeOffset bloqueadoAte);
}
