using HotChocolate;
using HotChocolate.Data;
using Inventory.Core.Entities;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.GraphQL;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Room> GetRooms([Service] InventoryDbContext db)
        => db.Rooms;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<InventoryItem> GetItems([Service] InventoryDbContext db)
        => db.InventoryItems;

    public async Task<Room?> GetRoomById(int id, [Service] InventoryDbContext db)
        => await db.Rooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<InventoryItem?> GetItemById(int id, [Service] InventoryDbContext db)
        => await db.InventoryItems
            .Include(i => i.Room)
            .FirstOrDefaultAsync(i => i.Id == id);
}
