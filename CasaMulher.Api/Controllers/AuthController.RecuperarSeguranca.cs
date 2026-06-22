using System.Security.Cryptography;
using System.Text;
using CasaMulher.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CasaMulher.Api.Controllers;

public sealed record RecuperarSegurancaSolicitarRequest(string Identificador, string Senha, string DestinoEmail);
public sealed record RecuperarSegurancaConfirmarRequest(string Token, bool Redefinir2fa, bool RedefinirPasskeys);
public sealed record RecuperarSegurancaOpcoesRequest(string Identificador, string Senha);

public partial class AuthController
{
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    [HttpPost("recuperar-seguranca/opcoes")]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarSegurancaOpcoes([FromBody] RecuperarSegurancaOpcoesRequest request)
    {
        var falhaGenerica = BadRequest(new { mensagem = "ID ou senha incorretos." });

        if (string.IsNullOrWhiteSpace(request.Identificador) || string.IsNullOrWhiteSpace(request.Senha))
            return falhaGenerica;

        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.IdentificadorFuncionario == request.Identificador && u.Ativo);
        if (user == null)
            return falhaGenerica;

        var senhaValida = await _userManager.CheckPasswordAsync(user, request.Senha);
        if (!senhaValida)
            return falhaGenerica;

        var tem2fa = user.TwoFactorEnabled || await _dbContext.UserTokens.AnyAsync(t => t.UserId == user.Id && t.LoginProvider == "[AspNetUserStore]" && t.Name == "AuthenticatorKey");
        var temPasskeys = await _dbContext.PasskeyCredentials.AnyAsync(p => p.UserId == user.Id);

        if (!tem2fa && !temPasskeys)
        {
            return BadRequest(new { mensagem = "Não há métodos de segurança avançados cadastrados para esta conta." });
        }

