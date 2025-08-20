using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultyThreading {
    /*Этот код используется в многопоточной среде, где несколько потоков вызывают AddOrderAsync, 
     * а один поток периодически вызывает GetTotalOrdersAsync.
    Какие проблемы могут возникнуть при использовании этого кода в многопоточной среде? 
    Учти транзакции и работу с базой данных.
    Отрефактори этот код, чтобы он стал надёжным и потокобезопасным. 
    Используй подходящие механизмы синхронизации (в коде или на уровне базы данных) и объясни свои решения.
    Ловушка: Может ли этот код привести к deadlock’у на уровне базы данных? Если да, как его избежать? Если нет, почему?*/

    public class Stats {
        public int Id { get; set; }
        public int TotalOrders { get; set; }
    }

    public class OrderAndStatsService {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public OrderAndStatsService(IDbContextFactory<AppDbContext> contextFactory) {
            _contextFactory = contextFactory;
        }

        public async Task AddOrderAsync(string description) {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            context.Orders.Add(new Order { Description = description });
            var stats = await context.Stats.FirstOrDefaultAsync();
            if (stats == null) {
                stats = new Stats { TotalOrders = 0 };
                context.Stats.Add(stats);
            }
            stats.TotalOrders++;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<int> GetTotalOrdersAsync() {
            using var context = _contextFactory.CreateDbContext();
            var stats = await context.Stats.FirstOrDefaultAsync();
            return stats?.TotalOrders ?? 0;
        }
    }
}
