namespace Inventory.Api.Dtos;

public class UpdateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime WarrantyExpiry { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
    public int RoomId { get; set; }
}
