using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OficinaBrasil.Application;
using OficinaBrasil.Domain;

namespace OficinaBrasil.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IWorkshopData data;
    private readonly IConfiguration config;
    public AuthController(IWorkshopData data, IConfiguration config)
    {
        this.data = data;
        this.config = config;
    }

    private readonly PasswordHasher<User> hasher = new();
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginInput x)
    {
        // [Etapa 1: localizar o funcionário e validar a senha e o acesso ativo]
        var user = await data.FindUserByEmail(x.Email.Trim().ToLowerInvariant());

        if (user == null || !user.Active || hasher.VerifyHashedPassword(user, user.PasswordHash, x.Password) == PasswordVerificationResult.Failed)
            return Unauthorized();

        // [Etapa 2: preparar a assinatura e as informações de identidade e papel do JWT]
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddHours(2), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
       
        // [Etapa 3: retornar o token com validade de duas horas]
        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token)
        });

    }

    [HttpPost("employees")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Register(UserInput x)
    {
        // [Etapa 1: validar e-mail único, dados obrigatórios, senha e papel]
        if (await data.FindUserByEmail(x.Email) != null)
            throw new BusinessException("E-mail já utilizado");

        Checks.Required(x.Name, "Nome");
        
        if (string.IsNullOrWhiteSpace(x.Email) || !x.Email.Contains('@'))
            throw new BusinessException("E-mail inválido");
        if ((x.Password ?? "").Length< 8)
            throw new BusinessException("Senha deve ter 8 caracteres ou mais");
        if (!Enum.IsDefined(x.Role))
            throw new BusinessException("Cargo inválido");

        // [Etapa 2: criar o funcionário e armazenar somente o hash da senha]
        var user = new User
        {
            Name = x.Name, Email = x.Email.Trim().ToLowerInvariant(), Role = x.Role
        };

        user.PasswordHash = hasher.HashPassword(user, x.Password!);
        // [Etapa 3: persistir o cadastro e retornar os dados sem a senha]
        await data.Users.Add(user);
        await data.Save();

        return Ok(new
        {
            user.Id, user.Name, user.Email, user.Role
        });
    }
}
