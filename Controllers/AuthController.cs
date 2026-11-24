using Habitarium.Api.Data;
using Habitarium.Api.Dtos;
using Habitarium.Api.Models;
using Habitarium.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Habitarium.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AppDbContext context, TokenService tokens) : ControllerBase
{
    private static readonly PasswordHasher<AppUser> Hasher = new();

    /// <summary>Cria uma conta e já devolve o token de acesso.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await context.AppUsers.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Este e-mail já está cadastrado." });

        var user = new AppUser { Name = request.Name.Trim(), Email = email };
        user.PasswordHash = Hasher.HashPassword(user, request.Password);

        context.AppUsers.Add(user);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dois cadastros simultâneos com o mesmo e-mail: o índice único barra o segundo.
            return Conflict(new { message = "Este e-mail já está cadastrado." });
        }

        return StatusCode(StatusCodes.Status201Created, BuildResponse(user));
    }

    /// <summary>Valida e-mail e senha e devolve o token de acesso.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

        var valid = user is not null &&
            Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                != PasswordVerificationResult.Failed;

        if (!valid)
            return Unauthorized(new { message = "E-mail ou senha inválidos." });

        return Ok(BuildResponse(user!));
    }

    /// <summary>Devolve os dados do usuário dono do token.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
            return Unauthorized();

        var user = await context.AppUsers.FindAsync(userId);
        if (user is null) return Unauthorized();

        return Ok(new { user.Id, user.Name, user.Email });
    }

    private AuthResponse BuildResponse(AppUser user)
    {
        var (token, expiresAt) = tokens.Create(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Name = user.Name,
            Email = user.Email
        };
    }
}