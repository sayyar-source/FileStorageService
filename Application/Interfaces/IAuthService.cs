using Application.DTOs;
using Domain.Commons;

namespace Application.Interfaces;
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task RegisterAsync(string email, string password);
}
