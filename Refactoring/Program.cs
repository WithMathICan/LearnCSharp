using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Refactoring;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using System.Text;
using System.Transactions;

class Program {
    static void Main0() {
        //BenchmarkRunner.Run<Benchmark>();
        BenchmarkRunner.Run<EventLoggerBenchmark>();
        //CompareTwoLoggers();
        //var processor = new DataProcessor001();
        //for (int i = 0; i < 10_000_000; i++) {
        //    processor.AddMeasurement(new Measurement(i, DateTime.Now));
        //}

        //Stopwatch sw = Stopwatch.StartNew();
        //double avg = processor.CalculateAverage();
        //Console.WriteLine($"Average: {avg}, Time: {sw.ElapsedMilliseconds} ms");
    }

    static void CompareTwoLoggers() {
        string[] _categories = Enumerable.Repeat("INFO", 10_000).ToArray();
        string[] _messages = Enumerable.Repeat("Test event", 10_000).ToArray();
        EventLogger _original = new EventLogger();
        var logger = new ApplicationLogger();
        EventLoggerOptimizedMutex _optimized = new EventLoggerOptimizedMutex(logger);
        MeasureAverageExecutionTime("Original", () => _original.LogEvents(_categories, _messages), 5);
        MeasureAverageExecutionTime("Optimized", () => _optimized.LogEvents(_categories, _messages), 5);
    }

    public static void MeasureAverageExecutionTime(string methodName, Action action, int iterations) {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i < iterations; i++) {
            action.Invoke();
        }
        stopwatch.Stop();
        Console.WriteLine($"Среднее время выполнения [{methodName}] за {iterations} итераций: {(double)stopwatch.ElapsedMilliseconds / iterations:F4} мс");
    }
}

[MemoryDiagnoser]
public class EventLoggerBenchmark {
    private readonly string[] _categories = Enumerable.Repeat("INFO", 10_000).ToArray();
    private readonly string[] _messages = Enumerable.Repeat("Test event", 10_000).ToArray();
    private readonly EventLogger _original = new EventLogger();
    private readonly EventLoggerOptimizedMutex _optimized;

    public EventLoggerBenchmark() {
        var logger = new ApplicationLogger();
        _optimized = new EventLoggerOptimizedMutex(logger);
    }

    [Benchmark(Baseline = true)]
    public void Original() {
        _original.LogEvents(_categories, _messages);
    }

    [Benchmark]
    public void Optimized() {
        _optimized.LogEvents(_categories, _messages);
    }
}

[MemoryDiagnoser]
public class Benchmark {
    private readonly DataProcessor _arrayListProcessor = new DataProcessor();
    private readonly DataProcessor001 _listProcessor = new DataProcessor001();

    [GlobalSetup]
    public void Setup() {
        for (int i = 0; i < 10_000_000; i++) {
            var measurement = new Measurement(i, DateTime.Now);
            _arrayListProcessor.AddMeasurement(measurement);
            _listProcessor.AddMeasurement(measurement);
        }
    }

    [Benchmark(Baseline = true)]
    public double ArrayList_Average() {
        return _arrayListProcessor.CalculateAverage();
    }

    [Benchmark]
    public double List_Average() {
        return _listProcessor.CalculateAverage();
    }
}


public struct Measurement {
    public double Value;
    public DateTime Timestamp;

    public Measurement(double value, DateTime timestamp) {
        Value = value;
        Timestamp = timestamp;
    }
}

class DataProcessor {
    private ArrayList measurements = new ArrayList();

    public void AddMeasurement(Measurement m) {
        measurements.Add(m);  // boxing
    }

    public double CalculateAverage() {
        double sum = 0;
        foreach (object item in measurements) 
        {
            Measurement m = (Measurement)item; // Unboxing
            sum += m.Value;
        }
        return sum / measurements.Count;
    }
}

class DataProcessor001 {
    private List<Measurement> measurements = new List<Measurement>();

    public void AddMeasurement(Measurement m) {
        measurements.Add(m);
    }

