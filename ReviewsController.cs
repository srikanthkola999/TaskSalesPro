using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSales.API.Interfaces;
using TaskSales.API.MongoDB;

namespace TaskSales.API.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IMongoReviewService _svc;
        private readonly IEmployeeRepository _empRepo;

        public ReviewsController(
            IMongoReviewService svc,
            IEmployeeRepository empRepo)
        {
            _svc = svc;
            _empRepo = empRepo;
        }

        // POST api/reviews — Employee submits a review
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddReviewRequest req)
        {
            if (req.TaskId <= 0)
                return BadRequest(new { message = "Invalid TaskId" });
            if (req.EmployeeId <= 0)
                return BadRequest(new { message = "Invalid EmployeeId" });
            if (req.Rating < 1 || req.Rating > 5)
                return BadRequest(new { message = "Rating must be 1 to 5" });
            if (string.IsNullOrWhiteSpace(req.Comment))
                return BadRequest(new { message = "Comment is required" });

            await _svc.AddReviewAsync(req);
            return Ok(new { message = "Review submitted successfully" });
        }

        // GET api/reviews/task/{taskId} — Get all reviews for a task
        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetByTask(int taskId)
        {
            var reviews = await _svc.GetByTaskAsync(taskId);
            var result = new List<object>();

            foreach (var r in reviews)
            {
                var emp = await _empRepo.GetByIdAsync(r.EmployeeId);
                result.Add(new
                {
                    r.Id,
                    r.TaskId,
                    r.EmployeeId,
                    EmployeeName = emp?.Name ?? "Unknown",
                    r.Rating,
                    r.Comment,
                    r.CreatedDate
                });
            }
            return Ok(result);
        }

        // GET api/reviews/check/{taskId}/{employeeId}
        // — Check if employee already reviewed this task
        [HttpGet("check/{taskId}/{employeeId}")]
        public async Task<IActionResult> HasReviewed(int taskId, int employeeId)
        {
            var reviews = await _svc.GetByTaskAsync(taskId);
            var already = reviews.Any(r => r.EmployeeId == employeeId);
            return Ok(new { hasReviewed = already });
        }
    }
}