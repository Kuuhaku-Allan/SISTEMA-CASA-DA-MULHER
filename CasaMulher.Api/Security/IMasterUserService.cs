using CasaMulher.Api.Models;

namespace CasaMulher.Api.Security;

public interface IMasterUserService
{
    string SuperAdminIdentificador { get; }

    string EquipeOwnerCodigo { get; }

    bool EhSuperAdminInstitucional(ApplicationUser? usuario);

    bool EhEquipeOwnerPrincipal(string? codigoEquipe);

    bool EhEquipeOwnerPrincipal(EquipeMembro? membro);
}
