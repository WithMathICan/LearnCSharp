using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace T {
    // Сценарий: парсим миллионы строк из лог-файла или получаем от API
    class MassLoggingExample {
        static void Main() {
            Console.WriteLine("=== Массовое создание логов с интернированием ===\n");

            // Генерируем тестовые данные (симулируем внешний источник)
            GenerateTestLogFile("test_logs.txt", 1_000_000);

            // Тест без интернирования
            TestWithoutIntern();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Тест с интернированием
            TestWithIntern();

            // Тест с предварительным интернированием
            TestWithPreIntern();
        }

        static void GenerateTestLogFile(string fileName, int count) {
            var logTypes = new[] { "INFO", "DEBUG", "ERROR", "WARN" };
            var random = new Random();

            using var writer = new StreamWriter(fileName);
            for (int i = 0; i < count; i++) {
                var logType = logTypes[random.Next(logTypes.Length)];
                writer.WriteLine($"{logType}|2024-01-15 10:30:45|Message {i}");
            }
        }

        static void TestWithoutIntern() {
            Console.WriteLine("--- БЕЗ интернирования ---");

            var stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);

            var logEntries = new List<LogEntry>();

            // Читаем файл построчно (симулируем получение данных извне)
            foreach (string line in File.ReadLines("test_logs.txt")) {
                var parts = line.Split('|');
                if (parts.Length >= 3) {
                    // Каждый раз создаем новый объект строки
                    string logType = new string(parts[0].ToCharArray()); // Симуляция внешнего источника

                    logEntries.Add(new LogEntry {
                        Category = logType, // НЕ интернированная строка
                        Timestamp = DateTime.Parse(parts[1]),
                        Message = parts[2]
                    });
                }
            }

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"Время: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine($"Память: {(memoryAfter - memoryBefore) / 1024 / 1024} МБ");
            Console.WriteLine($"Записей: {logEntries.Count}");

            // Проверяем, что у нас разные объекты для одинаковых категорий
            var infoLogs = logEntries.FindAll(x => x.Category == "INFO");
            if (infoLogs.Count >= 2) {
                Console.WriteLine($"Разные объекты INFO: {!ReferenceEquals(infoLogs[0].Category, infoLogs[1].Category)}");
            }
            Console.WriteLine();
        }

        static void TestWithIntern() {
            Console.WriteLine("--- С интернированием на лету ---");

            var stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);

            var logEntries = new List<LogEntry>();

            foreach (string line in File.ReadLines("test_logs.txt")) {
                var parts = line.Split('|');
                if (parts.Length >= 3) {
                    // Создаем новый объект строки (симуляция внешнего источника)
                    string logType = new string(parts[0].ToCharArray());

                    logEntries.Add(new LogEntry {
                        Category = string.Intern(logType), // Интернируем СРАЗУ при создании
                        Timestamp = DateTime.Parse(parts[1]),
                        Message = parts[2]
                    });
                }
            }

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"Время: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine($"Память: {(memoryAfter - memoryBefore) / 1024 / 1024} МБ");
            Console.WriteLine($"Записей: {logEntries.Count}");

            // Проверяем, что теперь у нас один объект для одинаковых категорий
            var infoLogs = logEntries.FindAll(x => x.Category == "INFO");
            if (infoLogs.Count >= 2) {
                Console.WriteLine($"Один объект INFO: {ReferenceEquals(infoLogs[0].Category, infoLogs[1].Category)}");
            }
            Console.WriteLine();
        }

        static void TestWithPreIntern() {
            Console.WriteLine("--- С предварительным интернированием (ОПТИМАЛЬНО) ---");

            var categoryFactory = new LogCategoryFactory();

            var stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);

            var logEntries = new List<LogEntry>();

            foreach (string line in File.ReadLines("test_logs.txt")) {
                var parts = line.Split('|');
                if (parts.Length >= 3) {
                    // Создаем новый объект строки (симуляция внешнего источника)
                    string logType = new string(parts[0].ToCharArray());

                    logEntries.Add(new LogEntry {
                        Category = categoryFactory.GetInternedCategory(logType), // Быстрое получение интернированной строки
                        Timestamp = DateTime.Parse(parts[1]),
                        Message = parts[2]
                    });
                }
            }

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(false);

            Console.WriteLine($"Время: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine($"Память: {(memoryAfter - memoryBefore) / 1024 / 1024} МБ");
            Console.WriteLine($"Записей: {logEntries.Count}");
            Console.WriteLine();
        }
    }

    // Простая структура лог-записи
    class LogEntry {
        public string Category { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }

    // Фабрика для эффективного получения интернированных категорий
    class LogCategoryFactory {
        // Предварительно интернированные строки
        private static readonly string INFO = string.Intern("INFO");
        private static readonly string DEBUG = string.Intern("DEBUG");
        private static readonly string ERROR = string.Intern("ERROR");
        private static readonly string WARN = string.Intern("WARN");
        private static readonly string UNKNOWN = string.Intern("UNKNOWN");

        // Словарь для быстрого поиска (O(1) вместо O(n))
        private static readonly Dictionary<string, string> CategoryMap = new() {
            ["INFO"] = INFO,
            ["DEBUG"] = DEBUG,
            ["ERROR"] = ERROR,
            ["WARN"] = WARN
        };

        public string GetInternedCategory(string category) {
            // Быстрый поиск по словарю
            return CategoryMap.TryGetValue(category, out var internedCategory)
                ? internedCategory
                : string.Intern(category ?? UNKNOWN);
        }
    }

    // Пример реального сценария: парсинг JSON логов
    class JsonLogProcessor {
        private readonly LogCategoryFactory _categoryFactory = new();

        public List<LogEntry> ProcessJsonLogs(string jsonFilePath) {
            var logEntries = new List<LogEntry>();

            string jsonContent = File.ReadAllText(jsonFilePath);
            var jsonLogs = JsonSerializer.Deserialize<JsonLogEntry[]>(jsonContent);

            foreach (var jsonLog in jsonLogs) {
                logEntries.Add(new LogEntry {
                    // Интернируем СРАЗУ при преобразовании из JSON
                    Category = _categoryFactory.GetInternedCategory(jsonLog.Level),
                    Timestamp = jsonLog.Timestamp,
                    Message = jsonLog.Message
                });
            }

            return logEntries;
        }
    }

    class JsonLogEntry {
        public string Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }

    // Пример использования с потоками данных
    class StreamLogProcessor {
        private readonly LogCategoryFactory _categoryFactory = new();

        public async IAsyncEnumerable<LogEntry> ProcessLogStreamAsync(Stream logStream) {
            using var reader = new StreamReader(logStream);
            string line;

            while ((line = await reader.ReadLineAsync()) != null) {
                var parts = line.Split('|');
                if (parts.Length >= 3) {
                    yield return new LogEntry {
                        // Интернируем каждую строку ПО МЕРЕ ПОСТУПЛЕНИЯ
                        Category = _categoryFactory.GetInternedCategory(parts[0]),
                        Timestamp = DateTime.Parse(parts[1]),
                        Message = parts[2]
                    };
                }
            }
        }
    }
}