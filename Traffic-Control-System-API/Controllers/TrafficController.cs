using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Traffic_Control_System_API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TrafficController : ControllerBase
    {
    }
}
