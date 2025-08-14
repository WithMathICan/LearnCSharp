using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvancedPolymorphism {
    public abstract class BaseRepository<T> where T : Entity {
        public virtual async Task<IEnumerable<T>> GetAllAsync() {
            // Симулируем базовую реализацию: возвращаем пустой список
            Console.WriteLine("BaseRepository.GetAllAsync called");
            return new List<T>();
        }
    }

    public class UserRepository : BaseRepository<User> {
        public override async Task<IEnumerable<User>> GetAllAsync() {
            Console.WriteLine("UserRepository.GetAllAsync called");
            // Реальная логика: загрузка пользователей из БД
            return new List<User> { new User { Id = 1, Name = "Admin" } };
        }
    }

    public class AdminUserRepository : UserRepository {  
        public new async Task<List<AdminUser>> GetAllAsync() {
            Console.WriteLine("AdminUserRepository.GetAllAsync called");
            // Логика для админов
            return new List<AdminUser> { new AdminUser { Id = 1, Name = "SuperAdmin", Role = "Admin" } };
        }
    }

    public class Entity {
        public int Id { get; set; }
    }

    public class User : Entity {
        public string Name { get; set; }
    }

    public class AdminUser : User {
        public string Role { get; set; }
    }

    public class Service {
        private readonly BaseRepository<User> _repository;

        public Service(BaseRepository<User> repository) {
            _repository = repository;
        }

        public async Task ProcessUsersAsync() {
            var users = await _repository.GetAllAsync();
            foreach (var user in users) {
                Console.WriteLine($"Processing user: {user.Name}");
                if (user is AdminUser admin) {
                    Console.WriteLine($"Admin role: {admin.Role}");
                }
            }
        }
    }

    internal class Program {
        public static async Task Main() {
            // Внедрение через DI в ASP.NET стиле
            var serviceWithUserRepo = new Service(new UserRepository());
            await serviceWithUserRepo.ProcessUsersAsync(); // Что выведет?

            var serviceWithAdminRepo = new Service(new AdminUserRepository());
            await serviceWithAdminRepo.ProcessUsersAsync(); // Что выведет? Почему?
        }
    }
}