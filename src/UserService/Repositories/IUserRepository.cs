using UserService.Models.Entities;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByCognitoSubAsync(string sub);
    }
}
