using System.Threading.Tasks;
using Repositories.Model;

namespace Repositories.Interface
{
    public interface IAuth
    {
        Task<User> RegisterUserAsync(Register registerModel);
        Task<User> LoginUserAsync(Login loginModel);
    }
}