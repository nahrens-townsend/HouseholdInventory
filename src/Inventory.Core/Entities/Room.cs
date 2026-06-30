namespace Inventory.Core.Entities;

public class Room
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<InventoryItem> Items { get; set; } = [];
}
