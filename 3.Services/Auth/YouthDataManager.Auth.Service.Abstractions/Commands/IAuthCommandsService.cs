using System.Threading.Tasks;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Auth.Service.Abstractions.Commands;

public interface IAuthCommandsService
{
    Task<ServiceResult<LoginResponse>> Login(LoginRequest request);
    Task<ServiceResult<LoginResponse>> RefreshToken(RefreshTokenRequest request);
    Task<ServiceResult<LoginResponse>> ChangePassword(System.Guid userId, ChangePasswordRequest request);
}