    public double CalculateAverage() {
        double sum = 0;
        if (measurements.Count == 0) return 0;
        //foreach (var item in measurements) {
        //    sum += item.Value;
        //}
        //Span<Measurement> span = CollectionsMarshal.AsSpan(measurements);
        for (int i = 0; i < measurements.Count; ++i) sum += measurements[i].Value;
        //for (int i = 0; i < span.Length; ++i) sum += measurements[i].Value;
        return sum / measurements.Count;
    }
}

class DataProcessor002 {
    private List<double> measurementsValue = new List<double>();
    private List<DateTime> measurementsDate = new List<DateTime>();

    public void AddMeasurement(Measurement m) {
        measurementsValue.Add(m.Value);
        measurementsDate.Add(m.Timestamp);
    }

    public double CalculateAverage() {
        return measurementsValue.Average();
    }
}






public class LogFormatter {
    public string FormatLogEntry(string level, string message, DateTime timestamp) {
        return level + ": " + timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message;
    }

    public void ProcessLogs(string[] messages) {
        string result = "";
        for (int i = 0; i < messages.Length; i++) {
            result += FormatLogEntry("INFO", messages[i], DateTime.Now);
            if (i < messages.Length - 1) result += "\n";
        }
        Console.WriteLine(result);
    }
}

public class LogFormatter001 {
    public static void FormatLogEntry(string level, string message, DateTime timestamp, StringBuilder stringBuilder) {
        stringBuilder.Append(level);
        stringBuilder.Append(": ");
        stringBuilder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        stringBuilder.Append(" - ");
        stringBuilder.Append(message);
    }

    public void ProcessLogs(string[] messages) {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < messages.Length; i++) {
            FormatLogEntry("INFO", messages[i], DateTime.Now, stringBuilder);
            if (i < messages.Length - 1) stringBuilder.Append(Environment.NewLine);
        }
        Console.WriteLine(stringBuilder.ToString());
    }
}

public class LogFormatterOptimized {
    private const string LogLevel = "INFO"; // Возможный кандидат для интернирования
    private static readonly object _consoleLock = new object();

    public static void FormatLogEntry(string level, string message, DateTime timestamp, StringBuilder builder) {
        // Оптимизация: используем Span<char> для форматирования DateTime
        Span<char> dateBuffer = stackalloc char[19]; // yyyy-MM-dd HH:mm:ss
        if (!timestamp.TryFormat(dateBuffer, out _, "yyyy-MM-dd HH:mm:ss")) {
            throw new FormatException("Failed to format timestamp");
        }

        builder.Append(level)
               .Append(": ")
               .Append(dateBuffer)
               .Append(" - ")
               .Append(message);
    }

    public void ProcessLogs(string[] messages) {
        // Используем пул StringBuilder для минимизации аллокаций
        StringBuilder builder = StringBuilderPool.Get();
        try {
            for (int i = 0; i < messages.Length; i++) {
                FormatLogEntry(LogLevel, messages[i], DateTime.Now, builder);
                if (i < messages.Length - 1) {
                    builder.Append(Environment.NewLine);
                }
            }

            // Потокобезопасный вывод
            lock (_consoleLock) {
                Console.WriteLine(builder.ToString());
            }
        } finally {
            StringBuilderPool.Return(builder);
        }
    }
}
public static class StringBuilderPool001 {
    private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>();

    public static StringBuilder Get() {
        lock (_pool) {
            if (_pool.Count > 0) {
                var sb = _pool.Pop();
                sb.Clear();
                return sb;
            }
        }
        return new StringBuilder();
    }

    public static void Return(StringBuilder sb) {
        if (sb == null) return;
        lock (_pool) {
            _pool.Push(sb);
        }
    }
}

public static class StringBuilderPool {
    private static readonly ConcurrentStack<StringBuilder> _pool = new ConcurrentStack<StringBuilder>();

    public static StringBuilder Get() {
        if (_pool.TryPop(out var sb)) {
            sb.Clear();
            return sb;
        }
        return new StringBuilder();
    }

    public static void Return(StringBuilder sb) {
        if (sb == null) return;
        _pool.Push(sb);
    }
}


