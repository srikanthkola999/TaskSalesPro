using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TaskSales.API.Data;
using TaskSales.API.Entities;
using TaskSales.API.Interfaces;
using TaskSales.API.MongoDB;

namespace TaskSales.API.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class AuthController(UserManager<ApplicationUser> userMgr,
        SignInManager<ApplicationUser> signMgr, RoleManager<IdentityRole> roleMgr,
        IEmployeeRepository empRepo, IMongoActivityLogService log) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var user = new ApplicationUser { UserName = req.Email, Email = req.Email, FullName = req.FullName };
            var result = await userMgr.CreateAsync(user, req.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            var role = req.Role == "Admin" ? "Admin" : "Employee";
            await userMgr.AddToRoleAsync(user, role);

            if (role == "Employee")
            {
                var emp = await empRepo.AddAsync(new Employee
                {
                    Name = req.FullName,
                    Email = req.Email,
                    Department = req.Department ?? "General",
                    Role = role,
                    UserId = user.Id
                });
                user.EmployeeId = emp.EmployeeId;
                await userMgr.UpdateAsync(user);
            }
            await log.LogAsync(user.Id, "Register", "Auth", $"Registered as {role}: {req.Email}");
            return Ok(new { message = "Registered successfully", role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var result = await signMgr.PasswordSignInAsync(req.Email, req.Password, req.RememberMe, false);
            if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials" });
            var user = await userMgr.FindByEmailAsync(req.Email);
            var roles = await userMgr.GetRolesAsync(user!);
            await log.LogAsync(user!.Id, "Login", "Auth", $"Login: {req.Email}");
            return Ok(new { user.Email, user.FullName, roles, user.EmployeeId });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        { await signMgr.SignOutAsync(); return Ok(new { message = "Logged out" }); }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!(User.Identity?.IsAuthenticated ?? false)) return Unauthorized();
            var user = await userMgr.GetUserAsync(User);
            var roles = await userMgr.GetRolesAsync(user!);
            return Ok(new { user!.Email, user.FullName, roles, user.EmployeeId });
        }
    }

    public record RegisterRequest(
        [Required] string FullName,
        [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Password,
        string Role = "Employee",
        string? Department = null);

    public record LoginRequest(
        [Required] string Email,
        [Required] string Password,
        bool RememberMe = false);
}
