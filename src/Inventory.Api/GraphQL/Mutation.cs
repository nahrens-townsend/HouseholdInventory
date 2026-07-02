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

    public async Task<Room?> UpdateRoom(int id, UpdateRoomRequest input, [Service] InventoryDbContext db)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null)
            return null;

        room.Name = input.Name;
        await db.SaveChangesAsync();
        return room;
    }

    public async Task<bool> DeleteRoom(int id, [Service] InventoryDbContext db)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null)
            return false;

        db.Rooms.Remove(room);
        await db.SaveChangesAsync();
        return true;
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

    public async Task<InventoryItem?> UpdateItem(int id, UpdateInventoryItemRequest input, [Service] InventoryDbContext db)
    {
        var item = await db.InventoryItems.FindAsync(id);
        if (item is null)
            return null;

        item.Name = input.Name;
        item.PurchasePrice = input.PurchasePrice;
        item.PurchaseDate = input.PurchaseDate;
        item.WarrantyExpiry = input.WarrantyExpiry;
        item.SerialNumber = input.SerialNumber;
        item.Notes = input.Notes;
        item.RoomId = input.RoomId;

        await db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteItem(int id, [Service] InventoryDbContext db)
    {
        var item = await db.InventoryItems.FindAsync(id);
        if (item is null)
            return false;

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
