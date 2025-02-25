using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Services;
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Result<LoginResponse>.Failure("Email and password are required.");
            }

            // Retrieve the user
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !user.VerifyPassword(request.Password))
            {
                return Result<LoginResponse>.Failure("Invalid email or password.");
            }

            // Generate token
            var token = GenerateJwtToken(user);
            _logger.LogInformation("User {Email} logged in successfully.", request.Email);

            return Result<LoginResponse>.Success(new LoginResponse
            {
                Token = token,
                UserId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return Result<LoginResponse>.Failure("An error occurred during login.");
        }
    }


    public async Task<Result<string>> RegisterAsync(string email, string password)
    {
        try
        {
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
                return Result<string>.Failure("User with this email already exists.");

            var user = new User(email, password);
            await _userRepository.AddAsync(user);
            _logger.LogInformation("User {Email} registered successfully.", email);

            return Result<string>.Success("User registered successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", email);
            return Result<string>.Failure("An unexpected error occurred during registration.");
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
