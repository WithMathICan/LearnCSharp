using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringTask2 {
    // ЗАДАЧА 2: Система событий с утечками памяти и проблемами производительности
    // Проблемы: утечки памяти в событиях, неправильное использование LINQ,
    // проблемы с замыканиями, нарушение принципов наследования

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // Проблема: базовый класс должен быть abstract, но не является
    public class BaseEventHandler {
        protected List<string> _logs = new List<string>();

        // Проблема: virtual метод вместо abstract
        public virtual void Handle(object eventData) {
            // Проблема: string concatenation в цикле
            string logEntry = "Handling event at " + DateTime.Now.ToString();
            _logs.Add(logEntry);
        }

        // Проблема: возвращает internal коллекцию
        public List<string> GetLogs() {
            return _logs;
        }
    }

    // Проблема: неправильное использование new вместо override
    public class OrderEventHandler : BaseEventHandler {
        private Dictionary<string, object> _orderCache = new Dictionary<string, object>();

        // Проблема: скрывает метод базового класса через new
        public new void Handle(object eventData) {
            base.Handle(eventData);

            // Проблема: boxing для примитивов
            var orderId = eventData.GetType().GetProperty("OrderId")?.GetValue(eventData);
            _orderCache[orderId?.ToString() ?? "unknown"] = eventData;

            // Проблема: создание замыкания в цикле без понимания последствий
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++) {
                tasks.Add(Task.Run(() =>
                {
                    // Проблема: захват переменной цикла
                    ProcessOrder(i, eventData);
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private void ProcessOrder(int index, object orderData) {
            // Проблема: неэффективное использование LINQ
            var recentLogs = _logs.Where(log => log.Contains("Handling"))
                                 .Where(log => log.Contains(DateTime.Now.Date.ToString()))
                                 .ToList() // Проблема: промежуточная материализация
                                 .Where(log => !string.IsNullOrEmpty(log))
                                 .ToList();

            Console.WriteLine($"Processing order {index}, Recent logs: {recentLogs.Count}");
        }
    }

    public class EventManager<T> where T : class {
        // Проблема: статическое событие может привести к утечкам памяти
        public static event Action<T> OnEventReceived;

        private List<BaseEventHandler> _handlers = new List<BaseEventHandler>();

        // Проблема: нет проверки на null, возможна утечка памяти
        public void Subscribe(BaseEventHandler handler) {
            _handlers.Add(handler);
            OnEventReceived += (eventData) =>
            {
                // Проблема: замыкание захватывает handler и может привести к утечке
                handler.Handle(eventData);
            };
        }

        // Проблема: нет способа отписаться от статического события
        public void Unsubscribe(BaseEventHandler handler) {
            _handlers.Remove(handler);
            // Проблема: нет отписки от статического события
        }

        public void PublishEvent(T eventData) {
            // Проблема: foreach создает копию списка, но не защищает от concurrent modification
            foreach (var handler in _handlers) {
                try {
                    handler.Handle(eventData);
                } catch (Exception ex) {
                    // Проблема: проглатывание исключений без логирования
                }
            }

            // Проблема: может быть null, нет thread-safety
            OnEventReceived?.Invoke(eventData);
        }

        // Проблема: неэффективный LINQ для простой операции
        public BaseEventHandler FindHandler(Type handlerType) {
            return _handlers.Where(h => h.GetType() == handlerType)
                           .FirstOrDefault();
        }

        // Проблема: использует рефлексию неэффективно
        public string GetAllHandlerStats() {
            string result = "";

            foreach (var handler in _handlers) {
                var handlerType = handler.GetType();
                var logsProperty = handlerType.GetProperty("GetLogs");

                if (logsProperty != null) {
                    // Проблема: каждый раз вызываем рефлексию
                    var logs = logsProperty.GetValue(handler);
                    result += $"Handler {handlerType.Name}: logs available\n";
                }
            }

            return result;
        }
    }

    // Проблема: класс не immutable, хотя должен быть
    public class OrderCreatedEvent {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Items { get; set; } = new List<string>();

        // Проблема: переопределен Equals, но не GetHashCode
        public override bool Equals(object obj) {
            if (obj is OrderCreatedEvent other) {
                return OrderId == other.OrderId;
            }
            return false;
        }
    }

    // Проблема: обобщенный класс используется неправильно
    public class SpecializedEventManager : EventManager<object> {
        // Проблема: теряется типобезопасность из-за object
        public void HandleSpecialEvent(object specialEvent) {
            // Проблема: приведение типов без проверки
            var orderEvent = (OrderCreatedEvent)specialEvent;
            PublishEvent(orderEvent);
        }
    }

    class Program {
        static void Main2() {
            var eventManager = new EventManager<OrderCreatedEvent>();
            var orderHandler = new OrderEventHandler();

            eventManager.Subscribe(orderHandler);

            // Проблема: создается много объектов в цикле
            for (int i = 0; i < 1000; i++) {
                var orderEvent = new OrderCreatedEvent {
                    OrderId = "ORDER_" + i.ToString(),
                    Amount = 100.50m,
                    CreatedAt = DateTime.Now,
                    Items = new List<string> { "Item1", "Item2" }
                };

                eventManager.PublishEvent(orderEvent);
            }

            Console.WriteLine(eventManager.GetAllHandlerStats());
        }
    }

    /*
    ЗАДАНИЯ ДЛЯ РЕФАКТОРИНГА:

    1. Исправьте утечки памяти в системе событий
    2. Правильно реализуйте наследование (abstract/virtual/override)
    3. Оптимизируйте LINQ запросы и устраните лишние материализации
    4. Исправьте проблемы с замыканиями в циклах
    5. Реализуйте правильную систему подписки/отписки
    6. Сделайте события immutable там где нужно
    7. Исправьте проблемы с boxing/unboxing
    8. Добавьте типобезопасность в generics
    9. Оптимизируйте использование рефлексии
    10. Реализуйте правильный Equals/GetHashCode
    11. Добавьте proper exception handling

    БОНУС: Реализуйте covariance/contravariance для делегатов событий
    */
}
