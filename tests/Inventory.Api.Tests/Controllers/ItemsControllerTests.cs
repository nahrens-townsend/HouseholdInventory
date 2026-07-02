using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Inventory.Api.Dtos;
using Inventory.Api.Tests.Infrastructure;
using Xunit;

namespace Inventory.Api.Tests.Controllers;

public class ItemsControllerTests : IClassFixture<InventoryWebAppFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ItemsControllerTests(InventoryWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_ReturnsOk()
    {
        var response = await _client.GetAsync("/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostItem_ReturnsCreatedItem()
    {
        var room = await CreateRoom();
        var itemName = $"Item-{Guid.NewGuid()}";

        var response = await _client.PostAsync("/items", JsonBody(new
        {
            name = itemName,
            purchasePrice = 299.99,
            purchaseDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiry = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            roomId = room.Id
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await Deserialize<InventoryItemResponse>(response);
        item.Id.Should().BeGreaterThan(0);
        item.Name.Should().Be(itemName);
        item.PurchasePrice.Should().Be(299.99m);
        item.RoomId.Should().Be(room.Id);
    }

    [Fact]
    public async Task GetItems_IncludesNewlyCreatedItem()
    {
        var room = await CreateRoom();
        var created = await CreateItem(room.Id);

        var response = await _client.GetAsync("/items");
        var items = await Deserialize<List<InventoryItemResponse>>(response);

        items.Should().Contain(i => i.Id == created.Id);
    }

    [Fact]
    public async Task GetItemById_ReturnsItem()
    {
        var room = await CreateRoom();
        var created = await CreateItem(room.Id);

        var response = await _client.GetAsync($"/items/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await Deserialize<InventoryItemResponse>(response);
        item.Id.Should().Be(created.Id);
        item.Name.Should().Be(created.Name);
    }

    [Fact]
    public async Task PutItem_UpdatesItem()
    {
        var room = await CreateRoom();
        var created = await CreateItem(room.Id, "Original Name");
        var newName = $"Updated-{Guid.NewGuid()}";

        var response = await _client.PutAsync(
            $"/items/{created.Id}",
            JsonBody(new
            {
                name = newName,
                purchasePrice = 199.99,
                purchaseDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                warrantyExpiry = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                roomId = room.Id
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await Deserialize<InventoryItemResponse>(response);
        updated.Id.Should().Be(created.Id);
        updated.Name.Should().Be(newName);
        updated.PurchasePrice.Should().Be(199.99m);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNoContent()
    {
        var room = await CreateRoom();
        var item = await CreateItem(room.Id);

        var response = await _client.DeleteAsync($"/items/{item.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetItemById_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var response = await _client.GetAsync("/items/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutItem_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var room = await CreateRoom();

        var response = await _client.PutAsync(
            "/items/99999",
            JsonBody(new
            {
                name = "Ghost Item",
                purchasePrice = 0.0,
                purchaseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                warrantyExpiry = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                roomId = room.Id
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var response = await _client.DeleteAsync("/items/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<RoomResponse> CreateRoom(string name = "Test Room")
    {
        var response = await _client.PostAsync("/rooms", JsonBody(new { name }));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RoomResponse>(content, JsonOptions)!;
    }

    private async Task<InventoryItemResponse> CreateItem(int roomId, string name = "Test Item")
    {
        var response = await _client.PostAsync("/items", JsonBody(new
        {
            name,
            purchasePrice = 99.99,
            purchaseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiry = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            roomId
        }));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<InventoryItemResponse>(content, JsonOptions)!;
    }

    private static StringContent JsonBody(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }
}
