using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models.Entities;

namespace UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _dbContext;

        public UserRepository(UserDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(User user)
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByCognitoSubAsync(string sub)
        {
            return _dbContext.Users.FirstOrDefaultAsync(u => u.CognitoSub == sub);
        }
    }
}
