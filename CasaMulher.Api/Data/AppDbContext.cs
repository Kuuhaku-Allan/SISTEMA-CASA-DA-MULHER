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
    }
}
