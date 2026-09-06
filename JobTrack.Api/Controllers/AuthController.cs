using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobTrack.Api.Configuration;
using JobTrack.Api.Contracts;
using JobTrack.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JobTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwt;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwt = jwtOptions.Value;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponse>> Register(
        RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new ValidationProblemDetails(
                result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.Description).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Registration failed."
            });
        }

        return Ok(CreateToken(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email address or password is incorrect.");
        }

        return Ok(CreateToken(user));
    }

    private AuthenticationResponse CreateToken(ApplicationUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expires,
            signingCredentials: credentials);

        return new AuthenticationResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            (int)TimeSpan.FromMinutes(_jwt.ExpirationMinutes).TotalSeconds,
            user.Email!);
    }
}
