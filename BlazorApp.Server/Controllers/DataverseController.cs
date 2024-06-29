using System.Threading.Tasks;
using BlazorApp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlazorApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataverseController : ControllerBase
    {
        private readonly DataverseService _dataverseService;
        private readonly ILogger<DataverseController> _logger;

        public DataverseController(DataverseService dataverseService, ILogger<DataverseController> logger)
        {
            _dataverseService = dataverseService;
            _logger = logger;
        }

        [HttpGet("fetchUserId")]
        public async Task<IActionResult> FetchUserId()
        {
            _logger.LogInformation("Fetching user ID from Dataverse.");
            var userId = await _dataverseService.FetchUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogError("Failed to fetch user ID.");
                return StatusCode(500, "Failed to fetch user ID.");
            }
            return Ok(new { UserId = userId });
        }
    }
}
