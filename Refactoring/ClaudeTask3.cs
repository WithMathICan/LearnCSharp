using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refactoringtask3 {
    // ЗАДАЧА 3: Система обработки данных с проблемами производительности
    // Проблемы: неэффективный LINQ, неправильный выбор коллекций, 
    // проблемы с IDisposable, неоптимальная работа с памятью

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    // Проблема: не реализует IDisposable, хотя работает с файлами
    public class DataProcessor {
        private List<string> _processedFiles = new List<string>();
        private Dictionary<string, List<DataRecord>> _dataCache = new Dictionary<string, List<DataRecord>>();
        private FileStream _logFile;

        public DataProcessor(string logPath) {
            // Проблема: не используется using или try-finally
            _logFile = new FileStream(logPath, FileMode.Append);
        }

        // Проблема: multiple enumeration, неэффективные LINQ операции
        public async Task<ProcessingResult> ProcessDataAsync(IEnumerable<DataRecord> records) {
            var validRecords = records.Where(r => r.IsValid);

            // Проблема: multiple enumeration одной и той же последовательности
            var count = validRecords.Count();
            var totalAmount = validRecords.Sum(r => r.Amount);
            var categories = validRecords.Select(r => r.Category).Distinct().ToList();

            // Проблема: неэффективная группировка с множественными ToList()
            var groupedByCategory = validRecords.GroupBy(r => r.Category)
                                               .ToDictionary(g => g.Key, g => g.ToList());

            var result = new ProcessingResult();

            // Проблема: выполняется в основном потоке, хотя могло быть async
            foreach (var categoryGroup in groupedByCategory) {
                var categoryData = ProcessCategory(categoryGroup.Value);
                result.CategoryResults.Add(categoryGroup.Key, categoryData);
            }

            // Проблема: неэффективный поиск максимума
            var maxAmount = validRecords.OrderByDescending(r => r.Amount).First().Amount;
            result.MaxAmount = maxAmount;

            // Проблема: создание новой коллекции для простой операции
            var expensiveItems = validRecords.Where(r => r.Amount > maxAmount * 0.8M).ToList();
            result.ExpensiveItemsCount = expensiveItems.Count();

            return result;
        }

        // Проблема: использует неподходящую коллекцию для поиска
        private CategoryData ProcessCategory(List<DataRecord> records) {
            var categoryData = new CategoryData();

            // Проблема: List используется для частых поисков
            var processedIds = new List<string>();

            foreach (var record in records) {
                // Проблема: O(n) поиск в списке
                if (!processedIds.Contains(record.Id)) {
                    processedIds.Add(record.Id);

                    // Проблема: строковая конкатенация в цикле
                    string logEntry = "Processing: " + record.Id + " - " + record.Category +
                                     " - " + record.Amount.ToString();

                    LogToFile(logEntry);
                    categoryData.ProcessedCount++;
                }
            }

            // Проблема: неэффективная агрегация
            categoryData.TotalAmount = records.Select(r => r.Amount).Sum();
            categoryData.AverageAmount = records.Select(r => r.Amount).Average();

            return categoryData;
        }

        // Проблема: синхронная операция с файлом в async методе
        private void LogToFile(string message) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(message + Environment.NewLine);
            _logFile.Write(bytes, 0, bytes.Length);
            _logFile.Flush(); // Проблема: flush после каждой записи
        }

        // Проблема: возвращает IEnumerable, но материализует внутри
        public IEnumerable<DataRecord> FilterAndSort(IEnumerable<DataRecord> records,
                                                      string category,
                                                      decimal minAmount) {
            // Проблема: материализация в ToList(), хотя можно оставить lazy
            var filtered = records.Where(r => r.Category == category && r.Amount >= minAmount)
                                  .OrderBy(r => r.Amount)
                                  .ToList();

            // Проблема: кэширование уже материализованных данных
            _dataCache[category] = filtered;

            return filtered;
        }

        // Проблема: использует LINQ там, где простой цикл был бы эффективнее
        public Dictionary<string, int> GetCategoryCounts(IEnumerable<DataRecord> records) {
            return records.GroupBy(r => r.Category)
                         .ToDictionary(g => g.Key,
                                     g => g.Count()); // Проблема: Count() на уже сгруппированных данных
        }

        // Проблема: неэффективный поиск дубликатов
        public List<DataRecord> FindDuplicates(IEnumerable<DataRecord> records) {
            var duplicates = new List<DataRecord>();

            foreach (var record in records) {
                // Проблема: квадратичная сложность O(n²)
                var otherRecords = records.Where(r => r != record);
                if (otherRecords.Any(r => r.Id == record.Id)) {
                    duplicates.Add(record);
                }
            }

            return duplicates.Distinct().ToList(); // Проблема: Distinct() без IEquatable
        }

        // Проблема: нет освобождения ресурсов
        public void Close() {
            _logFile?.Close();
        }
    }

    // Проблема: не реализует IEquatable, но используется в Distinct()
    public class DataRecord {
        public string Id { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(Id) && Amount > 0;

        // Проблема: переопределен только Equals
        public override bool Equals(object obj) {
            return obj is DataRecord record && Id == record.Id;
        }
    }

    // Проблема: изменяемые коллекции в публичных свойствах
    public class ProcessingResult {
        public Dictionary<string, CategoryData> CategoryResults { get; set; } =
            new Dictionary<string, CategoryData>();
        public decimal MaxAmount { get; set; }
        public int ExpensiveItemsCount { get; set; }
    }

    public class CategoryData {
        public int ProcessedCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    // Проблема: неэффективный генератор тестовых данных
    public static class TestDataGenerator {
        public static List<DataRecord> GenerateRecords(int count) {
            var records = new List<DataRecord>();
            var random = new Random();
            var categories = new[] { "A", "B", "C", "D", "E" };

            // Проблема: создание строк в цикле через конкатенацию
            for (int i = 0; i < count; i++) {
                records.Add(new DataRecord {
                    Id = "ID_" + i.ToString(),
                    Category = categories[random.Next(categories.Length)],
                    Amount = random.Next(100, 10000),
                    Date = DateTime.Now.AddDays(-random.Next(365))
                });
            }

            return records;
        }
    }

    class Program {
        static async Task Main() {
            var processor = new DataProcessor("log.txt");

            // Проблема: большой объем данных в памяти одновременно
            var testData = TestDataGenerator.GenerateRecords(100000);

            try {
                var result = await processor.ProcessDataAsync(testData);

                var filtered = processor.FilterAndSort(testData, "A", 500);
                var counts = processor.GetCategoryCounts(testData);
                var duplicates = processor.FindDuplicates(testData);

                Console.WriteLine($"Processed {result.CategoryResults.Count} categories");
                Console.WriteLine($"Found {duplicates.Count} duplicates");
            } finally {
                // Проблема: вручную вызываем Close() вместо using
                processor.Close();
            }
        }
    }

    /*
    ЗАДАНИЯ ДЛЯ РЕФАКТОРИНГА:

    1. Исправьте multiple enumeration в LINQ запросах
    2. Выберите правильные коллекции для конкретных задач
    3. Реализуйте правильный паттерн IDisposable
    4. Оптимизируйте алгоритмы с квадратичной сложностью
    5. Исправьте проблемы с Equals/GetHashCode для корректной работы Distinct()
    6. Оптимизируйте работу со строками
    7. Сделайте LINQ запросы более эффективными
    8. Исправьте проблемы с async/await
    9. Добавьте lazy evaluation где это возможно
    10. Защитите публичные коллекции от изменений
    11. Оптимизируйте работу с файлами

    БОНУС: Реализуйте streaming обработку для больших объемов данных
    */
}
