using System.Text;
using System.Text.Json;
using FluentAssertions;
using Inventory.Api.Tests.Infrastructure;
using Xunit;

namespace Inventory.Api.Tests.GraphQL;

public class GraphQlItemsTests : IClassFixture<InventoryWebAppFactory>
{
    private readonly HttpClient _client;

    public GraphQlItemsTests(InventoryWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Items_Query_ReturnsOkResult()
    {
        var data = await SendGraphQl("{ items { id name } }");

        data.TryGetProperty("items", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateItem_Mutation_ReturnsCreatedItem()
    {
        var roomId = await CreateRoomId();
        var itemName = $"GQL-Item-{Guid.NewGuid()}";

        var data = await SendGraphQl($$"""
            mutation {
                createItem(input: {
                    name: "{{itemName}}"
                    purchasePrice: 299.99
                    purchaseDate: "2024-03-01T00:00:00Z"
                    warrantyExpiry: "2025-03-01T00:00:00Z"
                    roomId: {{roomId}}
                }) { id name purchasePrice roomId }
            }
            """);

        var item = data.GetProperty("createItem");
        item.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        item.GetProperty("name").GetString().Should().Be(itemName);
        item.GetProperty("roomId").GetInt32().Should().Be(roomId);
    }

    [Fact]
    public async Task Items_Query_ContainsCreatedItem()
    {
        var roomId = await CreateRoomId();
        var itemName = $"GQL-Item-{Guid.NewGuid()}";

        await SendGraphQl($$"""
            mutation {
                createItem(input: {
                    name: "{{itemName}}"
                    purchasePrice: 99.99
                    purchaseDate: "2024-01-01T00:00:00Z"
                    warrantyExpiry: "2025-01-01T00:00:00Z"
                    roomId: {{roomId}}
                }) { id }
            }
            """);

        var data = await SendGraphQl("{ items { id name } }");
        var names = data.GetProperty("items")
                        .EnumerateArray()
                        .Select(i => i.GetProperty("name").GetString())
                        .ToList();

        names.Should().Contain(itemName);
    }

    [Fact]
    public async Task UpdateItem_Mutation_UpdatesFields()
    {
        var roomId = await CreateRoomId();
        var createData = await SendGraphQl($$"""
            mutation {
                createItem(input: {
                    name: "GQL-Item-{{Guid.NewGuid()}}"
                    purchasePrice: 99.99
                    purchaseDate: "2024-01-01T00:00:00Z"
                    warrantyExpiry: "2025-01-01T00:00:00Z"
                    roomId: {{roomId}}
                }) { id }
            }
            """);
        var id = createData.GetProperty("createItem").GetProperty("id").GetInt32();
        var updatedName = $"GQL-Updated-{Guid.NewGuid()}";

        var updateData = await SendGraphQl($$"""
            mutation {
                updateItem(id: {{id}}, input: {
                    name: "{{updatedName}}"
                    purchasePrice: 149.99
                    purchaseDate: "2024-06-01T00:00:00Z"
                    warrantyExpiry: "2026-06-01T00:00:00Z"
                    roomId: {{roomId}}
                }) { id name purchasePrice }
            }
            """);

        var item = updateData.GetProperty("updateItem");
        item.GetProperty("id").GetInt32().Should().Be(id);
        item.GetProperty("name").GetString().Should().Be(updatedName);
    }

    [Fact]
    public async Task DeleteItem_Mutation_ReturnsTrue()
    {
        var roomId = await CreateRoomId();
        var createData = await SendGraphQl($$"""
            mutation {
                createItem(input: {
                    name: "GQL-ToDelete-{{Guid.NewGuid()}}"
                    purchasePrice: 0.0
                    purchaseDate: "2024-01-01T00:00:00Z"
                    warrantyExpiry: "2025-01-01T00:00:00Z"
                    roomId: {{roomId}}
                }) { id }
            }
            """);
        var id = createData.GetProperty("createItem").GetProperty("id").GetInt32();

        var deleteData = await SendGraphQl($$"""
            mutation { deleteItem(id: {{id}}) }
            """);

        deleteData.GetProperty("deleteItem").GetBoolean().Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<int> CreateRoomId()
    {
        var data = await SendGraphQl($$"""
            mutation { createRoom(input: { name: "Room-{{Guid.NewGuid()}}" }) { id } }
            """);
        return data.GetProperty("createRoom").GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Sends a GraphQL request and returns the <c>data</c> element.
    /// Throws <see cref="InvalidOperationException"/> if the response contains errors.
    /// </summary>
    private async Task<JsonElement> SendGraphQl(string query)
    {
        var body = new StringContent(
            JsonSerializer.Serialize(new { query }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/graphql", body);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        if (doc.RootElement.TryGetProperty("errors", out var errors))
            throw new InvalidOperationException($"GraphQL errors: {errors}");

        return doc.RootElement.GetProperty("data").Clone();
    }
}
