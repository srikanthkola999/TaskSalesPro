using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSales.API.DTOs;
using TaskSales.API.Entities;
using TaskSales.API.Interfaces;
using TaskSales.API.MongoDB;

namespace TaskSales.API.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize]
    public class TasksController(ITaskRepository repo, IMongoActivityLogService log) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok((await repo.GetAllAsync()).Select(ToDto));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        { var t = await repo.GetByIdAsync(id); return t == null ? NotFound() : Ok(ToDto(t)); }

        [HttpGet("employee/{empId}")]
        public async Task<IActionResult> GetByEmployee(int empId)
            => Ok((await repo.GetByEmployeeIdAsync(empId)).Select(ToDto));

        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdue()
            => Ok((await repo.GetOverdueTasksAsync()).Select(ToDto));

        [HttpPost, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            // ✅ Parse string → enum safely with fallback
            if (!Enum.TryParse<AppTaskStatus>(dto.Status, ignoreCase: true, out var status))
                status = AppTaskStatus.Pending;
            if (!Enum.TryParse<AppTaskPriority>(dto.Priority, ignoreCase: true, out var priority))
                priority = AppTaskPriority.Medium;

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = status,
                Priority = priority,
                AssignedEmployeeId = dto.AssignedEmployeeId,
                DueDate = dto.DueDate
            };
            var created = await repo.AddAsync(task);

            // ✅ Fire-and-forget — MongoDB failure won't crash the save
            _ = Task.Run(async () => {
                try
                {
                    await log.LogAsync(UserId(), "CreateTask", "Tasks",
                    $"'{dto.Title}' → Employee {dto.AssignedEmployeeId}");
                }
                catch { }
            });

            return CreatedAtAction(nameof(GetById), new { id = created.TaskId }, ToDto(created));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTaskDto dto)
        {
            var task = await repo.GetByIdAsync(id);
            if (task == null) return NotFound();

            if (!Enum.TryParse<AppTaskStatus>(dto.Status, ignoreCase: true, out var status))
                status = AppTaskStatus.Pending;
            if (!Enum.TryParse<AppTaskPriority>(dto.Priority, ignoreCase: true, out var priority))
                priority = AppTaskPriority.Medium;

            task.Title = dto.Title; task.Description = dto.Description;
            task.Status = status; task.Priority = priority;
            task.AssignedEmployeeId = dto.AssignedEmployeeId; task.DueDate = dto.DueDate;
            await repo.UpdateAsync(task);

            _ = Task.Run(async () => {
                try { await log.LogAsync(UserId(), "UpdateTask", "Tasks", $"Updated {id}"); }
                catch { }
            });

            return NoContent();
        }

        [HttpDelete("{id}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        { await repo.DeleteAsync(id); return NoContent(); }

        private string UserId() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        private static TaskDto ToDto(TaskItem t) => new()
        {
            TaskId = t.TaskId,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),       // ✅ correct property name
            Priority = t.Priority.ToString(),   // ✅ correct property name
            AssignedEmployeeId = t.AssignedEmployeeId,
            EmployeeName = t.AssignedEmployee?.Name,
            DueDate = t.DueDate,
            CreatedDate = t.CreatedDate
        };
    }
}
