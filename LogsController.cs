using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSales.API.MongoDB;

namespace TaskSales.API.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize(Roles = "Admin")]
    public class LogsController(IMongoActivityLogService svc) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int limit = 200)
            => Ok(await svc.GetLogsAsync(limit));

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
            => Ok(await svc.GetLogsByUserAsync(userId));
    }
}
