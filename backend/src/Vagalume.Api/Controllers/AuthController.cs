using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Vagalume.Api.Extensions;
using Vagalume.Application.Common;
using Vagalume.Application.DTOs;
using Vagalume.Domain.Abstractions;
using Vagalume.Domain.Authorization;
using Vagalume.Domain.Entities;
using Vagalume.Infra.Data;
using Vagalume.Infra.Services;
using Vagalume.Infra.Services.Auth;

namespace Vagalume.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEntitlementService _entitlements;
    private readonly IGoogleTokenValidator _googleTokens;
    private readonly IExternalLoginService _externalLogins;
    private readonly IDateTimeProvider _clock;

    public AuthController(
        UserManager<AppUser> userManager,
        AppDbContext db,
        ITokenService tokenService,
        IEntitlementService entitlements,
        IGoogleTokenValidator googleTokens,
        IExternalLoginService externalLogins,
        IDateTimeProvider clock)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _entitlements = entitlements;
        _googleTokens = googleTokens;
        _externalLogins = externalLogins;
        _clock = clock;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioDto>), 201)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            return BadRequest(GenericResponseData<object>.Fail("E-mail já cadastrado"));

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            UserName = email,
            Email = email,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Senha);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(TraduzirErroIdentity));
            return BadRequest(GenericResponseData<object>.Fail(msg));
        }

        var dto = new UsuarioDto { Id = user.Id, Nome = user.Nome, Email = user.Email!, TemSenha = true };
        return StatusCode(201, GenericResponseData<UsuarioDto>.Ok(dto, "Usuário criado com sucesso"));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(GenericResponseData<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 401)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 423)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
            return Unauthorized(GenericResponseData<object>.Fail("Credenciais inválidas"));

        if (await _userManager.IsLockedOutAsync(user))
            return StatusCode(423, GenericResponseData<object>.Fail("Conta temporariamente bloqueada por excesso de tentativas. Aguarde alguns minutos."));

        var valid = await _userManager.CheckPasswordAsync(user, request.Senha);
        if (!valid)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(GenericResponseData<object>.Fail("Credenciais inválidas"));
        }

        if (user.AccessFailedCount > 0)
            await _userManager.ResetAccessFailedCountAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refresh = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refresh.Token,
            Usuario = await ToUsuarioDtoAsync(user)
        };

        return Ok(GenericResponseData<AuthResponseDto>.Ok(response, "Login realizado com sucesso"));
    }

    [HttpPost("google")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(GenericResponseData<AuthResponseDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 503)]
    public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequest request)
    {
        if (!_googleTokens.Configurado)
            return StatusCode(503, GenericResponseData<object>.Fail("Login com Google não configurado"));

        GoogleUserInfo info;
        try
        {
            info = await _googleTokens.ValidateAsync(request.IdToken);
        }
        catch (InvalidJwtException)
        {
            return BadRequest(GenericResponseData<object>.Fail("Token do Google inválido ou expirado"));
        }

        if (string.IsNullOrWhiteSpace(info.Email))
            return BadRequest(GenericResponseData<object>.Fail("E-mail do Google não disponível"));

        AppUser user;
        try
        {
            user = await _externalLogins.FindOrCreateGoogleUserAsync(info);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(GenericResponseData<object>.Fail(ex.Message));
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refresh = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refresh.Token,
            Usuario = await ToUsuarioDtoAsync(user)
        };

        return Ok(GenericResponseData<AuthResponseDto>.Ok(response, "Login realizado com sucesso"));
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(GenericResponseData<object>), 200)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        if (request?.TodasSessoes == true && User.Identity?.IsAuthenticated == true)
        {
            var usuarioId = User.GetUserId();
            await _tokenService.RevokeAllAsync(usuarioId);
        }
        else if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            await _tokenService.RevokeAsync(request.RefreshToken);
        }

        return Ok(GenericResponseData<object?>.Ok(null, "Sessão encerrada"));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 401)]
    public async Task<IActionResult> Me()
    {
        var usuarioId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (user == null)
            return Unauthorized(GenericResponseData<object>.Fail("Usuário não encontrado"));

        return Ok(GenericResponseData<UsuarioDto>.Ok(await ToUsuarioDtoAsync(user)));
    }

    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(typeof(GenericResponseData<UsuarioDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilUsuarioRequest request)
    {
        var nome = request.Nome.Trim();
        if (string.IsNullOrWhiteSpace(nome))
            return BadRequest(GenericResponseData<object>.Fail("Nome é obrigatório"));

        var usuarioId = User.GetUserId();
        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user == null)
            return Unauthorized(GenericResponseData<object>.Fail("Usuário não encontrado"));

        user.Nome = nome;
        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(GenericResponseData<object>.Fail(string.Join("; ", res.Errors.Select(TraduzirErroIdentity))));

        return Ok(GenericResponseData<UsuarioDto>.Ok(await ToUsuarioDtoAsync(user), "Perfil atualizado"));
    }

    [HttpPost("alterar-senha")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(GenericResponseData<object>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
    {
        var usuarioId = User.GetUserId();
        var user = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (user == null)
            return Unauthorized(GenericResponseData<object>.Fail("Usuário não encontrado"));

        var temSenha = await _userManager.HasPasswordAsync(user);
        IdentityResult result;

        if (temSenha)
        {
            if (string.IsNullOrWhiteSpace(request.SenhaAtual))
                return BadRequest(GenericResponseData<object>.Fail("Informe sua senha atual"));
            result = await _userManager.ChangePasswordAsync(user, request.SenhaAtual, request.NovaSenha);
        }
        else
        {
            // Conta criada via Google (ou senha limpa no vínculo): permite definir senha inicial
            result = await _userManager.AddPasswordAsync(user, request.NovaSenha);
        }

        if (!result.Succeeded)
            return BadRequest(GenericResponseData<object>.Fail(string.Join("; ", result.Errors.Select(TraduzirErroIdentity))));

        await _tokenService.RevokeAllAsync(usuarioId);
        return Ok(GenericResponseData<object?>.Ok(null, "Senha atualizada com sucesso"));
    }

    [HttpGet("solicitacao-verificacao")]
    [Authorize]
    [ProducesResponseType(typeof(GenericResponseData<SolicitacaoVerificacaoDto?>), 200)]
    public async Task<IActionResult> ObterSolicitacaoVerificacao()
    {
        var usuarioId = User.GetUserId();
        var solicitacao = await _db.SolicitacoesVerificacao
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.CriadoEm)
            .FirstOrDefaultAsync();

        if (solicitacao is null)
            return Ok(GenericResponseData<SolicitacaoVerificacaoDto?>.Ok(null));

        return Ok(GenericResponseData<SolicitacaoVerificacaoDto>.Ok(ToSolicitacaoDto(solicitacao)));
    }

    [HttpPost("solicitacao-verificacao")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(GenericResponseData<SolicitacaoVerificacaoDto>), 200)]
    [ProducesResponseType(typeof(GenericResponseData<object>), 400)]
    public async Task<IActionResult> SolicitarVerificacao([FromBody] SolicitarVerificacaoRequest? request)
    {
        var usuarioId = User.GetUserId();
        var permissoes = await _entitlements.ListarAsync(usuarioId);
        if (permissoes.Contains(Permissoes.VerifyUser) || permissoes.Contains(Permissoes.Admin))
            return BadRequest(GenericResponseData<object>.Fail("Sua conta já está verificada"));

        var pendente = await _db.SolicitacoesVerificacao
            .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.Status == SolicitacaoVerificacao.StatusPendente);

        if (pendente is not null)
        {
            if (!string.IsNullOrWhiteSpace(request?.Mensagem))
            {
                pendente.Mensagem = request.Mensagem.Trim();
                await _db.SaveChangesAsync();
            }
            return Ok(GenericResponseData<SolicitacaoVerificacaoDto>.Ok(
                ToSolicitacaoDto(pendente), "Sua solicitação já está na fila de verificação"));
        }

        var nova = new SolicitacaoVerificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Status = SolicitacaoVerificacao.StatusPendente,
            Mensagem = string.IsNullOrWhiteSpace(request?.Mensagem) ? null : request.Mensagem.Trim(),
            CriadoEm = _clock.UtcNow
        };

        _db.SolicitacoesVerificacao.Add(nova);
        await _db.SaveChangesAsync();

        return Ok(GenericResponseData<SolicitacaoVerificacaoDto>.Ok(
            ToSolicitacaoDto(nova), "Solicitação de verificação enviada"));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
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

    private async Task<UsuarioDto> ToUsuarioDtoAsync(AppUser user)
    {
        var permissoes = await _entitlements.ListarAsync(user.Id);
        var temSenha = !string.IsNullOrEmpty(user.PasswordHash);
        return new UsuarioDto
        {
            Id = user.Id,
            Nome = user.Nome,
            Email = user.Email!,
            TemSenha = temSenha,
            Permissoes = permissoes.ToList()
        };
    }

    private static SolicitacaoVerificacaoDto ToSolicitacaoDto(SolicitacaoVerificacao s) => new()
    {
        Id = s.Id,
        Status = s.Status,
        Mensagem = s.Mensagem,
        CriadoEm = s.CriadoEm,
        ResolvidoEm = s.ResolvidoEm
    };

    private static string TraduzirErroIdentity(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "E-mail já cadastrado",
        "InvalidEmail" or "InvalidUserName" => "E-mail inválido",
        "PasswordTooShort" => "A senha deve ter no mínimo 6 caracteres",
        "PasswordMismatch" => "Senha atual incorreta",
        "UserAlreadyHasPassword" => "Esta conta já possui uma senha cadastrada",
        _ => error.Description
    };
}
