namespace CasaMulher.Api.Services;

public interface IFuncionarioIdentificadorService
{
    Task<string> GerarProximoAsync(string perfil);
}
