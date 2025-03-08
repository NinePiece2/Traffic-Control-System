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

        [HttpGet]
        [Route("GetDirectionAndTime")]
        public IActionResult GetDirectionAndTime(string DeviceStreamID)
        {
            try
            {
                var streamClient = _applicationDbContext.StreamClients.FirstOrDefault(x => x.DeviceStreamID == DeviceStreamID);

                if (streamClient == null)
                {
                    return NotFound($"No stream client found for DeviceStreamID: {DeviceStreamID}");
                }

                var activeSignal = _applicationDbContext.ActiveSignals
                    .FirstOrDefault(x => x.DeviceStreamUID == streamClient.UID && x.IsActive == true);

                if (activeSignal == null)
                {
                    return NotFound($"No active signal found for ID: {streamClient.UID}");
                }

                return Ok(new
                {
                    Direction1 = activeSignal.Direction1,
                    Direction2 = activeSignal.Direction2,
                    Direction1Time = activeSignal.Direction1Green,
                    Direction2Time = activeSignal.Direction2Green,
                    PedestrianWalkTime = activeSignal.PedestrianWalkTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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

                var activeSignalID = _applicationDbContext.ActiveSignals
                    .Where(x => x.DeviceStreamUID == _applicationDbContext.StreamClients
                        .Where(c => c.DeviceStreamID == model.DeviceID)
                        .Select(c => c.UID)
                        .FirstOrDefault())
                    .Select(x => x.ID)
                    .FirstOrDefault();

                var violation = new TrafficViolations
                {
                    ActiveSignalID = activeSignalID,
                    LicensePlate = model.LicensePlate,
                    Filename = model.Filename,
                    DateCreated = DateTime.Now 
                };

                _applicationDbContext.Add(violation);
                _applicationDbContext.SaveChanges();

                return Ok(new { Message = "Traffic violation added successfully", Violation = violation });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.ToString()}");
            }
        }

        [HttpGet]
        [Route("GetGUID")]
        public IActionResult GetGUID()
        {
            return Ok(new {GUID = Guid.NewGuid()});
        }
    }
}