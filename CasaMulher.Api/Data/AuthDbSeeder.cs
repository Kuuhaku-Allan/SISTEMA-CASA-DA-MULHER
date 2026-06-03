using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Data;

public static class AuthDbSeeder
{
    private static readonly DemoConvite[] ConvitesDemo =
    [
        new("Coordenação", "coord@casamulher.local", PerfisAcesso.Adm, "ADM-2026"),
        new("Recepção", "recepcao@casamulher.local", PerfisAcesso.Recepcao, "REC-2026"),
        new("Professora", "professora@casamulher.local", PerfisAcesso.Professor, "PROF-2026"),
        new("Assistente Social", "social@casamulher.local", PerfisAcesso.AssistenteSocial, "SOCIAL-2026"),
        new("Jurídico", "juridico@casamulher.local", PerfisAcesso.Juridico, "JUR-2026")
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var codigoService = scope.ServiceProvider.GetRequiredService<IConviteCodigoService>();
        var identificadorService = scope.ServiceProvider.GetRequiredService<IFuncionarioIdentificadorService>();

        await dbContext.Database.MigrateAsync();

        foreach (var perfil in PerfisAcesso.Todos)
        {
            if (!await roleManager.RoleExistsAsync(perfil))
            {
                await roleManager.CreateAsync(new IdentityRole(perfil));
            }
        }

        foreach (var conviteDemo in ConvitesDemo)
        {
            var codigoHash = codigoService.GerarHash(conviteDemo.Codigo);
            var conviteExiste = await dbContext.FuncionariosConvites
                .SingleOrDefaultAsync(convite => convite.CodigoHash == codigoHash);

            if (conviteExiste is not null)
            {
                if (string.IsNullOrWhiteSpace(conviteExiste.IdentificadorFuncionario))
                {
                    conviteExiste.IdentificadorFuncionario = await identificadorService.GerarProximoAsync(conviteDemo.Perfil);
                    await dbContext.SaveChangesAsync();
                }

                continue;
            }

            dbContext.FuncionariosConvites.Add(new FuncionarioConvite
            {
                NomeCompleto = conviteDemo.NomeCompleto,
                Email = conviteDemo.Email,
                Perfil = conviteDemo.Perfil,
                IdentificadorFuncionario = await identificadorService.GerarProximoAsync(conviteDemo.Perfil),
                CodigoHash = codigoHash,
                ExpiraEm = DateTime.UtcNow.AddMonths(6)
            });

            await dbContext.SaveChangesAsync();
        }
    }

    private sealed record DemoConvite(string NomeCompleto, string Email, string Perfil, string Codigo);
}
