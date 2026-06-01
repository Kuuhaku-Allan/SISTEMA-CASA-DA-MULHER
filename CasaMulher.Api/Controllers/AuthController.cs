using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string AuthenticatorIssuer = "Casa da Mulher";
    private static readonly TimeSpan LoginTemporarioValidade = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConviteCodigoService _codigoService;
    private readonly IFuncionarioIdentificadorService _identificadorService;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _loginDoisFatoresProtector;

    public AuthController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConviteCodigoService codigoService,
        IFuncionarioIdentificadorService identificadorService,
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _codigoService = codigoService;
        _identificadorService = identificadorService;
        _configuration = configuration;
        _loginDoisFatoresProtector = dataProtectionProvider.CreateProtector("CasaMulher.LoginDoisFatores");
    }

    [AllowAnonymous]
    [HttpPost("register-funcionario")]
    public async Task<IActionResult> RegisterFuncionario(RegisterFuncionarioRequest request)
    {
        if (request.Senha != request.ConfirmarSenha)
        {
            return BadRequest(new { mensagem = "Senha e confirmacao de senha nao conferem." });
        }

        var email = request.Email.Trim();
        var codigoHash = _codigoService.GerarHash(request.CodigoCadastro);
        var convite = await _dbContext.FuncionariosConvites
            .SingleOrDefaultAsync(item => item.CodigoHash == codigoHash);

        if (convite is null || !_codigoService.CodigoCorresponde(request.CodigoCadastro, convite.CodigoHash))
        {
            return BadRequest(new { mensagem = "Codigo de cadastro invalido." });
        }

        if (convite.Cancelado)
        {
            return BadRequest(new { mensagem = "Codigo de cadastro cancelado." });
        }

        if (convite.Usado)
        {
            return BadRequest(new { mensagem = "Codigo de cadastro ja utilizado." });
        }

        if (convite.ExpiraEm < DateTime.UtcNow)
        {
            return BadRequest(new { mensagem = "Codigo de cadastro expirado." });
        }

        if (!string.Equals(convite.Email.Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "E-mail informado nao corresponde ao convite." });
        }

        if (!PerfisAcesso.EhValido(convite.Perfil))
        {
            return BadRequest(new { mensagem = "Perfil do convite invalido." });
        }

        var usuarioExistente = await _userManager.FindByEmailAsync(email);

        if (usuarioExistente is not null)
        {
            return BadRequest(new { mensagem = "Ja existe usuario cadastrado com este e-mail." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        if (!await _roleManager.RoleExistsAsync(convite.Perfil))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(convite.Perfil));

            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    mensagem = "Nao foi possivel preparar o perfil do usuario.",
                    erros = roleResult.Errors.Select(error => error.Description)
                });
            }
        }

        var identificadorFuncionario = await _identificadorService.GerarProximoAsync(convite.Perfil);
        var usuario = new ApplicationUser
        {
            NomeCompleto = request.NomeCompleto.Trim(),
            Email = email,
            UserName = identificadorFuncionario,
            IdentificadorFuncionario = identificadorFuncionario,
            Perfil = convite.Perfil,
            EmailConfirmed = true,
            Ativo = true,
            DoisFatoresObrigatorio = PerfilExigeDoisFatores(convite.Perfil)
        };

        var createResult = await _userManager.CreateAsync(usuario, request.Senha);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Nao foi possivel cadastrar o funcionario.",
                erros = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleAssignResult = await _userManager.AddToRoleAsync(usuario, convite.Perfil);

        if (!roleAssignResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Nao foi possivel vincular o perfil ao funcionario.",
                erros = roleAssignResult.Errors.Select(error => error.Description)
            });
        }

        convite.Usado = true;
        convite.UsadoEm = DateTime.UtcNow;
        convite.UsuarioId = usuario.Id;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            mensagem = "Funcionario cadastrado com sucesso.",
            identificadorFuncionario = usuario.IdentificadorFuncionario
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var usuario = await EncontrarUsuarioParaLogin(request);

        if (usuario is null || !usuario.Ativo)
        {
            if (usuario is not null && !usuario.Ativo)
            {
                return Unauthorized(new { mensagem = "Usuario desativado. Procure a coordenacao." });
            }

            return Unauthorized(new { mensagem = "Identificador ou senha invalidos." });
        }

        var senhaValida = await _userManager.CheckPasswordAsync(usuario, request.Senha);

        if (!senhaValida)
        {
            return Unauthorized(new { mensagem = "Identificador ou senha invalidos." });
        }

        var roles = await _userManager.GetRolesAsync(usuario);

        if (usuario.TwoFactorEnabled)
        {
            return Ok(GerarRespostaDoisFatores(usuario, roles));
        }

        return Ok(GerarAuthResponse(usuario, roles));
    }

    [AllowAnonymous]
    [HttpPost("login-2fa")]
    public async Task<ActionResult<AuthResponse>> LoginDoisFatores(LoginDoisFatoresRequest request)
    {
        var usuario = await ObterUsuarioDoLoginTemporario(request.LoginTemporario);

        if (usuario is null || !usuario.Ativo || !usuario.TwoFactorEnabled)
        {
            return Unauthorized(new { mensagem = "Login temporario invalido ou expirado." });
        }

        var codigo = NormalizarCodigoDoisFatores(request.Codigo);
        var valido = await _userManager.VerifyTwoFactorTokenAsync(
            usuario,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            codigo);

        if (!valido)
        {
            return Unauthorized(new { mensagem = "Codigo do autenticador invalido." });
        }

        var roles = await _userManager.GetRolesAsync(usuario);
        return Ok(GerarAuthResponse(usuario, roles));
    }

    [Authorize]
    [HttpPost("2fa/iniciar-configuracao")]
    public async Task<ActionResult<DoisFatoresConfiguracaoResponse>> IniciarConfiguracaoDoisFatores()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (usuario.TwoFactorEnabled)
        {
            return BadRequest(new { mensagem = "Dois fatores ja esta ativo para este usuario." });
        }

        await _userManager.ResetAuthenticatorKeyAsync(usuario);
        var chave = await _userManager.GetAuthenticatorKeyAsync(usuario);

        if (string.IsNullOrWhiteSpace(chave))
        {
            return BadRequest(new { mensagem = "Nao foi possivel gerar chave do autenticador." });
        }

        var uri = GerarAuthenticatorUri(usuario, chave);

        return Ok(new DoisFatoresConfiguracaoResponse
        {
            ChaveManual = FormatarChaveManual(chave),
            AuthenticatorUri = uri,
            QrCodeData = uri
        });
    }

    [Authorize]
    [HttpPost("2fa/confirmar")]
    public async Task<IActionResult> ConfirmarDoisFatores(ConfirmarDoisFatoresRequest request)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        var codigo = NormalizarCodigoDoisFatores(request.Codigo);
        var valido = await _userManager.VerifyTwoFactorTokenAsync(
            usuario,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            codigo);

        if (!valido)
        {
            return BadRequest(new { mensagem = "Codigo do autenticador invalido." });
        }

        await _userManager.SetTwoFactorEnabledAsync(usuario, true);

        return Ok(new { mensagem = "Dois fatores ativado com sucesso." });
    }

    [Authorize]
    [HttpPost("2fa/desativar")]
    public async Task<IActionResult> DesativarDoisFatores()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (usuario.DoisFatoresObrigatorio)
        {
            return BadRequest(new { mensagem = "Dois fatores e obrigatorio para este perfil." });
        }

        await _userManager.SetTwoFactorEnabledAsync(usuario, false);
        await _userManager.ResetAuthenticatorKeyAsync(usuario);

        return Ok(new { mensagem = "Dois fatores desativado." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioAtualResponse>> Me()
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(new UsuarioAtualResponse
        {
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        });
    }

    [Authorize]
    [HttpPost("trocar-senha-obrigatoria")]
    public async Task<IActionResult> TrocarSenhaObrigatoria(TrocarSenhaObrigatoriaRequest request)
    {
        var usuario = await ObterUsuarioAtual();

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (request.NovaSenha != request.ConfirmarNovaSenha)
        {
            return BadRequest(new { mensagem = "Nova senha e confirmacao nao conferem." });
        }

        var result = await _userManager.ChangePasswordAsync(usuario, request.SenhaAtual, request.NovaSenha);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Nao foi possivel trocar a senha.",
                erros = result.Errors.Select(error => error.Description)
            });
        }

        usuario.DeveTrocarSenha = false;
        await _userManager.UpdateAsync(usuario);

        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }

    private async Task<ApplicationUser?> EncontrarUsuarioParaLogin(LoginRequest request)
    {
        var identificador = request.Identificador.Trim();

        if (string.IsNullOrWhiteSpace(identificador))
        {
            identificador = request.Email.Trim();
        }

        if (string.IsNullOrWhiteSpace(identificador))
        {
            return null;
        }

        if (identificador.Contains('@'))
        {
            return await _userManager.FindByEmailAsync(identificador);
        }

        var identificadorNormalizado = identificador.ToUpperInvariant();

        return await _dbContext.Users.SingleOrDefaultAsync(usuario =>
            usuario.NormalizedUserName == identificadorNormalizado
            || usuario.IdentificadorFuncionario.ToUpper() == identificadorNormalizado);
    }

    private static bool PerfilExigeDoisFatores(string perfil)
    {
        return string.Equals(perfil, PerfisAcesso.Adm, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.Juridico, StringComparison.OrdinalIgnoreCase)
            || string.Equals(perfil, PerfisAcesso.AssistenteSocial, StringComparison.OrdinalIgnoreCase);
    }

    private AuthResponse GerarRespostaDoisFatores(ApplicationUser usuario, IEnumerable<string> roles)
    {
        return new AuthResponse
        {
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = roles.FirstOrDefault() ?? usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            RequerDoisFatores = true,
            LoginTemporario = GerarLoginTemporario(usuario),
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        };
    }

    private AuthResponse GerarAuthResponse(ApplicationUser usuario, IEnumerable<string> roles)
    {
        return new AuthResponse
        {
            Token = GerarJwt(usuario, roles),
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = roles.FirstOrDefault() ?? usuario.Perfil,
            IdentificadorFuncionario = usuario.IdentificadorFuncionario,
            RequerDoisFatores = false,
            DoisFatoresObrigatorio = usuario.DoisFatoresObrigatorio,
            DoisFatoresAtivado = usuario.TwoFactorEnabled,
            DeveTrocarSenha = usuario.DeveTrocarSenha
        };
    }

    private string GerarJwt(ApplicationUser usuario, IEnumerable<string> roles)
    {
        var key = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configure Jwt:Key para gerar tokens.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.NomeCompleto),
            new("perfil", usuario.Perfil),
            new("identificadorFuncionario", usuario.IdentificadorFuncionario)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expirationHours = _configuration.GetValue("Jwt:ExpirationHours", 8);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GerarLoginTemporario(ApplicationUser usuario)
    {
        var ticket = new LoginTemporarioTicket(
            usuario.Id,
            usuario.SecurityStamp ?? string.Empty,
            DateTimeOffset.UtcNow);

        return _loginDoisFatoresProtector.Protect(JsonSerializer.Serialize(ticket));
    }

    private async Task<ApplicationUser?> ObterUsuarioDoLoginTemporario(string loginTemporario)
    {
        try
        {
            var json = _loginDoisFatoresProtector.Unprotect(loginTemporario);
            var ticket = JsonSerializer.Deserialize<LoginTemporarioTicket>(json);

            if (ticket is null || DateTimeOffset.UtcNow - ticket.EmitidoEm > LoginTemporarioValidade)
            {
                return null;
            }

            var usuario = await _userManager.FindByIdAsync(ticket.UsuarioId);

            if (usuario is null || !string.Equals(usuario.SecurityStamp, ticket.SecurityStamp, StringComparison.Ordinal))
            {
                return null;
            }

            return usuario;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ApplicationUser?> ObterUsuarioAtual()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(usuarioId);
    }

    private static string NormalizarCodigoDoisFatores(string codigo)
    {
        return codigo.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string GerarAuthenticatorUri(ApplicationUser usuario, string chave)
    {
        var conta = string.IsNullOrWhiteSpace(usuario.IdentificadorFuncionario)
            ? usuario.Email ?? usuario.Id
            : usuario.IdentificadorFuncionario;

        return "otpauth://totp/"
            + $"{Uri.EscapeDataString(AuthenticatorIssuer)}:{Uri.EscapeDataString(conta)}"
            + $"?secret={Uri.EscapeDataString(chave)}"
            + $"&issuer={Uri.EscapeDataString(AuthenticatorIssuer)}"
            + "&digits=6";
    }

    private static string FormatarChaveManual(string chave)
    {
        return string.Join(" ", chave.Chunk(4).Select(grupo => new string(grupo)));
    }

    private sealed record LoginTemporarioTicket(string UsuarioId, string SecurityStamp, DateTimeOffset EmitidoEm);
}
