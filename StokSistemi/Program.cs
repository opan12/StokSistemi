using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StokSistemi.Data;
using StokSistemi.Models;
using StokSistemi.Service;
using StokSistemi.Services;

var builder = WebApplication.CreateBuilder(args);

// Baðlantý dizesini al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// DbContext kaydet
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity yapýlandýrmasý
builder.Services.AddIdentity<Customer, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Diðer servis kayýtlarý
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<CustomerQueue>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<OrderService>();

// Arka plan hizmeti ekleniyor
builder.Services.AddHostedService<OrderCancellationService>(); // Buraya ekleniyor

builder.Services.AddHostedService<PriorityScoreUpdater>();

builder.Services.AddControllersWithViews();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[] { "Admin", "User" }; // Oluþturulacak roller
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                // Hata durumunda log yazabilirsiniz
            }
        }
    }
}

// Middleware yapýlandýrmalarý
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Kimlik doðrulama middleware'i
app.UseAuthorization();

// Log için özel rota
app.MapControllerRoute(
    name: "log",
    pattern: "log/{action=Index}/{id?}",
    defaults: new { controller = "Log" });

// Skor için özel rota
app.MapControllerRoute(
    name: "skor",
    pattern: "Admin/Skor",
    defaults: new { controller = "Skor", action = "Index" });


// Varsayýlan rota
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

app.Run();
