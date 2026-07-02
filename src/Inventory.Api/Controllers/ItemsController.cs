using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Data;
using Inventory.Core.Entities;
using Inventory.Api.Dtos;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public ItemsController(InventoryDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.InventoryItems
            .Select(i => new InventoryItemResponse
            {
                Id = i.Id,
                Name = i.Name,
                PurchasePrice = i.PurchasePrice,
                PurchaseDate = i.PurchaseDate,
                WarrantyExpiry = i.WarrantyExpiry,
                SerialNumber = i.SerialNumber,
                Notes = i.Notes,
                RoomId = i.RoomId
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null)
            return NotFound();

        return Ok(new InventoryItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            PurchasePrice = item.PurchasePrice,
            PurchaseDate = item.PurchaseDate,
            WarrantyExpiry = item.WarrantyExpiry,
            SerialNumber = item.SerialNumber,
            Notes = item.Notes,
            RoomId = item.RoomId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInventoryItemRequest request)
    {
        var item = new InventoryItem
        {
            Name = request.Name,
            PurchasePrice = request.PurchasePrice,
            PurchaseDate = request.PurchaseDate,
            WarrantyExpiry = request.WarrantyExpiry,
            SerialNumber = request.SerialNumber,
            Notes = request.Notes,
            RoomId = request.RoomId
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new InventoryItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            PurchasePrice = item.PurchasePrice,
            PurchaseDate = item.PurchaseDate,
            WarrantyExpiry = item.WarrantyExpiry,
            SerialNumber = item.SerialNumber,
            Notes = item.Notes,
            RoomId = item.RoomId
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateInventoryItemRequest request)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null)
            return NotFound();

        item.Name = request.Name;
        item.PurchasePrice = request.PurchasePrice;
        item.PurchaseDate = request.PurchaseDate;
        item.WarrantyExpiry = request.WarrantyExpiry;
        item.SerialNumber = request.SerialNumber;
        item.Notes = request.Notes;
        item.RoomId = request.RoomId;

        await _db.SaveChangesAsync();

        return Ok(new InventoryItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            PurchasePrice = item.PurchasePrice,
            PurchaseDate = item.PurchaseDate,
            WarrantyExpiry = item.WarrantyExpiry,
            SerialNumber = item.SerialNumber,
            Notes = item.Notes,
            RoomId = item.RoomId
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null)
            return NotFound();

        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}