        var opcoes = new List<object>();

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            opcoes.Add(new { id = "principal", mascarado = MascararEmail(user.Email) });
        }

        if (user.EmailRecuperacaoConfirmado && !string.IsNullOrWhiteSpace(user.EmailRecuperacao))
        {
            opcoes.Add(new { id = "recuperacao", mascarado = MascararEmail(user.EmailRecuperacao) });
        }

        if (opcoes.Count == 0)
        {
            return BadRequest(new { mensagem = "Você não possui nenhum e-mail cadastrado ou confirmado para receber o link. Procure a coordenação." });
        }

        return Ok(new { opcoes });
    }

    [HttpPost("recuperar-seguranca/solicitar")]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarSegurancaSolicitar([FromBody] RecuperarSegurancaSolicitarRequest request)
    {
        var falhaGenerica = BadRequest(new { mensagem = "Não foi possível iniciar a recuperação. Verifique os dados informados." });

        if (string.IsNullOrWhiteSpace(request.Identificador) || string.IsNullOrWhiteSpace(request.Senha))
            return falhaGenerica;

        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.IdentificadorFuncionario == request.Identificador && u.Ativo);
        if (user == null)
            return falhaGenerica;

        var senhaValida = await _userManager.CheckPasswordAsync(user, request.Senha);
        if (!senhaValida)
            return falhaGenerica;

        var tem2fa = user.TwoFactorEnabled || await _dbContext.UserTokens.AnyAsync(t => t.UserId == user.Id && t.LoginProvider == "[AspNetUserStore]" && t.Name == "AuthenticatorKey");
        var temPasskeys = await _dbContext.PasskeyCredentials.AnyAsync(p => p.UserId == user.Id);

        if (!tem2fa && !temPasskeys)
        {
            return BadRequest(new { mensagem = "Não há métodos de segurança avançados cadastrados para esta conta." });
        }

        string emailDestino;
        if (request.DestinoEmail == "recuperacao")
        {
            if (!user.EmailRecuperacaoConfirmado || string.IsNullOrWhiteSpace(user.EmailRecuperacao))
                return falhaGenerica;
            emailDestino = user.EmailRecuperacao;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return falhaGenerica;
            emailDestino = user.Email;
        }

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = HashToken(token);

        var recuperacao = new RecuperacaoSegurancaToken
        {
            FuncionarioId = user.Id,
            TokenHash = tokenHash,
            Tipo = "RecuperacaoSeguranca",
            EmailDestino = emailDestino,
            ExpiraEm = DateTime.UtcNow.AddMinutes(30),
            IpSolicitante = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
        };

        _dbContext.RecuperacaoSegurancaTokens.Add(recuperacao);
        await _dbContext.SaveChangesAsync();

        var linkRelativo = $"confirmar-recuperacao-seguranca.html?token={Uri.EscapeDataString(token)}";
        var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5500";
        var linkAbsoluto = $"{baseUrl.TrimEnd('/')}/{linkRelativo}";

        var corpoHtml = $"""
            <p>Olá, {WebUtility.HtmlEncode(user.NomeCompleto)}.</p>
            <p>Recebemos uma solicitação para redefinir os métodos de segurança da sua conta no Sistema Casa da Mulher de Itaquaquecetuba.</p>
            <p>Se foi você, clique no link abaixo para continuar:</p>
            <p><a href="{WebUtility.HtmlEncode(linkAbsoluto)}" style="display:inline-block;padding:12px 18px;background:#18726b;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;">Recuperar Métodos de Segurança</a></p>
            <p>Esse link expira em 30 minutos e só pode ser usado uma vez.</p>
            <p>Se você não solicitou isso, ignore este e-mail e avise a coordenação.</p>
            """;

        await _emailService.EnviarAsync(emailDestino, "Recuperação dos métodos de segurança - Sistema Casa da Mulher", corpoHtml, "RecuperacaoSeguranca");

        await _auditoriaService.RegistrarAsync("SEGURANCA_RECUPERACAO_SOLICITADA", "ApplicationUser", user.Id, $"Recuperação de segurança solicitada via e-mail {request.DestinoEmail}");
        await _auditoriaService.RegistrarAsync("SEGURANCA_RECUPERACAO_EMAIL_ENVIADO", "ApplicationUser", user.Id, $"E-mail de recuperação de segurança enviado para {MascararEmail(emailDestino)}");

        return Ok(new { mensagem = "Enviamos um link de recuperação para o e-mail selecionado." });
    }

    [HttpGet("recuperar-seguranca/detalhes")]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarSegurancaDetalhes([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest();

        var hash = HashToken(token);
        var recuperacao = await _dbContext.RecuperacaoSegurancaTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (recuperacao == null || recuperacao.UsadoEm != null || recuperacao.ExpiraEm < DateTime.UtcNow)
        {
            return BadRequest(new { erro = "INVALID_TOKEN", mensagem = "Link inválido ou expirado." });
        }

        var user = await _userManager.FindByIdAsync(recuperacao.FuncionarioId);
        if (user == null || !user.Ativo)
            return BadRequest(new { erro = "INVALID_TOKEN", mensagem = "Link inválido ou expirado." });

        var tem2fa = user.TwoFactorEnabled || await _dbContext.UserTokens.AnyAsync(t => t.UserId == user.Id && t.LoginProvider == "[AspNetUserStore]" && t.Name == "AuthenticatorKey");
        var temPasskeys = await _dbContext.PasskeyCredentials.AnyAsync(p => p.UserId == user.Id);

        await _auditoriaService.RegistrarAsync("SEGURANCA_RECUPERACAO_TOKEN_VALIDADO", "ApplicationUser", user.Id, $"Token de recuperação validado");

        return Ok(new
        {
            identificador = MascararEmail(user.Email ?? user.IdentificadorFuncionario),
            tem2fa,
            temPasskeys
        });
    }

    [HttpPost("recuperar-seguranca/confirmar")]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarSegurancaConfirmar([FromBody] RecuperarSegurancaConfirmarRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest();

        var hash = HashToken(request.Token);
        var recuperacao = await _dbContext.RecuperacaoSegurancaTokens.SingleOrDefaultAsync(t => t.TokenHash == hash);

        if (recuperacao == null || recuperacao.UsadoEm != null || recuperacao.ExpiraEm < DateTime.UtcNow)
        {
            return BadRequest(new { erro = "INVALID_TOKEN", mensagem = "Link inválido ou expirado." });
        }

        var user = await _userManager.FindByIdAsync(recuperacao.FuncionarioId);
        if (user == null || !user.Ativo)
            return BadRequest(new { erro = "INVALID_TOKEN", mensagem = "Link inválido ou expirado." });

        if (!request.Redefinir2fa && !request.RedefinirPasskeys)
            return BadRequest(new { mensagem = "Nenhuma opção foi selecionada." });

        if (request.Redefinir2fa)
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            
            // Invalidate current logins if needed (update security stamp)
            await _userManager.UpdateSecurityStampAsync(user);
            
            await _auditoriaService.RegistrarAsync("SEGURANCA_2FA_REDEFINIDO_POR_RECUPERACAO", "ApplicationUser", user.Id, "2FA redefinido por fluxo de recuperação");
        }

        if (request.RedefinirPasskeys)
        {
            var passkeys = await _dbContext.PasskeyCredentials.Where(p => p.UserId == user.Id).ToListAsync();
            if (passkeys.Any())
            {
                _dbContext.PasskeyCredentials.RemoveRange(passkeys);
                await _dbContext.SaveChangesAsync();
            }
            await _auditoriaService.RegistrarAsync("SEGURANCA_PASSKEYS_REDEFINIDAS_POR_RECUPERACAO", "ApplicationUser", user.Id, "Passkeys redefinidas por fluxo de recuperação");
        }

        user.SecuritySetupRequired = true;
        await _userManager.UpdateAsync(user);

        recuperacao.UsadoEm = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync("SEGURANCA_RECUPERACAO_CONCLUIDA", "ApplicationUser", user.Id, "Recuperação de segurança concluída com sucesso");

        try
        {
            await _securitySnapshot.PersistAsync("security_recovery_completed", HttpContext.RequestAborted);
        }
        catch
        {
            // Log silently or ignore, we don't want to break the flow if HML fails
        }

        return Ok(new { mensagem = "Métodos de segurança redefinidos com sucesso." });
    }
}
