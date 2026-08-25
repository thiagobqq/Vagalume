using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<AppUser> userManager, AppDbContext db, ITokenService tokenService)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(GenericResponseData<object>.Fail("E-mail já cadastrado"));

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Senha);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(GenericResponseData<object>.Fail(msg));
        }

        var dto = new UsuarioDto { Id = user.Id, Nome = user.Nome, Email = user.Email! };
        return StatusCode(201, GenericResponseData<UsuarioDto>.Ok(dto, "Usuário criado com sucesso"));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(GenericResponseData<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(GenericResponseData<object>.Fail("Credenciais inválidas"));

        var valid = await _userManager.CheckPasswordAsync(user, request.Senha);
        if (!valid)
            return Unauthorized(GenericResponseData<object>.Fail("Credenciais inválidas"));

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refresh = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refresh.Token,
            Usuario = new UsuarioDto { Id = user.Id, Nome = user.Nome, Email = user.Email! }
        };

        return Ok(GenericResponseData<AuthResponseDto>.Ok(response, "Login realizado com sucesso"));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(GenericResponseData<RefreshResponseDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var novo = await _tokenService.ValidateAndRotateAsync(request.RefreshToken);
        if (novo == null)
            return Unauthorized(GenericResponseData<object>.Fail("Refresh token inválido ou expirado"));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == novo.UsuarioId);
        if (user == null)
            return Unauthorized(GenericResponseData<object>.Fail("Usuário não encontrado"));

        var accessToken = _tokenService.GenerateAccessToken(user);

        var response = new RefreshResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = novo.Token
        };

        return Ok(GenericResponseData<RefreshResponseDto>.Ok(response, "Token atualizado com sucesso"));
    }
}
