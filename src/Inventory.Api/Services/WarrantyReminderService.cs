using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Inventory.Core.Entities;

namespace Inventory.Api.Services;

/// <summary>
/// Background service that periodically checks for inventory items with warranties
/// nearing expiration and creates reminder records in the database.
/// </summary>
public class WarrantyReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WarrantyReminderService> _logger;

    public WarrantyReminderService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<WarrantyReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // How often the service runs (defaults to 24 hours if not configured)
        int intervalMinutes = _configuration.GetValue<int>(
            "WarrantyReminderService:CheckIntervalInMinutes",
            1440);

        _logger.LogInformation(
            "WarrantyReminderService started. Check interval: {IntervalMinutes} minutes.",
            intervalMinutes);

        // Main background loop - runs until application shutdown
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "WarrantyReminderService checking for expiring warranties at {Time}.",
                    DateTime.UtcNow);

                // Create a DI scope because DbContext is scoped, but this service is singleton
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                await ProcessWarrantyRemindersAsync(context, DateTime.UtcNow.Date, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log unexpected failures but do not crash the background service
                _logger.LogError(ex,
                    "WarrantyReminderService encountered an error during check.");
            }

            // Wait before running the next check cycle
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Checks for items with warranties expiring within the next 5 days and creates
    /// reminder records for any that have not yet been reminded.
    /// Extracted for testability — callers supply the reference date instead of <c>DateTime.UtcNow</c>.
    /// </summary>
    public async Task ProcessWarrantyRemindersAsync(
        InventoryDbContext context,
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        // Define the time window for "expiring soon" warranties
        var cutoff = today.AddDays(6); // ~5 day warning window + inclusive buffer

        // Fetch items whose warranty expires within the next ~5-6 days
        var expiringItems = await context.InventoryItems
            .Where(i => i.WarrantyExpiry >= today && i.WarrantyExpiry < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var item in expiringItems)
        {
            // Check if we already created a reminder for this item within its warning window
            bool alreadyReminded = await context.WarrantyReminders
                .AnyAsync(wr =>
                    wr.InventoryItemId == item.Id &&
                    wr.CreatedAt >= item.WarrantyExpiry.AddDays(-5),
                    cancellationToken);

            // If no reminder exists, create one
            if (!alreadyReminded)
            {
                context.WarrantyReminders.Add(new WarrantyReminder
                {
                    InventoryItemId = item.Id,

                    // Simple human-readable message (future use: email/SMS notifications)
                    Message = $"Warranty for '{item.Name}' expires on {item.WarrantyExpiry:yyyy-MM-dd}.",

                    CreatedAt = DateTime.UtcNow,
                    IsProcessed = false
                });
            }
        }

        // Persist all newly created reminders
        await context.SaveChangesAsync(cancellationToken);
    }
}