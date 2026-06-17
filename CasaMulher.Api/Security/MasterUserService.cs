using CasaMulher.Api.Models;

namespace CasaMulher.Api.Security;

public class MasterUserService : IMasterUserService
{
    private readonly IConfiguration _configuration;

    public MasterUserService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string SuperAdminIdentificador =>
        _configuration["Seguranca:Master:SuperAdminIdentificador"] ?? "ADM-000003";

    public string EquipeOwnerCodigo =>
        _configuration["Seguranca:Master:EquipeOwnerCodigo"] ?? "EQP-000001";

    public bool EhSuperAdminInstitucional(ApplicationUser? usuario)
    {
        return usuario is not null
            && usuario.Ativo
            && string.Equals(usuario.Perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase)
            && string.Equals(usuario.IdentificadorFuncionario, SuperAdminIdentificador, StringComparison.OrdinalIgnoreCase);
    }

    public bool EhEquipeOwnerPrincipal(string? codigoEquipe)
    {
        return !string.IsNullOrWhiteSpace(codigoEquipe)
            && string.Equals(codigoEquipe, EquipeOwnerCodigo, StringComparison.OrdinalIgnoreCase);
    }

    public bool EhEquipeOwnerPrincipal(EquipeMembro? membro)
    {
        return membro is not null
            && membro.Ativo
            && string.Equals(membro.PapelEquipe, EquipePapeis.Owner, StringComparison.OrdinalIgnoreCase)
            && EhEquipeOwnerPrincipal(membro.CodigoEquipe);
    }
}
