using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Data.Entities;

namespace Ticketfy.Core.Repositories;

public interface IUserRepository
{
    Task<List<UserModel>> GetActiveUsersAsync();
    Task<UserModel?> ValidatePinAsync(string username, string pin4Digits);
    Task<UserModel?> ValidateAnyPinAsync(string pin4Digits);
    Task<bool> ValidateAdminPinAsync(string pin4Digits);
    Task<bool> HasAnyUsersAsync();
    Task CreateUserAsync(UserEntity user);
    Task<UserEntity?> GetByUsernameAsync(string username);
}
