using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using EFCoreProfiling.Data;

namespace EFCoreProfiling.Helper {
    public class QueryProfiler : IObserver<KeyValuePair<string, object>> {
        private readonly List<QueryInfo> _queries = new List<QueryInfo>();

        public IReadOnlyList<QueryInfo> Queries => _queries;

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object> value) {
            if (value.Value is CommandExecutedEventData commandExecuted) {
                var query = new QueryInfo {
                    CommandText = commandExecuted.Command.CommandText,
                    Duration = commandExecuted.Duration,
                    ExecutionTime = DateTime.Now
                };

                _queries.Add(query);
            }
        }

        public static QueryProfiler CreateProfiler(DbContextOptionsBuilder optionsBuilder) {
            var profiler = new QueryProfiler();

            // Register the profiler as an observer for command executed events
            optionsBuilder
                .EnableSensitiveDataLogging()
                .LogTo(
                    action: (eventId) => {
                        //if (eventId.Id == RelationalEventId.CommandExecuted.Id) {
                            Console.WriteLine($"SQL Query executed at {DateTime.Now}");
                        //}
                    },
                    categories: new[] { DbLoggerCategory.Database.Command.Name });

            // Add our profiler as an interceptor to capture detailed information
            optionsBuilder.AddInterceptors(new ProfilerDbCommandInterceptor(profiler));

            return profiler;
        }

        // Execute and profile a query action
        public static void ProfileQuery(string description, DbContext context, Action queryAction) {
            Console.WriteLine($"\n--- Profiling: {description} ---");

            // Clear internal query statistics on the context
            context.Database.EnsureCreated();

            // Execute the query with timing
            var sw = Stopwatch.StartNew();
            queryAction();
            sw.Stop();

            Console.WriteLine($"Execution time: {sw.ElapsedMilliseconds}ms");

            // Retrieve the latest command executed
            if (context is BookStoreContext bookContext && bookContext.QueryLogger.Queries.Any()) {
                var recentQueries = bookContext.QueryLogger.Queries.TakeLast(10).ToList();

                Console.WriteLine($"SQL Queries executed: {recentQueries.Count}");

                foreach (var query in recentQueries) {
                    Console.WriteLine("\nSQL:");
                    Console.WriteLine(query.CommandText);
                    Console.WriteLine($"Duration: {query.Duration.TotalMilliseconds}ms");
                }
            }

            Console.WriteLine("------------------------------------");
        }
    }

    // Class to store query information
    public class QueryInfo {
        public string CommandText { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public DateTime ExecutionTime { get; set; }
    }

    // Interceptor to capture query execution details
    public class ProfilerDbCommandInterceptor : DbCommandInterceptor {
        private readonly QueryProfiler _profiler;

        public ProfilerDbCommandInterceptor(QueryProfiler profiler) {
            _profiler = profiler;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) {
            Console.WriteLine($"Executing SQL: {command.CommandText}");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result) {
            _profiler.OnNext(new KeyValuePair<string, object>(string.Empty, eventData));
            return base.ReaderExecuted(command, eventData, result);
        }
    }
}
