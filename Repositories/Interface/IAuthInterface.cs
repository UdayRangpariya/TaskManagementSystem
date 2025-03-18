using System.Collections.Generic;
using System.Threading.Tasks;
using Repositories.Model;

namespace Repositories.Interface
{
    public interface IAuthInterface
    {
        Task<User> RegisterUserAsync(Register registerModel);
        Task<User> LoginUserAsync(Login loginModel);
        Task<List<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(int userId);
        Task<User> UpdateUserAsync(int userId, UserUpdate userUpdate);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> DeleteUserAsync(int userId);
        Task<SystemStatistics> GetSystemStatisticsAsync();
    }
}