using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public class LoggerExample2 {
    // В этом примере мы будем динамически интернировать строки.
    // Вместо фиксированного массива мы будем использовать HashSet для быстрого поиска
    // и чтобы избежать дублирования в нашем пуле.
    private static readonly HashSet<string> InternedCategories = new HashSet<string>();

    public static void LogManyMessages(string[] categories, string[] messages) {
        Console.WriteLine($"Начинаем обработку {categories.Length} логов...\n");

        int internedCount = 0;
        int newStringCount = 0;

        for (int i = 0; i < categories.Length; i++) {
            // Динамически создаем строку, чтобы компилятор её не интернировал
            string dynamicCategory = categories[i].ToUpper();

            // Проверяем, есть ли эта строка в нашем пуле.
            if (InternedCategories.Add(dynamicCategory)) {
                // Это новая уникальная строка.
                newStringCount++;
            } else {
                // Строка уже была интернирована.
                internedCount++;
            }

            // На этом этапе, в реальном приложении, мы бы использовали интернированную строку
            // для дальнейшей работы, чтобы избежать создания её копий.
            // Например:
            // string categoryForLog = InternedCategories.First(c => c == dynamicCategory); 
            // Это лишний код, но он показывает идею.
            // В реальной жизни мы просто используем результат InternedCategories.Add().
        }

        Console.WriteLine($"\nВсего строк обработано: {categories.Length}");
        Console.WriteLine($"Строки, которые были интернированы повторно: {internedCount}");
        Console.WriteLine($"Новые, уникальные строки: {newStringCount}");
        Console.WriteLine($"Общее количество уникальных категорий: {InternedCategories.Count}");
    }

    public static void Main1() {
        // Создаем массив с очень большим количеством повторяющихся строк,
        // но с некоторыми уникальными.
        string[] categories = Enumerable.Repeat("info", 97)
            .Concat(new[] { "error", "debug", "warning", "critical", "info" }) // добавим "info" ещё раз
            .ToArray();

        LogManyMessages(categories, new string[categories.Length]);
    }
}


public class LoggerExample1 {
    // Интернируем часто используемые категории один раз.
    // Это гарантирует, что в пуле строк будут только эти три объекта.
    private static readonly string[] InternedCategories = new[] { "INFO", "ERROR", "DEBUG" }
        //.Select(String.Intern)
        .ToArray();

    public static void LogManyMessages(string[] categories, string[] messages) {
        Console.WriteLine($"Начинаем обработку {categories.Length} логов...");

        // Мы будем использовать эти две переменные для демонстрации.
        int totalStringAllocations = 0;
        int totalReferenceEqualities = 0;

        for (int i = 0; i < categories.Length; i++) {
            // Здесь мы имитируем получение новой строки категории извне
            string incomingCategory = categories[i];

            // Проверяем, существует ли входящая строка в нашем интернированном пуле.
            // Вместо посимвольного сравнения мы можем использовать более быстрый поиск,
            // зная, что все наши интернированные строки уникальны.
            string internedCategory = InternedCategories.FirstOrDefault(
                cat => cat == incomingCategory); // Этот '==' работает быстрее, если строки уже интернированы!

            // Если входящая строка совпала с одной из интернированных...
            if (internedCategory != null) {
                // Проверим, что это действительно один и тот же объект в памяти.
                if (object.ReferenceEquals(internedCategory, incomingCategory)) {
                    totalReferenceEqualities++;
                }

                // Используем интернированную строку для дальнейшей работы.
                Console.WriteLine($"\t[{internedCategory}] - Сообщение {i + 1}");
            } else {
                // Если строка не совпала, она является новым уникальным объектом.
                totalStringAllocations++;
                Console.WriteLine($"\t[{incomingCategory}] - Сообщение {i + 1}");
            }
        }

        Console.WriteLine($"\nВсего проверок на равенство ссылок (ReferenceEquals): {totalReferenceEqualities}");
        Console.WriteLine($"Всего новых выделений памяти для уникальных строк: {totalStringAllocations}");
    }

    public static void Main1() {
        // Создаём 100 категорий, 97 из которых - это "INFO".
        string[] categories = Enumerable.Repeat("INFO", 97)
            .Concat(new[] { "ERROR", "DEBUG", "WARN" })
            .ToArray();

        string[] messages = new string[100]; // Просто заглушки
        for (int i = 0; i < 100; i++) {
            messages[i] = $"Сообщение {i}";
        }

        LogManyMessages(categories, messages);
    }
}

internal class Program1{
    //private static void Main(string[] args) {
    //    CompareStringsIntern("Ukra", "ine");
    //}

    static void CompareStrings(string s1, string s2) {
        string str1 = "Ukraine";
        string str2 = "Ukraine";
        string str3 = "Ukra" +"ine";
        string str4 = $"{s1}{s2}";

        Console.WriteLine(str1 == str2);
        Console.WriteLine(string.ReferenceEquals(str1, str2)); //True
        Console.WriteLine(string.ReferenceEquals(str1, str3)); //True
        Console.WriteLine(string.ReferenceEquals(str1, str4)); //False
    }

    static void CompareStringsIntern(string s1, string s2) {
        string.Intern("Ukraine");
        string str1 = "Ukraine";
        string str2 = "Ukraine";
        string str3 = "Ukra" + "ine";
        string str4 = $"{s1}{s2}";

        Console.WriteLine(str1 == str2);
        Console.WriteLine(string.ReferenceEquals(str1, str2)); //True
        Console.WriteLine(string.ReferenceEquals(str1, str3)); //True
        Console.WriteLine(string.ReferenceEquals(str1, str4)); //False
    }
}