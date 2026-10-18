using Starter.Application.Common.Persistence;
using Starter.Domain.Users;

namespace Starter.Application.Users;

public interface IUserRepository : IRepository<User, UserFilter>;
