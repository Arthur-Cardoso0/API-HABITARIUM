using System.ComponentModel.DataAnnotations;

namespace Habitarium.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}