public struct LargeStruct {
    public double Data1, Data2, Data3, Data4, Data5; // 40 байт
}

public class DataProcessor1 {
    public void UpdateStruct(ref LargeStruct data) {
        data.Data1 += 1;
    }

    public void ProcessData(LargeStruct[] items) {
        //for (int i = 0; i < items.Length; ++i) {
        //    UpdateStruct(ref items[i]); 
        //}
        Parallel.For(0, items.Length, i => UpdateStruct(ref items[i])); // Boxing
    }
}

public class LogProcessor {
    private string _prefix = "INFO";

    public string FormatLog(string message, DateTime timestamp) {
        return $"{_prefix}: {timestamp:yyyy-MM-dd HH:mm:ss} - {message}";
    }

    public void ProcessLogs(string[] messages) {
        string result = string.Empty;
        foreach (var message in messages) {
            result += FormatLog(message, DateTime.Now) + Environment.NewLine;
        }
        File.WriteAllText("log.txt", result);
    }
}

public class LogProcessor0001 {
    private readonly static string _prefix = "INFO";

    public string FormatLog(string message, DateTime timestamp) {
        return $"{_prefix}: {timestamp:yyyy-MM-dd HH:mm:ss} - {message}";
    }

    public void ProcessLogs(string[] messages) {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var message in messages) {
            stringBuilder.AppendLine(FormatLog(message, DateTime.Now));
        }
        lock (this) {
            File.WriteAllText("log.txt", stringBuilder.ToString());
        }
    }
}

public class LogProcessor0002 {
    private readonly static string _prefix = "INFO";

    public string FormatLog(string message, DateTime timestamp) {
        return $"{_prefix}: {timestamp:yyyy-MM-dd HH:mm:ss} - {message}";
    }

    public void ProcessLogs(string[] messages) {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var message in messages) {
            stringBuilder.AppendLine(FormatLog(message, DateTime.Now));
        }
        lock (this) {
            File.WriteAllText("log.txt", stringBuilder.ToString());
        }
    }
}



public struct Transaction {
    public Guid Id;
    public decimal Amount;
    public DateTime Timestamp;
    public long AccountId;
    public bool IsProcessed;
}

public class TransactionProcessor {
    public bool TryProcessTransaction(ref Transaction transaction, out decimal newBalance) {
        newBalance = 0;
        if (transaction.Amount <= 0) {
            return false;
        }
        newBalance = transaction.Amount; // Упрощённая логика
        transaction.IsProcessed = true; // Ловушка здесь
        return true;
    }

    public void ProcessBatch(Span<Transaction> transactions) {
        decimal totalBalance = 0;
        for (int i = 0; i < transactions.Length; i++) {
            if (TryProcessTransaction(ref transactions[i], out decimal balance)) {
                totalBalance += balance;
            }
        }
        Console.WriteLine($"Total balance: {totalBalance}");
    }
}



public class EventLogger001 {
    Mutex mutex = new Mutex(false, "ApplicationLog");
    public void FormatEvent(string category, string message, DateTime timestamp, StringBuilder sb) {

        Span<char> dateBuffer = stackalloc char[19]; // yyyy-MM-dd HH:mm:ss
        if (!timestamp.TryFormat(dateBuffer, out _, "yyyy-MM-dd HH:mm:ss")) {
            throw new FormatException("Failed to format timestamp");
        }
        sb.Append(category)
          .Append(": ")
          .Append(dateBuffer)
          .Append(" - ")
          .Append(message);
    }

    public void LogEvents(string[] categories, string[] messages) {
        StringBuilder sb = new();
        for (int i = 0; i < categories.Length; i++) {
            FormatEvent(categories[i], messages[i], DateTime.Now, sb);
            sb.Append(Environment.NewLine);
        }
        bool acquired = mutex.WaitOne();
        try {
            File.WriteAllText("log.txt", sb.ToString());
        } finally {
            if (acquired) {
                mutex.ReleaseMutex();
            }
        }
    }

