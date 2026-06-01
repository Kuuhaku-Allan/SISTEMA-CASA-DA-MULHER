namespace CasaMulher.Api.Services;

public interface IAuditoriaService
{
    Task RegistrarAsync(string acao, string entidade, string? entidadeId, string descricao);
}
