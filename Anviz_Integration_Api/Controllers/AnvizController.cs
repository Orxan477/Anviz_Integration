using System.Collections.Specialized;
using Anviz_Integration_Api.Model;
using Anviz_Integration_Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Anviz_Integration_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnvizController : ControllerBase
    {
        private IAnvizService _anvizService;

        public AnvizController(IAnvizService anvizSerivce)
        {
            _anvizService = anvizSerivce;
        }
        [HttpPost]
        [Route("/auth")]
        public async Task Index()
        {
            await _anvizService.GetToken();
        }
    }
}