    //public void LogEventsStream(string[] categories, string[] messages) {
    //    StringBuilder sb = new();
    //    for (int i = 0; i < categories.Length; i++) {
    //        FormatEvent(categories[i], messages[i], DateTime.Now, sb);
    //        sb.Append(Environment.NewLine);
    //    }
    //    bool acquired = mutex.WaitOne();
    //    using var stream = new FileStream("log.txt", FileMode.Append);
    //    try {
    //        stream.WriteByte(sb);
    //    } finally {
    //        if (acquired) {
    //            mutex.ReleaseMutex();
    //        }
    //    }
    //}
}



public class EventLoggerOptimized {
    private static readonly string[] InternedCategories = { String.Intern("INFO"), String.Intern("ERROR"), String.Intern("DEBUG") };
    private static readonly Mutex _fileMutex = new Mutex(false, "ApplicationLog");

    public void FormatEvent(string category, string message, DateTime timestamp, StringBuilder builder) {
        Span<char> dateBuffer = stackalloc char[19]; // yyyy-MM-dd HH:mm:ss
        if (!timestamp.TryFormat(dateBuffer, out _, "yyyy-MM-dd HH:mm:ss")) {
            throw new FormatException("Failed to format timestamp");
        }

        builder.Append(category)
               .Append(": ")
               .Append(dateBuffer)
               .Append(" - ")
               .Append(message);
    }

    public void LogEvents(string[] categories, string[] messages) {
        if (categories == null || messages == null || categories.Length != messages.Length) {
            return; // Обработка edge-кейсов
        }

        // Оцениваем начальную ёмкость StringBuilder
        int estimatedLength = categories.Length * (10 + 19 + 3 + 50 + Environment.NewLine.Length);
        StringBuilder builder = new();

        for (int i = 0; i < categories.Length; i++) {
            // Используем интернированную категорию, если она из известного набора
            string category = Array.IndexOf(InternedCategories, categories[i]) >= 0
                ? InternedCategories[Array.IndexOf(InternedCategories, categories[i])]
                : categories[i];

            FormatEvent(category, messages[i], DateTime.Now, builder);
            if (i < categories.Length - 1) {
                builder.Append(Environment.NewLine);
            }
        }

        bool acquired = _fileMutex.WaitOne();
        try {
            File.AppendAllText("events.log", builder.ToString());
        } finally {
            if (acquired) {
                _fileMutex.ReleaseMutex();
            }
        }

    }

    public void LogEventsStream(string[] categories, string[] messages) {
        if (categories == null || messages == null || categories.Length != messages.Length) {
            return; // Обработка edge-кейсов
        }

        int estimatedLength = categories.Length * (10 + 19 + 3 + 50 + Environment.NewLine.Length);
        StringBuilder builder = new();
        try {
            for (int i = 0; i < categories.Length; i++) {
                string category = Array.IndexOf(InternedCategories, categories[i]) >= 0
                    ? InternedCategories[Array.IndexOf(InternedCategories, categories[i])]
                    : categories[i];

                FormatEvent(category, messages[i], DateTime.Now, builder);
                if (i < categories.Length - 1) {
                    builder.Append(Environment.NewLine);
                }
            }

            bool acquired = _fileMutex.WaitOne();
            try {
                using var stream = new FileStream("events.log", FileMode.Append, FileAccess.Write, FileShare.Read);
                // Получаем содержимое StringBuilder как ReadOnlySpan<char>
                Span<char> buffer = new char[builder.Length];
                builder.CopyTo(0, buffer, builder.Length);

                // Преобразуем в байты и записываем в поток
                Span<byte> byteBuffer = new byte[Encoding.UTF8.GetByteCount(buffer)];
                Encoding.UTF8.GetBytes(buffer, byteBuffer);
                stream.Write(byteBuffer);

                var streamWriter = new StreamWriter(stream);
                streamWriter.WriteAsync(builder);
            } finally {
                if (acquired) {
                    _fileMutex.ReleaseMutex();
                }
            }
        } finally {
            StringBuilderPool.Return(builder);
        }
    }
}










