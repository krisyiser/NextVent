using System.Collections.Generic;
using System.Threading.Tasks;
using NextVent.Core.Models;

namespace NextVent.Core.Repositories;

public interface IUserRepository
{
    Task<List<UserModel>> GetActiveUsersAsync();
    Task<UserModel?> ValidatePinAsync(string username, string pin4Digits);
    Task<UserModel?> ValidateAnyPinAsync(string pin4Digits);
    Task<bool> ValidateAdminPinAsync(string pin4Digits);
}
