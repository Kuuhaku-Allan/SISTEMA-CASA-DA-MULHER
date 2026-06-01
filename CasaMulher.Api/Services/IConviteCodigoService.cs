namespace CasaMulher.Api.Services;

public interface IConviteCodigoService
{
    string GerarCodigoCadastro();

    string GerarHash(string codigo);

    bool CodigoCorresponde(string codigo, string hashSalvo);
}
