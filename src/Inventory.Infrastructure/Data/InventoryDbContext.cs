using Microsoft.EntityFrameworkCore;
using Inventory.Core.Entities;

namespace Inventory.Infrastructure.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<WarrantyReminder> WarrantyReminders => Set<WarrantyReminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Items)
            .WithOne(i => i.Room)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WarrantyReminder>()
            .HasOne(wr => wr.InventoryItem)
            .WithMany(i => i.WarrantyReminders)
            .HasForeignKey(wr => wr.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}