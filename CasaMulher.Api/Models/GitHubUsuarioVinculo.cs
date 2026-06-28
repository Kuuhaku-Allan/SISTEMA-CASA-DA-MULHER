using System;
using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.Models;

public class GitHubUsuarioVinculo
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser? ApplicationUser { get; set; }

    [Required]
    [MaxLength(100)]
    public string GitHubUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string GitHubLogin { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GitHubAvatarUrl { get; set; }

    [MaxLength(500)]
    public string? GitHubProfileUrl { get; set; }

    // O Token Criptografado via IDataProtector
    [Required]
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    public string? RefreshTokenEncrypted { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    [MaxLength(50)]
    public string TokenType { get; set; } = "bearer";

    [MaxLength(500)]
    public string? Scopes { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = "GitHub";

    [MaxLength(50)]
    public string AppMode { get; set; } = "OAuthApp";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? RevogadoEm { get; set; }

    public DateTime? UltimoUsoEm { get; set; }
}
