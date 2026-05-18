using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CasaMulher.Api.Data;
using CasaMulher.Api.DTOs;
using CasaMulher.Api.Models;
using CasaMulher.Api.Security;
using CasaMulher.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CasaMulher.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConviteCodigoService _codigoService;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConviteCodigoService codigoService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _codigoService = codigoService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("register-funcionario")]
    public async Task<IActionResult> RegisterFuncionario(RegisterFuncionarioRequest request)
    {
        if (request.Senha != request.ConfirmarSenha)
        {
            return BadRequest(new { mensagem = "Senha e confirmação de senha não conferem." });
        }

        var email = request.Email.Trim();
        var codigoHash = _codigoService.GerarHash(request.CodigoCadastro);
        var convite = await _dbContext.FuncionariosConvites
            .SingleOrDefaultAsync(item => item.CodigoHash == codigoHash);

        if (convite is null || !_codigoService.CodigoCorresponde(request.CodigoCadastro, convite.CodigoHash))
        {
            return BadRequest(new { mensagem = "Código de cadastro inválido." });
        }

        if (convite.Usado)
        {
            return BadRequest(new { mensagem = "Código de cadastro já utilizado." });
        }

        if (convite.ExpiraEm < DateTime.UtcNow)
        {
            return BadRequest(new { mensagem = "Código de cadastro expirado." });
        }

        if (!string.Equals(convite.Email.Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensagem = "E-mail informado não corresponde ao convite." });
        }

        if (!PerfisAcesso.EhValido(convite.Perfil))
        {
            return BadRequest(new { mensagem = "Perfil do convite inválido." });
        }

        var usuarioExistente = await _userManager.FindByEmailAsync(email);

        if (usuarioExistente is not null)
        {
            return BadRequest(new { mensagem = "Já existe usuário cadastrado com este e-mail." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        if (!await _roleManager.RoleExistsAsync(convite.Perfil))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(convite.Perfil));

            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    mensagem = "Não foi possível preparar o perfil do usuário.",
                    erros = roleResult.Errors.Select(error => error.Description)
                });
            }
        }

        var usuario = new ApplicationUser
        {
            NomeCompleto = request.NomeCompleto.Trim(),
            Email = email,
            UserName = email,
            Perfil = convite.Perfil,
            EmailConfirmed = true,
            Ativo = true
        };

        var createResult = await _userManager.CreateAsync(usuario, request.Senha);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível cadastrar o funcionário.",
                erros = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleAssignResult = await _userManager.AddToRoleAsync(usuario, convite.Perfil);

        if (!roleAssignResult.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível vincular o perfil ao funcionário.",
                erros = roleAssignResult.Errors.Select(error => error.Description)
            });
        }

        convite.Usado = true;
        convite.UsadoEm = DateTime.UtcNow;
        convite.UsuarioId = usuario.Id;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { mensagem = "Funcionário cadastrado com sucesso." });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var usuario = await _userManager.FindByEmailAsync(email);

        if (usuario is null || !usuario.Ativo)
        {
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });
        }

        var senhaValida = await _userManager.CheckPasswordAsync(usuario, request.Senha);

        if (!senhaValida)
        {
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });
        }

        var roles = await _userManager.GetRolesAsync(usuario);
        var perfil = roles.FirstOrDefault() ?? usuario.Perfil;

        return Ok(new AuthResponse
        {
            Token = GerarJwt(usuario, roles),
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email ?? string.Empty,
            Perfil = perfil
        });
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
            new("perfil", usuario.Perfil)
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
}
