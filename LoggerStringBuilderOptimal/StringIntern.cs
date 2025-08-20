using System;
using System.Collections.Generic;
using System.Diagnostics;

class Program {
    static void Main10() {
        // Тестовые данные - миллион записей каждого типа
        var logTypes = new[] { "INFO", "DEBUG", "ERROR", "WARN" };
        var random = new Random();

        Console.WriteLine("=== Сравнение использования String.Intern ===\n");

        // Тест без String.Intern
        TestWithoutIntern(logTypes, random, 1_000_000);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Тест со String.Intern
        TestWithIntern(logTypes, random, 1_000_000);

        // Демонстрация того, что интернированные строки ссылаются на один объект
        DemonstrateStringInterning();
    }

    static void TestWithoutIntern(string[] logTypes, Random random, int count) {
        Console.WriteLine("--- БЕЗ String.Intern ---");

        var stopwatch = Stopwatch.StartNew();
        long memoryBefore = GC.GetTotalMemory(true);

        var logEntries = new List<LogEntry>(count);

        for (int i = 0; i < count; i++) {
            // Создаем новую строку каждый раз (симулируем получение из внешнего источника)
            string logType = new string(logTypes[random.Next(logTypes.Length)].ToCharArray());

            logEntries.Add(new LogEntry {
                Type = logType, // Каждая строка - отдельный объект в памяти
                Message = $"Log message {i}"
            });
        }

        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(false);

        Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
        Console.WriteLine($"Потреблено памяти: {(memoryAfter - memoryBefore) / 1024 / 1024} МБ");
        Console.WriteLine($"Количество записей: {logEntries.Count}\n");
    }

    static void TestWithIntern(string[] logTypes, Random random, int count) {
        Console.WriteLine("--- СО String.Intern ---");

        var stopwatch = Stopwatch.StartNew();
        long memoryBefore = GC.GetTotalMemory(true);

        var logEntries = new List<LogEntry>(count);

        for (int i = 0; i < count; i++) {
            // Создаем новую строку каждый раз (симулируем получение из внешнего источника)
            string logType = new string(logTypes[random.Next(logTypes.Length)].ToCharArray());

            logEntries.Add(new LogEntry {
                Type = string.Intern(logType), // Интернируем строку - ссылается на один объект
                Message = $"Log message {i}"
            });
        }

        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(false);

        Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
        Console.WriteLine($"Потреблено памяти: {(memoryAfter - memoryBefore) / 1024 / 1024} МБ");
        Console.WriteLine($"Количество записей: {logEntries.Count}\n");
    }

    static void DemonstrateStringInterning() {
        Console.WriteLine("--- Демонстрация интернирования ---");

        // Создаем "новые" строки
        string str1 = new string("INFO".ToCharArray());
        string str2 = new string("INFO".ToCharArray());
        string str3 = "INFO"; // Литеральные строки автоматически интернируются

        Console.WriteLine($"str1 == str2: {str1 == str2}"); // True (равенство содержимого)
        Console.WriteLine($"ReferenceEquals(str1, str2): {ReferenceEquals(str1, str2)}"); // False (разные объекты)
        Console.WriteLine($"ReferenceEquals(str1, str3): {ReferenceEquals(str1, str3)}"); // False

        // Интернируем строки
        string internStr1 = string.Intern(str1);
        string internStr2 = string.Intern(str2);

        Console.WriteLine($"После интернирования:");
        Console.WriteLine($"ReferenceEquals(internStr1, internStr2): {ReferenceEquals(internStr1, internStr2)}"); // True
        Console.WriteLine($"ReferenceEquals(internStr1, str3): {ReferenceEquals(internStr1, str3)}"); // True
        Console.WriteLine($"ReferenceEquals(internStr1, \"INFO\"): {ReferenceEquals(internStr1, "INFO")}"); // True
    }
}

class LogEntry {
    public string Type { get; set; }
    public string Message { get; set; }
}

// Альтернативный подход с предварительным интернированием
class OptimizedLogger {
    // Предварительно интернированные строки
    private static readonly string INFO = string.Intern("INFO");
    private static readonly string DEBUG = string.Intern("DEBUG");
    private static readonly string ERROR = string.Intern("ERROR");
    private static readonly string WARN = string.Intern("WARN");

    public static string GetInternedLogType(string logType) {
        // Более эффективный подход - сравниваем и возвращаем уже интернированную строку
        return logType switch {
            "INFO" => INFO,
            "DEBUG" => DEBUG,
            "ERROR" => ERROR,
            "WARN" => WARN,
            _ => string.Intern(logType) // Для неожиданных типов
        };
    }
}