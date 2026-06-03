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

    public DbSet<AuditoriaEvento> AuditoriaEventos => Set<AuditoriaEvento>();

    public DbSet<EmailEvento> EmailEventos => Set<EmailEvento>();

    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();

    public DbSet<PasskeyChallenge> PasskeyChallenges => Set<PasskeyChallenge>();

    public DbSet<PasskeyReconfirmacao> PasskeyReconfirmacoes => Set<PasskeyReconfirmacao>();

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
