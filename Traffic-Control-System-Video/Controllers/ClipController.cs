using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Traffic_Control_System_Video.Controllers
{
    [Authorize]
    [Route("/[controller]")]
    [ApiController]
    public class ClipController : ControllerBase
    {
    }
}
