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
            // EF Core 8+ registers options via IDbContextOptionsConfiguration<T> rather than a
            // plain DbContextOptions<T> descriptor — remove both patterns so the SQL Server
            // configuration from Program.cs is fully replaced.
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<InventoryDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContextOptionsConfiguration") &&
                     d.ServiceType.GenericTypeArguments.Length == 1 &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(InventoryDbContext)))
                .ToList();

            foreach (var d in efDescriptors)
                services.Remove(d);

            // Build a dedicated InMemory service provider so EF never sees both SqlServer
            // and InMemory providers in the same IServiceProvider (which would throw).
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<InventoryDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
            });

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

