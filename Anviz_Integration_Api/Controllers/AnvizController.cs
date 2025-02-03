using Anviz_Integration_Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Anviz_Integration_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnvizController : ControllerBase
    {
        private IAnvizService _anvizService;
        private ILogger<AnvizController> _logger;

        public AnvizController(IAnvizService anvizSerivce, ILogger<AnvizController> logger)
        {
            _anvizService = anvizSerivce;
            _logger = logger;
        }
        [HttpPost]
        [Route("/auth")]
        public async Task Index()
        {
            _logger.LogInformation("Ana sayfa endpoint'ine istek geldi.");
            await _anvizService.GetToken();
        }
    }
}
