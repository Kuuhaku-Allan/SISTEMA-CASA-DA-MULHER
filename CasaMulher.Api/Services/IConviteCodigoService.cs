namespace CasaMulher.Api.Services;

public interface IConviteCodigoService
{
    string GerarHash(string codigo);

    bool CodigoCorresponde(string codigo, string hashSalvo);
}
