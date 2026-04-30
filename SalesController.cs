using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSales.API.DTOs;
using TaskSales.API.Entities;
using TaskSales.API.Interfaces;
using TaskSales.API.MongoDB;

namespace TaskSales.API.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize]
    public class SalesController(ISaleRepository repo, IMongoActivityLogService log) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok((await repo.GetAllAsync()).Select(ToDto));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        { var s = await repo.GetByIdAsync(id); return s == null ? NotFound() : Ok(ToDto(s)); }

        [HttpGet("employee/{empId}")]
        public async Task<IActionResult> GetByEmployee(int empId)
            => Ok((await repo.GetByEmployeeIdAsync(empId)).Select(ToDto));

        [HttpGet("analytics/by-category"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> ByCategory()
            => Ok(await repo.GetSalesByCategoryAsync());

        [HttpGet("analytics/monthly/{year}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Monthly(int year)
            => Ok(await repo.GetMonthlySalesAsync(year));

        [HttpGet("analytics/by-employee"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> ByEmployee()
            => Ok(await repo.GetSalesByEmployeeAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
        {
            var sale = await repo.AddAsync(new Sale
            {
                ProductName = dto.ProductName,
                Category = dto.Category,
                Amount = dto.Amount,
                SaleDate = dto.SaleDate,
                EmployeeId = dto.EmployeeId
            });
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            await log.LogAsync(uid, "RecordSale", "Sales", $"{dto.ProductName} ${dto.Amount}");
            return CreatedAtAction(nameof(GetById), new { id = sale.SaleId }, ToDto(sale));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSaleDto dto)
        {
            var s = await repo.GetByIdAsync(id);
            if (s == null) return NotFound();
            s.ProductName = dto.ProductName; s.Category = dto.Category;
            s.Amount = dto.Amount; s.SaleDate = dto.SaleDate; s.EmployeeId = dto.EmployeeId;
            await repo.UpdateAsync(s); return NoContent();
        }

        [HttpDelete("{id}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        { await repo.DeleteAsync(id); return NoContent(); }

        private static SaleDto ToDto(Sale s) => new()
        {
            SaleId = s.SaleId,
            ProductName = s.ProductName,
            Category = s.Category,
            Amount = s.Amount,
            SaleDate = s.SaleDate,
            EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee?.Name
        };
    }
}
