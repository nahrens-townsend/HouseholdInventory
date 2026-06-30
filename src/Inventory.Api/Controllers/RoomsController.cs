using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Data;
using Inventory.Core.Entities;
using Inventory.Api.Dtos;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("rooms")]
public class RoomsController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public RoomsController(InventoryDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rooms = await _db.Rooms
            .Select(r => new RoomResponse { Id = r.Id, Name = r.Name })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomRequest request)
    {
        var room = new Room
        {
            Name = request.Name
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return Ok(new RoomResponse { Id = room.Id, Name = room.Name });
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetItems(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
            return NotFound();

        var items = room.Items.Select(i => new InventoryItemResponse
        {
            Id = i.Id,
            Name = i.Name,
            PurchasePrice = i.PurchasePrice,
            PurchaseDate = i.PurchaseDate,
            WarrantyExpiry = i.WarrantyExpiry,
            SerialNumber = i.SerialNumber,
            Notes = i.Notes,
            RoomId = i.RoomId
        });

        return Ok(items);
    }
}
