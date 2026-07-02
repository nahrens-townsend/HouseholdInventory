using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Inventory.Api.Dtos;
using Inventory.Api.Tests.Infrastructure;
using Inventory.Core.Entities;
using Xunit;

namespace Inventory.Api.Tests.Controllers;

public class RoomsControllerTests : IClassFixture<InventoryWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly InventoryWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RoomsControllerTests(InventoryWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRooms_ReturnsOk()
    {
        var response = await _client.GetAsync("/rooms");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostRoom_ReturnsCreatedRoom()
    {
        var name = $"Room-{Guid.NewGuid()}";

        var response = await _client.PostAsync("/rooms", JsonBody(new { name }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var room = await Deserialize<RoomResponse>(response);
        room.Id.Should().BeGreaterThan(0);
        room.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetRooms_IncludesNewlyCreatedRoom()
    {
        var name = $"Room-{Guid.NewGuid()}";
        await CreateRoom(name);

        var response = await _client.GetAsync("/rooms");
        var rooms = await Deserialize<List<RoomResponse>>(response);

        rooms.Should().Contain(r => r.Name == name);
    }

    [Fact]
    public async Task PutRoom_UpdatesRoomName()
    {
        var room = await CreateRoom($"Original-{Guid.NewGuid()}");
        var updatedName = $"Updated-{Guid.NewGuid()}";

        var response = await _client.PutAsync(
            $"/rooms/{room.Id}",
            JsonBody(new { name = updatedName }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await Deserialize<RoomResponse>(response);
        updated.Id.Should().Be(room.Id);
        updated.Name.Should().Be(updatedName);
    }

    [Fact]
    public async Task DeleteRoom_ReturnsNoContent()
    {
        var room = await CreateRoom($"ToDelete-{Guid.NewGuid()}");

        var response = await _client.DeleteAsync($"/rooms/{room.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetRoomItems_ReturnsItemsForRoom()
    {
        // Seed directly so this test is independent of ItemsController
        int roomId = 0;
        string itemName = $"Item-{Guid.NewGuid()}";
        _factory.UseDbContext(db =>
        {
            var room = new Room { Name = $"RoomWithItem-{Guid.NewGuid()}" };
            db.Rooms.Add(room);
            db.SaveChanges();
            db.InventoryItems.Add(new InventoryItem
            {
                Name = itemName,
                PurchasePrice = 99.99m,
                PurchaseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                WarrantyExpiry = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                RoomId = room.Id
            });
            db.SaveChanges();
            roomId = room.Id;
        });

        var response = await _client.GetAsync($"/rooms/{roomId}/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await Deserialize<List<InventoryItemResponse>>(response);
        items.Should().Contain(i => i.Name == itemName && i.RoomId == roomId);
    }

    [Fact]
    public async Task PutRoom_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        var response = await _client.PutAsync(
            "/rooms/99999",
            JsonBody(new { name = "Ghost" }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRoom_ReturnsNotFound_WhenRoomDoesNotExist()
    {
        var response = await _client.DeleteAsync("/rooms/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<RoomResponse> CreateRoom(string name)
    {
        var response = await _client.PostAsync("/rooms", JsonBody(new { name }));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await Deserialize<RoomResponse>(response))!;
    }

    private static StringContent JsonBody(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }
}
