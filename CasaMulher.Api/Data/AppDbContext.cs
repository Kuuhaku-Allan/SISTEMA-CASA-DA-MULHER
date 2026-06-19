using CasaMulher.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CasaMulher.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FuncionarioConvite> FuncionariosConvites => Set<FuncionarioConvite>();

    public DbSet<EquipeConvite> EquipeConvites => Set<EquipeConvite>();

    public DbSet<EquipeMembro> EquipeMembros => Set<EquipeMembro>();

    public DbSet<EquipeSenhaReset> EquipeSenhaResets => Set<EquipeSenhaReset>();

    public DbSet<AuditoriaEvento> AuditoriaEventos => Set<AuditoriaEvento>();

    public DbSet<EmailEvento> EmailEventos => Set<EmailEvento>();

    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();

    public DbSet<PasskeyChallenge> PasskeyChallenges => Set<PasskeyChallenge>();

    public DbSet<PasskeyReconfirmacao> PasskeyReconfirmacoes => Set<PasskeyReconfirmacao>();

    public DbSet<UserLoginIdentifier> UserLoginIdentifiers => Set<UserLoginIdentifier>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(user => user.IdentificadorFuncionario)
                .IsUnique();

            entity.Property(user => user.NomeCompleto)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(user => user.Perfil)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(user => user.IdentificadorFuncionario)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(user => user.EmailRecuperacao)
                .HasMaxLength(256);
        });

        builder.Entity<UserLoginIdentifier>(entity =>
        {
            entity.ToTable("UserLoginIdentifiers");

            entity.HasIndex(identifier => identifier.Identificador)
                .IsUnique();

            entity.HasIndex(identifier => identifier.UserId);

            entity.Property(identifier => identifier.UserId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(identifier => identifier.Identificador)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(identifier => identifier.Tipo)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasOne(identifier => identifier.User)
                .WithMany(user => user.LoginIdentifiers)
                .HasForeignKey(identifier => identifier.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FuncionarioConvite>(entity =>
        {
            entity.ToTable("FuncionariosConvites");

            entity.HasIndex(convite => convite.CodigoHash)
                .IsUnique();

            entity.HasIndex(convite => convite.Email);

            entity.Property(convite => convite.NomeCompleto)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(convite => convite.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(convite => convite.Perfil)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(convite => convite.IdentificadorFuncionario)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(convite => convite.IdentificadorFuncionario);

            entity.Property(convite => convite.CodigoHash)
                .HasMaxLength(128)
                .IsRequired();
        });

        builder.Entity<EquipeConvite>(entity =>
        {
            entity.ToTable("EquipeConvites");

            entity.HasIndex(convite => convite.CodigoEquipe)
                .IsUnique();

            entity.HasIndex(convite => convite.CodigoAtivacaoHash)
                .IsUnique();

            entity.HasIndex(convite => convite.Status);

            entity.Property(convite => convite.CodigoEquipe)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(convite => convite.CodigoAtivacaoHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(convite => convite.Status)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(convite => convite.CriadoPorUserId)
                .HasMaxLength(80);

            entity.Property(convite => convite.UsadoPorUserId)
                .HasMaxLength(80);

            entity.Property(convite => convite.NomeInformado)
                .HasMaxLength(160);

            entity.Property(convite => convite.PapelEquipe)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(convite => convite.FluxoTrabalho)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(convite => convite.Observacao)
                .HasMaxLength(500);
        });

        builder.Entity<EquipeMembro>(entity =>
        {
            entity.ToTable("EquipeMembros");

            entity.HasIndex(membro => membro.UserId)
                .IsUnique();

            entity.HasIndex(membro => membro.CodigoEquipe)
                .IsUnique();

            entity.Property(membro => membro.UserId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(membro => membro.CodigoEquipe)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(membro => membro.Nome)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(membro => membro.PapelEquipe)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(membro => membro.FluxoTrabalho)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(membro => membro.GitHubUsername)
                .HasMaxLength(80);

            entity.Property(membro => membro.GitHubId)
                .HasMaxLength(80);

            entity.Property(membro => membro.ForkUrl)
                .HasMaxLength(300);
        });

        builder.Entity<EquipeSenhaReset>(entity =>
        {
            entity.ToTable("EquipeSenhaResets");

            entity.HasIndex(reset => reset.CodigoHash)
                .IsUnique();

            entity.HasIndex(reset => reset.CodigoEquipe);
            entity.HasIndex(reset => reset.ExpiraEm);

            entity.Property(reset => reset.CodigoEquipe)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(reset => reset.CodigoHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(reset => reset.GeradoPorUserId)
                .HasMaxLength(80)
                .IsRequired();
        });

        builder.Entity<AuditoriaEvento>(entity =>
        {
            entity.ToTable("AuditoriaEventos");

            entity.HasIndex(evento => evento.CriadoEm);
            entity.HasIndex(evento => evento.UsuarioId);
            entity.HasIndex(evento => evento.EntidadeId);

            entity.Property(evento => evento.UsuarioId).HasMaxLength(80);
            entity.Property(evento => evento.IdentificadorFuncionario).HasMaxLength(20);
            entity.Property(evento => evento.NomeFuncionario).HasMaxLength(160);
            entity.Property(evento => evento.PerfilFuncionario).HasMaxLength(40);
            entity.Property(evento => evento.Escopo).HasMaxLength(20).IsRequired();
            entity.HasIndex(evento => evento.Escopo);
            entity.Property(evento => evento.Acao).HasMaxLength(80).IsRequired();
            entity.Property(evento => evento.Entidade).HasMaxLength(80).IsRequired();
            entity.Property(evento => evento.EntidadeId).HasMaxLength(80);
            entity.Property(evento => evento.Descricao).HasMaxLength(500).IsRequired();
            entity.Property(evento => evento.IpOrigem).HasMaxLength(80);
            entity.Property(evento => evento.UserAgent).HasMaxLength(500);
        });

        builder.Entity<EmailEvento>(entity =>
        {
            entity.ToTable("EmailEventos");

            entity.HasIndex(evento => evento.CriadoEm);
            entity.HasIndex(evento => evento.Destinatario);
            entity.HasIndex(evento => evento.Tipo);
            entity.HasIndex(evento => evento.Status);

            entity.Property(evento => evento.Destinatario).HasMaxLength(256).IsRequired();
            entity.Property(evento => evento.Assunto).HasMaxLength(200).IsRequired();
            entity.Property(evento => evento.Tipo).HasMaxLength(80).IsRequired();
            entity.Property(evento => evento.Status).HasMaxLength(40).IsRequired();
            entity.Property(evento => evento.Erro).HasMaxLength(500);
        });

        builder.Entity<PasskeyCredential>(entity =>
        {
            entity.ToTable("PasskeyCredentials");

            entity.HasIndex(c => c.CredentialId).IsUnique();
            entity.HasIndex(c => c.UserId);

            entity.Property(c => c.UserId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(c => c.NomeDispositivo)
                .HasMaxLength(120);

            entity.Property(c => c.Transports)
                .HasMaxLength(200);

            entity.Property(c => c.RpId)
                .HasMaxLength(253)
                .IsRequired();

            entity.HasOne(c => c.User)
                .WithMany(u => u.PasskeyCredentials)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasskeyChallenge>(entity =>
        {
            entity.ToTable("PasskeyChallenges");

            entity.HasIndex(c => c.ChallengeId).IsUnique();
            entity.HasIndex(c => c.ExpiracaoEm);

            entity.Property(c => c.ChallengeId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(c => c.Tipo)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(c => c.UserId)
                .HasMaxLength(80);

            entity.Property(c => c.ContextoPerfil)
                .HasMaxLength(20);

            entity.Property(c => c.ContextoIdentificador)
                .HasMaxLength(20);

            entity.Property(c => c.OptionsJson)
                .IsRequired();
        });

        builder.Entity<PasskeyReconfirmacao>(entity =>
        {
            entity.ToTable("PasskeyReconfirmacoes");

            entity.HasIndex(r => r.ReconfirmacaoId).IsUnique();
            entity.HasIndex(r => r.ExpiracaoEm);

            entity.Property(r => r.ReconfirmacaoId)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(r => r.UserId)
                .HasMaxLength(80)
                .IsRequired();
        });
    }
}
