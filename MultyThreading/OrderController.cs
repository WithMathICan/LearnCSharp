using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MultyThreading {

    /*Какие проблемы возникнут при использовании этого кода в многопоточной среде 
     * (учти, что ASP.NET Core обрабатывает запросы асинхронно)?
Отрефактори этот код, чтобы он стал потокобезопасным. 
    Используй подходящий механизм синхронизации и объясни, почему ты его выбрал.
    
    Ловушка: Как статическое поле _totalOrders влияет на масштабируемость приложения, 
    если оно развернуто на нескольких серверах (например, в Azure с горизонтальным масштабированием)? 
    Как бы ты решил эту проблему?*/

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase {
        private static int _totalOrders = 0;

        [HttpPost]
        public IActionResult AddOrder([FromBody] string order) {
            _totalOrders++;
            return Ok();
        }

        [HttpGet("count")]
        public IActionResult GetOrderCount() {
            return Ok(_totalOrders);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController_ThreadSafe : ControllerBase {
        private static int _totalOrders = 0;

        [HttpPost]
        public IActionResult AddOrder([FromBody] string order) {
            Interlocked.Increment(ref _totalOrders);
            return Ok();
        }

        [HttpGet("count")]
        public IActionResult GetOrderCount() {
            return Ok(_totalOrders);
        }
    }

    

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController_WithRedis : ControllerBase {
        private readonly StackExchange.Redis.IDatabase _redis;

        public OrderController_WithRedis(IConnectionMultiplexer redis) {
            _redis = redis.GetDatabase();
        }

        [HttpPost]
        public async Task<IActionResult> AddOrder([FromBody] string order) {
            if (string.IsNullOrEmpty(order)) return BadRequest("Order description is required");
            await _redis.StringIncrementAsync("total_orders");
            return Ok();
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetOrderCount() {
            RedisValue count = await _redis.StringGetAsync("total_orders");
            return Ok((int)count);
        }
    }
}
