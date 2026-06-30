using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Data;
using Inventory.Core.Entities;
using Inventory.Api.Models;

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
        var rooms = await _db.Rooms.ToListAsync();
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

        return Ok(room);
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetItems(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
            return NotFound();

        return Ok(room.Items);
    }
}
