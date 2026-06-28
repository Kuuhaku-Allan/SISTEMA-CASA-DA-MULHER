namespace CasaMulher.Api.Services;

public interface IAuditoriaService
{
    Task RegistrarAsync(
        string acao,
        string entidade,
        string? entidadeId,
        string descricao,
        string? identificadorReferencia = null,
        string? escopo = null);
}
