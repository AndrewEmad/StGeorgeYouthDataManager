using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Ensure it can be hit without authentication
public class HeartbeatController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}
