using HotChocolate;
using Inventory.Api.Dtos;
using Inventory.Core.Entities;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.GraphQL;

public class Mutation
{
    public async Task<Room> CreateRoom(CreateRoomRequest input, [Service] InventoryDbContext db)
    {
        var room = new Room { Name = input.Name };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    public async Task<InventoryItem> CreateItem(CreateInventoryItemRequest input, [Service] InventoryDbContext db)
    {
        var item = new InventoryItem
        {
            Name = input.Name,
            PurchasePrice = input.PurchasePrice,
            PurchaseDate = input.PurchaseDate,
            WarrantyExpiry = input.WarrantyExpiry,
            SerialNumber = input.SerialNumber,
            Notes = input.Notes,
            RoomId = input.RoomId
        };
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        await db.Entry(item).Reference(i => i.Room).LoadAsync();
        return item;
    }
}
