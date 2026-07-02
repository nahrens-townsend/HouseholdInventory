using Inventory.Api.Services;
using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Api.Tests.Infrastructure;

/// <summary>
/// Bootstraps the real ASP.NET Core pipeline for integration testing,
/// replacing SQL Server with an isolated EF Core InMemory database.
/// Each factory instance gets its own database so test classes don't share state.
/// </summary>
public class InventoryWebAppFactory : WebApplicationFactory<Program>
{
    // Unique name ensures each factory instance (i.e., each test class) has its own database.
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContext registration from Program.cs
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // Replace with an isolated InMemory database
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Remove the background service — it uses real timers and would
            // interfere with deterministic tests
            var reminderDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(WarrantyReminderService));
            if (reminderDescriptor != null)
                services.Remove(reminderDescriptor);
        });
    }

    /// <summary>
    /// Runs an action against a scoped DbContext. Use for seeding test data
    /// or asserting database state after an HTTP call.
    /// </summary>
    public void UseDbContext(Action<InventoryDbContext> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        action(db);
    }
}
