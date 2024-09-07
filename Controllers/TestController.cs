using Microsoft.AspNetCore.Mvc;

namespace ApiDataDog.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        public TestController(ILogger<TestController> logger)
        {
        }

        [HttpGet]
        public IActionResult Get()
        {
            var random = new Random().Next(0, 10);

            if (random > 8)
                throw new Exception($"Erro valor do random é: {random}");

            return Ok();
        }
    }
}
