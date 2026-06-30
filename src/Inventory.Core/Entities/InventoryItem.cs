namespace Inventory.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class InventoryItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    public DateTime PurchaseDate { get; set; }

    public DateTime WarrantyExpiry { get; set; }

    public string? SerialNumber { get; set; }

    public string? Notes { get; set; }

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;
}