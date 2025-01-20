using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;

namespace Traffic_Control_System_API.Controllers
{
    [Authorize]
    [Route("/[controller]")]
    [ApiController]
    public class TrafficController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public TrafficController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;

        }

        [HttpGet]
        [Route("APItest")]
        public IActionResult APItest()
        {
            return Ok("Test");
        }

        [HttpGet]
        [Route("GetStreamClientKey")]
        public IActionResult GetStreamClientKey(string DeviceStreamID)
        {
            try
            {
                if (string.IsNullOrEmpty(DeviceStreamID))
                {
                    return BadRequest("DeviceStreamID is required");
                }

                var streamClient = _applicationDbContext.StreamClients.FirstOrDefault(x => x.DeviceStreamID == DeviceStreamID);

                if (streamClient == null)
                {
                    return NotFound($"No Key found for DeviceStreamID: {DeviceStreamID}");
                }

                return Ok(new { DeviceStreamKEY = streamClient.DeviceStreamKEY });

            }

            catch (Exception ex)
            {
                {
                    return BadRequest(ex.Message);
                }
            }

        }

        [HttpPost]
        [Route("AddTrafficViolation")]
        public IActionResult AddTrafficViolation([FromBody] TrafficViolationsModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var violation = new TrafficViolations
                {
                    ActiveSignalID = model.ActiveSignalID,
                    LicensePlate = model.LicensePlate,
                    VideoURL = model.VideoURL,
                    DateCreated = DateTime.Now // Automatically set the creation date
                };

                //_applicationDbContext.TrafficViolations.Add(violation);
                //_db.Add(violation);
                _applicationDbContext.Add(violation);
                _applicationDbContext.SaveChanges();

                return Ok(new { Message = "Traffic violation added successfully", Violation = violation });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.ToString()}");
            }
        }


    }



}
