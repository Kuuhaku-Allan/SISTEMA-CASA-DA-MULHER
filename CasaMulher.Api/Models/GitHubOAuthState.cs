using System;
using System.ComponentModel.DataAnnotations;

namespace CasaMulher.Api.Models;

public class GitHubOAuthState
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string StateHash { get; set; } = string.Empty;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ReturnUrl { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiraEm { get; set; }

    public DateTime? UsadoEm { get; set; }

    [MaxLength(50)]
    public string? IpSolicitante { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }
}
