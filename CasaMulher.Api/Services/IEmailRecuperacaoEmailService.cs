using CasaMulher.Api.Models;

namespace CasaMulher.Api.Services;

public interface IEmailRecuperacaoEmailService
{
    Task<ResultadoEmailRecuperacao> EnviarConfirmacaoAsync(ApplicationUser funcionario);
}
