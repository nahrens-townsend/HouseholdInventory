namespace Inventory.Core.Entities;

public class WarrantyReminder
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsProcessed { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;
}
