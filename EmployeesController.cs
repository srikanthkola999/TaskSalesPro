using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSales.API.DTOs;
using TaskSales.API.Entities;
using TaskSales.API.Interfaces;
using TaskSales.API.MongoDB;

namespace TaskSales.API.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize]
    public class EmployeesController(IEmployeeRepository repo, IMongoActivityLogService log) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await repo.GetAllAsync();
            return Ok(list.Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                Name = e.Name,
                Email = e.Email,
                Department = e.Department,
                Role = e.Role,
                CreatedDate = e.CreatedDate,
                UserId = e.UserId
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        { var e = await repo.GetByIdAsync(id); return e == null ? NotFound() : Ok(ToDto(e)); }

        [HttpPost, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
        {
            var e = await repo.AddAsync(new Employee
            { Name = dto.Name, Email = dto.Email, Department = dto.Department, Role = dto.Role });
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            await log.LogAsync(uid, "CreateEmployee", "Employees", $"Created: {dto.Name}");
            return CreatedAtAction(nameof(GetById), new { id = e.EmployeeId }, ToDto(e));
        }

        [HttpPut("{id}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateEmployeeDto dto)
        {
            var e = await repo.GetByIdAsync(id);
            if (e == null) return NotFound();
            e.Name = dto.Name; e.Email = dto.Email; e.Department = dto.Department; e.Role = dto.Role;
            await repo.UpdateAsync(e); return NoContent();
        }

        [HttpDelete("{id}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        { await repo.DeleteAsync(id); return NoContent(); }

        private static EmployeeDto ToDto(Employee e) => new()
        {
            EmployeeId = e.EmployeeId,
            Name = e.Name,
            Email = e.Email,
            Department = e.Department,
            Role = e.Role,
            CreatedDate = e.CreatedDate,
            UserId = e.UserId
        };
    }
}
