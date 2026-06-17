using Microsoft.AspNetCore.Identity;

namespace CasaMulher.Api.Security;

public class PortugueseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Criar(nameof(DefaultError), "Ocorreu um erro inesperado.");

    public override IdentityError ConcurrencyFailure() =>
        Criar(nameof(ConcurrencyFailure), "O registro foi alterado por outra operação. Tente novamente.");

    public override IdentityError PasswordMismatch() =>
        Criar(nameof(PasswordMismatch), "A senha informada está incorreta.");

    public override IdentityError InvalidToken() =>
        Criar(nameof(InvalidToken), "O token informado é inválido.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Criar(nameof(RecoveryCodeRedemptionFailed), "Não foi possível usar o código de recuperação.");

    public override IdentityError LoginAlreadyAssociated() =>
        Criar(nameof(LoginAlreadyAssociated), "Este login já está associado a outra conta.");

    public override IdentityError InvalidUserName(string? userName) =>
        Criar(nameof(InvalidUserName), $"O nome de usuário '{userName}' é inválido.");

    public override IdentityError InvalidEmail(string? email) =>
        Criar(nameof(InvalidEmail), $"O e-mail '{email}' é inválido.");

    public override IdentityError DuplicateUserName(string userName) =>
        Criar(nameof(DuplicateUserName), $"O nome de usuário '{userName}' já está em uso.");

    public override IdentityError DuplicateEmail(string email) =>
        Criar(nameof(DuplicateEmail), $"O e-mail '{email}' já está em uso.");

    public override IdentityError InvalidRoleName(string? role) =>
        Criar(nameof(InvalidRoleName), $"O perfil de acesso '{role}' é inválido.");

    public override IdentityError DuplicateRoleName(string role) =>
        Criar(nameof(DuplicateRoleName), $"O perfil de acesso '{role}' já existe.");

    public override IdentityError UserAlreadyHasPassword() =>
        Criar(nameof(UserAlreadyHasPassword), "Este usuário já possui uma senha.");

    public override IdentityError UserLockoutNotEnabled() =>
        Criar(nameof(UserLockoutNotEnabled), "O bloqueio não está habilitado para este usuário.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Criar(nameof(UserAlreadyInRole), $"O usuário já possui o perfil de acesso '{role}'.");

    public override IdentityError UserNotInRole(string role) =>
        Criar(nameof(UserNotInRole), $"O usuário não possui o perfil de acesso '{role}'.");

    public override IdentityError PasswordTooShort(int length) =>
        Criar(nameof(PasswordTooShort), $"A senha deve ter pelo menos {length} caracteres.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Criar(nameof(PasswordRequiresUniqueChars), $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Criar(nameof(PasswordRequiresNonAlphanumeric), "A senha deve ter pelo menos um símbolo.");

    public override IdentityError PasswordRequiresDigit() =>
        Criar(nameof(PasswordRequiresDigit), "A senha deve ter pelo menos um número de 0 a 9.");

    public override IdentityError PasswordRequiresLower() =>
        Criar(nameof(PasswordRequiresLower), "A senha deve ter pelo menos uma letra minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Criar(nameof(PasswordRequiresUpper), "A senha deve ter pelo menos uma letra maiúscula.");

    private static IdentityError Criar(string code, string description)
    {
        return new IdentityError
        {
            Code = code,
            Description = description
        };
    }
}
