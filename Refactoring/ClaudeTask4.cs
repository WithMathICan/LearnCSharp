using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringTask4 {
    // ЗАДАЧА 4: Система репозиториев с проблемами generics и covariance/contravariance
    // Проблемы: неправильное использование обобщений, нарушение принципов 
    // covariance/contravariance, проблемы с интерфейсами и абстракцией

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // Проблема: интерфейс не инвариантный, хотя должен поддерживать ковариантность
    public interface IRepository<T> where T : class {
        // Проблема: возвращает конкретную реализацию вместо интерфейса
        List<T> GetAll();
        T GetById(object id); // Проблема: object вместо generic constraint
        void Add(T entity);
        void Update(T entity);
        void Delete(object id);

        // Проблема: нарушает принцип единой ответственности
        string GenerateReport();
        void ValidateEntity(T entity);
    }

    // Проблема: базовый класс не абстрактный, содержит конкретную логику
    public class BaseRepository<T> : IRepository<T> where T : class {
        protected List<T> _entities = new List<T>();

        // Проблема: возвращает изменяемую коллекцию
        public virtual List<T> GetAll() {
            return _entities;
        }

        // Проблема: использует рефлексию неэффективно для каждого вызова
        public virtual T GetById(object id) {
            foreach (var entity in _entities) {
                var idProperty = entity.GetType().GetProperty("Id");
                if (idProperty != null) {
                    var entityId = idProperty.GetValue(entity);
                    if (entityId?.ToString() == id?.ToString()) // Проблема: сравнение через строки
                    {
                        return entity;
                    }
                }
            }
            return null;
        }

        public virtual void Add(T entity) {
            _entities.Add(entity);
        }

        public virtual void Update(T entity) {
            // Проблема: поиск O(n) для обновления
            var existingEntity = GetById(GetEntityId(entity));
            if (existingEntity != null) {
                var index = _entities.IndexOf(existingEntity);
                _entities[index] = entity;
            }
        }

        public virtual void Delete(object id) {
            var entity = GetById(id);
            if (entity != null) {
                _entities.Remove(entity);
            }
        }

        // Проблема: рефлексия в каждом вызове
        private object GetEntityId(T entity) {
            var idProperty = entity.GetType().GetProperty("Id");
            return idProperty?.GetValue(entity);
        }

        // Проблема: бизнес-логика в репозитории
        public virtual string GenerateReport() {
            string report = $"Total {typeof(T).Name} entities: {_entities.Count}\n";
            return report;
        }

        // Проблема: валидация в репозитории
        public virtual void ValidateEntity(T entity) {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
        }
    }

    // Проблема: неправильное наследование, переопределение не виртуальных методов
    public class UserRepository : BaseRepository<User> {
        // Проблема: скрывает метод базового класса
        public new List<User> GetAll() {
            // Проблема: создается новый список каждый раз
            return _entities.Where(u => u.IsActive).ToList();
        }

        // Проблема: дублирование кода с базовым классом
        public override User GetById(object id) {
            if (id is int userId) {
                return _entities.FirstOrDefault(u => u.Id == userId);
            }
            return null;
        }

        // Проблема: добавляет бизнес-логику в репозиторий
        public override void Add(User entity) {
            ValidateUser(entity); // Дублируется с ValidateEntity
            entity.CreatedAt = DateTime.Now;
            base.Add(entity);
        }

        // Проблема: дублирование валидации
        private void ValidateUser(User user) {
            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("Email is required");
        }

        // Проблема: метод нарушает принцип единой ответственности
        public List<User> GetActiveUsersWithOrders() {
            return _entities.Where(u => u.IsActive && u.Orders.Any()).ToList();
        }
    }

    // Проблема: generic constraint слишком ограничивающий
    public class OrderRepository<T> : BaseRepository<T> where T : class, IOrder {
        // Проблема: ковариантность не поддерживается
        public IEnumerable<T> GetOrdersByStatus(OrderStatus status) {
            return _entities.Where(o => o.Status == status);
        }

        // Проблема: контравариантность не работает правильно
        public void ProcessOrders(Action<T> processor) {
            foreach (var order in _entities) {
                processor(order);
            }
        }

        // Проблема: нарушение Liskov Substitution Principle
        public override void Delete(object id) {
            var order = GetById(id);
            if (order != null && order.Status != OrderStatus.Completed) {
                base.Delete(id);
            } else {
                throw new InvalidOperationException("Cannot delete completed orders");
            }
        }
    }

    // Проблема: интерфейс не generic, ограничивает полиморфизм
    public interface IOrder {
        int Id { get; set; }
        OrderStatus Status { get; set; }
        decimal Amount { get; set; }
    }

    public enum OrderStatus {
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    // Проблема: класс не immutable, хотя представляет данные
    public class User {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Проблема: lazy initialization может вызвать проблемы с многопоточностью
        private List<Order> _orders;
        public List<Order> Orders {
            get {
                if (_orders == null)
                    _orders = new List<Order>();
                return _orders;
            }
        }

        // Проблема: reference equality вместо value equality
        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj);
        }
    }

    public class Order : IOrder {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Amount { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }

        // Проблема: не переопределен GetHashCode при переопределении Equals
        public override bool Equals(object obj) {
            if (obj is Order other)
                return Id == other.Id;
            return false;
        }
    }

    // Проблема: фабрика не generic, создает проблемы с типизацией
    public class RepositoryFactory {
        private static Dictionary<Type, object> _repositories = new Dictionary<Type, object>();

        // Проблема: возвращает object, теряется типизация
        public static object GetRepository(Type entityType) {
            if (!_repositories.ContainsKey(entityType)) {
                // Проблема: использование рефлексии для создания экземпляров
                var repositoryType = typeof(BaseRepository<>).MakeGenericType(entityType);
                var repository = Activator.CreateInstance(repositoryType);
                _repositories[entityType] = repository;
            }

            return _repositories[entityType];
        }

        // Проблема: метод не generic, требует cast
        public static T GetRepository<T>() where T : class {
            var repo = GetRepository(typeof(T));
            return repo as T; // Проблема: может вернуть null без предупреждения
        }
    }

    // Проблема: сервис смешивает разные уровни абстракции
    public class UserService {
        private UserRepository _userRepository;
        private OrderRepository<Order> _orderRepository;

        public UserService() {
            // Проблема: жесткая зависимость от конкретных типов
            _userRepository = new UserRepository();
            _orderRepository = new OrderRepository<Order>();
        }

        // Проблема: метод делает слишком много, нарушает SRP
        public async Task<string> ProcessUserWithOrdersAsync(int userId) {
            var user = _userRepository.GetById(userId);
            if (user == null)
                return "User not found";

            // Проблема: синхронные операции в async методе
            var userOrders = _orderRepository.GetOrdersByStatus(OrderStatus.Pending)
                                            .Where(o => o.UserId == userId)
                                            .ToList();

            // Проблема: строковая конкатенация в цикле
            string result = $"User: {user.Name}\nOrders:\n";
            foreach (var order in userOrders) {
                result += $"Order {order.Id}: {order.Amount:C}\n";
            }

            return result;
        }

        // Проблема: нет обработки исключений
        public void TransferOrdersBetweenUsers(int fromUserId, int toUserId) {
            var fromUser = _userRepository.GetById(fromUserId);
            var toUser = _userRepository.GetById(toUserId);

            // Проблема: изменение коллекции во время итерации
            foreach (var order in fromUser.Orders) {
                order.UserId = toUserId;
                toUser.Orders.Add(order);
                fromUser.Orders.Remove(order);
            }
        }
    }

    class Program {
        static async Task Main() {
            var userService = new UserService();

            // Проблема: создание объектов с нарушением инкапсуляции
            var user1 = new User {
                Id = 1,
                Name = "John",
                Email = "john@test.com",
                IsActive = true
            };

            var user2 = new User {
                Id = 2,
                Name = "Jane",
                Email = "jane@test.com",
                IsActive = true
            };

            // Проблема: прямое обращение к репозиториям из Main
            var userRepo = new UserRepository();
            userRepo.Add(user1);
            userRepo.Add(user2);

            var orderRepo = new OrderRepository<Order>();
            orderRepo.Add(new Order { Id = 1, UserId = 1, Amount = 100.00m, Status = OrderStatus.Pending });
            orderRepo.Add(new Order { Id = 2, UserId = 1, Amount = 200.00m, Status = OrderStatus.Pending });

            var result = await userService.ProcessUserWithOrdersAsync(1);
            Console.WriteLine(result);

            // Проблема: нет проверки на ошибки
            userService.TransferOrdersBetweenUsers(1, 2);

            Console.WriteLine("Orders transferred successfully");
        }
    }

    /*
    ЗАДАНИЯ ДЛЯ РЕФАКТОРИНГА:

    1. Исправьте проблемы с generic constraints и сделайте интерфейсы ковариантными/контравариантными где нужно
    2. Реализуйте правильную иерархию абстракций (abstract classes, interfaces)
    3. Устраните нарушения принципов SOLID (SRP, LSP, DIP)
    4. Исправьте проблемы с Equals/GetHashCode
    5. Оптимизируйте использование рефлексии
    6. Сделайте коллекции immutable где нужно
    7. Реализуйте правильный dependency injection
    8. Добавьте proper exception handling
    9. Исправьте проблемы с async/await
    10. Устраните дублирование кода
    11. Реализуйте правильные generic constraints
    12. Добавьте thread-safety где необходимо

    БОНУС: Реализуйте Unit of Work паттерн с правильными обобщениями
    */
}
