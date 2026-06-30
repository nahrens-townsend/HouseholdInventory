using Microsoft.EntityFrameworkCore;
using Inventory.Core.Entities;

namespace Inventory.Infrastructure.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
}