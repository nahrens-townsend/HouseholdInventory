using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Data;
using Inventory.Core.Entities;
using Inventory.Api.Models;

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
        var items = await _db.InventoryItems.ToListAsync();
        return Ok(items);
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
            Notes = request.Notes
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(item);
    }
}