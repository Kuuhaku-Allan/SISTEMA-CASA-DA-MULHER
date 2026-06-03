using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services;

public interface IRedefinicaoSenhaEmailService
{
    Task<ResultadoRedefinicaoSenhaEmail> EnviarAsync(ApplicationUser funcionario);
}
