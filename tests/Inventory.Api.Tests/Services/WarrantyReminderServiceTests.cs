using FluentAssertions;
using Inventory.Api.Services;
using Inventory.Core.Entities;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Inventory.Api.Tests.Services;

/// <summary>
/// Tests for <see cref="WarrantyReminderService.ProcessWarrantyRemindersAsync"/>.
/// Uses EF Core InMemory directly — no WebApplicationFactory needed.
/// A fixed reference date (<see cref="Today"/>) is passed to the method so tests
/// are deterministic and never depend on the real clock.
/// </summary>
public class WarrantyReminderServiceTests
{
    // Fixed reference date used as "today" in all tests
    private static readonly DateTime Today = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ItemExpiringInThreeDays_CreatesReminder()
    {
        using var db = CreateDbContext();
        var item = SeedItem(db, "Washing Machine", Today.AddDays(3));

        await RunService(db);

        var reminders = await db.WarrantyReminders.ToListAsync();
        reminders.Should().HaveCount(1);
        reminders[0].InventoryItemId.Should().Be(item.Id);
        reminders[0].Message.Should().Contain("Washing Machine");
    }

    [Fact]
    public async Task ItemExpiringToday_CreatesReminder()
    {
        using var db = CreateDbContext();
        SeedItem(db, "Blender", Today);

        await RunService(db);

        (await db.WarrantyReminders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ItemExpiringAtUpperBoundary_FiveDaysAway_CreatesReminder()
    {
        // cutoff = today + 6, so today+5 IS inside the window (5 < 6)
        using var db = CreateDbContext();
        SeedItem(db, "Dryer", Today.AddDays(5));

        await RunService(db);

        (await db.WarrantyReminders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ItemExpiringJustOutsideWindow_SixDaysAway_DoesNotCreateReminder()
    {
        // cutoff = today + 6, so today+6 is NOT inside the window (6 is NOT < 6)
        using var db = CreateDbContext();
        SeedItem(db, "TV", Today.AddDays(6));

        await RunService(db);

        (await db.WarrantyReminders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ItemWithAlreadyExpiredWarranty_DoesNotCreateReminder()
    {
        using var db = CreateDbContext();
        SeedItem(db, "Microwave", Today.AddDays(-1));

        await RunService(db);

        (await db.WarrantyReminders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CalledTwice_WithSameItems_DoesNotCreateDuplicateReminders()
    {
        using var db = CreateDbContext();
        SeedItem(db, "Dishwasher", Today.AddDays(3));

        await RunService(db);
        await RunService(db); // second run — should detect existing reminder and skip

        (await db.WarrantyReminders.CountAsync()).Should()
            .Be(1, because: "a second pass must not create duplicate reminders");
    }

    [Fact]
    public async Task MultipleItems_OnlyItemsInWindowGetReminders()
    {
        using var db = CreateDbContext();
        var inside1 = SeedItem(db, "Fridge", Today.AddDays(1));
        var inside2 = SeedItem(db, "Oven", Today.AddDays(5));
        SeedItem(db, "Lamp", Today.AddDays(6));   // just outside
        SeedItem(db, "Fan",  Today.AddDays(-1));  // already expired

        await RunService(db);

        var reminders = await db.WarrantyReminders.ToListAsync();
        reminders.Should().HaveCount(2);
        reminders.Select(r => r.InventoryItemId)
                 .Should().BeEquivalentTo(new[] { inside1.Id, inside2.Id });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Creates a fresh InMemory DbContext isolated to this test.</summary>
    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InventoryDbContext(options);
    }

    /// <summary>Seeds a single InventoryItem and saves it.</summary>
    private static InventoryItem SeedItem(InventoryDbContext db, string name, DateTime warrantyExpiry)
    {
        var item = new InventoryItem
        {
            Name = name,
            WarrantyExpiry = warrantyExpiry,
            PurchaseDate = Today.AddDays(-365),
            PurchasePrice = 100m,
            RoomId = 0  // InMemory does not enforce FK constraints
        };
        db.InventoryItems.Add(item);
        db.SaveChanges();
        return item;
    }

    /// <summary>Instantiates the service and runs one check pass with the fixed <see cref="Today"/>.</summary>
    private static async Task RunService(InventoryDbContext db)
    {
        var sp = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var service = new WarrantyReminderService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            new ConfigurationBuilder().Build(),
            NullLogger<WarrantyReminderService>.Instance);

        await service.ProcessWarrantyRemindersAsync(db, Today);
    }
}
