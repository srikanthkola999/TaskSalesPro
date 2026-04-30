using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskSales.API.Data;
using TaskSales.API.Interfaces;
using TaskSales.API.Middleware;
using TaskSales.API.MongoDB;
using TaskSales.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── SQL Server + EF Core ──────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Core Identity (cookie auth, NO JWT) ───────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o => {
    o.Password.RequireDigit = true;
    o.Password.RequiredLength = 8;
    o.Password.RequireNonAlphanumeric = false;
    o.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o => {
    o.Cookie.Name = "TaskSalesAuth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

// ── CORS (allow Blazor UI) ────────────────────────────────
builder.Services.AddCors(o => o.AddPolicy("Blazor", p =>
    p.WithOrigins(builder.Configuration["BlazorOrigin"] ?? "https://localhost:7001")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ── MongoDB ───────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<IMongoActivityLogService, MongoActivityLogService>();
builder.Services.AddSingleton<IMongoReviewService, MongoReviewService>();

// ── Repositories (Scoped) ─────────────────────────────────
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
// REPLACE this line:
builder.Services.AddControllers();

// WITH THIS:
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ✅ Makes enums serialize/deserialize as strings ("Pending" not 0)
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Auto-create roles on startup ─────────────────────────
using (var scope = app.Services.CreateScope())
{
    var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Employee" })
        if (!await rm.RoleExistsAsync(role))
            await rm.CreateAsync(new IdentityRole(role));
}

app.UseMiddleware<GlobalExceptionMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors("Blazor");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
