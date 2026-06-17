namespace CasaMulher.Api.Security;

public static class RateLimitPolicies
{
    public const string Login = "rate-login";
    public const string LoginDoisFatores = "rate-login-2fa";
    public const string ConvitePublico = "rate-convite-publico";
    public const string EquipeAtivacao = "rate-equipe-ativacao";
    public const string SolicitarRedefinicaoSenha = "rate-solicitar-redefinicao-senha";
    public const string RedefinirSenha = "rate-redefinir-senha";
    public const string PasskeyLoginIniciar = "rate-passkey-login-iniciar";
    public const string PasskeyLoginConcluir = "rate-passkey-login-concluir";
    public const string PasskeyReconfirmar = "rate-passkey-reconfirmar";
}
