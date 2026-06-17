namespace CasaMulher.Api.Services;

public interface IConviteCodigoService
{
    string GerarCodigoCadastro();

    string GerarCodigoAtivacaoEquipe();

    string GerarHash(string codigo);

    bool CodigoCorresponde(string codigo, string hashSalvo);
}
