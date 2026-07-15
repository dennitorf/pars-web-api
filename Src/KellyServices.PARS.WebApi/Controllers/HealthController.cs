using Microsoft.AspNetCore.Mvc;

namespace KellyServices.PARS.WebApi.Controllers
{
    [Route("ns-pars/api/[controller]")]
    [ApiController]
    public class Health : ControllerBase
    {
        [HttpGet("GetHealthCheck")]
        public string Get()
        {
            return "i'm healthy";
        }

        public StatusCodeResult DefaultResponse()
        {
            return StatusCode(505);
        }

    }
}