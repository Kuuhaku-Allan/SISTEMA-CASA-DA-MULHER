using CasaMulher.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Services;

public class ContaEquipeSincronizadaService
{
    public const string MensagemAlteracaoSenha =
        "Esta conta é sincronizada pelo portal EQP. Para alterar a senha, use a opção de redefinição de senha no portal da equipe.";

    private readonly AppDbContext _dbContext;

    public ContaEquipeSincronizadaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EhSincronizadaAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(false);
        }

        return _dbContext.UserLoginIdentifiers.AnyAsync(
            identificador => identificador.UserId == userId && identificador.Ativo,
            cancellationToken);
    }
}
