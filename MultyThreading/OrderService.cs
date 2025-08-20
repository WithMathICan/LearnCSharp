using Microsoft.EntityFrameworkCore;

namespace MultyThreading {
    public class Order {
        public int Id { get; set; }
        public string Description { get; set; } = "";
    }

    public class AppDbContext : DbContext {
        public DbSet<Order> Orders { get; set; }
        public DbSet<Stats> Stats { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

    public class OrderService(AppDbContext context) {
        private readonly AppDbContext _context = context;
        private int _totalOrdersProcessed = 0;

        public async Task AddOrderAsync(string description) {
            _context.Orders.Add(new Order { Description = description });
            await _context.SaveChangesAsync();
            _totalOrdersProcessed++;
        }

        public Task<int> GetTotalOrdersProcessedAsync() {
            return Task.FromResult(_totalOrdersProcessed);
        }
    }

    public class OrderService_Refactored_NotCorrect(AppDbContext context) {
        private readonly AppDbContext _context = context;
        private int _totalOrdersProcessed = 0;
        private SemaphoreSlim slim = new(1, 1);

        public async Task AddOrderAsync(string description) {
            await slim.WaitAsync();
            try {
                _context.Orders.Add(new Order { Description = description });
                await _context.SaveChangesAsync();
                _totalOrdersProcessed++;
            } finally { slim.Release(); }
        }

        public int GetTotalOrdersProcessedAsync() {
            return _totalOrdersProcessed;
        }
    }

    public class OrderService_Correct {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private int _totalOrdersProcessed = 0;

        public OrderService_Correct(IDbContextFactory<AppDbContext> contextFactory) {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task AddOrderAsync(string description) {
            using var context = _contextFactory.CreateDbContext();
            context.Orders.Add(new Order { Description = description });
            await context.SaveChangesAsync();
            Interlocked.Increment(ref _totalOrdersProcessed);
        }

        public Task<int> GetTotalOrdersProcessedAsync() {
            return Task.FromResult(_totalOrdersProcessed);
        }
    }
}
