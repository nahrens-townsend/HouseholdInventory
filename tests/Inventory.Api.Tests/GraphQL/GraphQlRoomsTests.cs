using System.Text;
using System.Text.Json;
using FluentAssertions;
using Inventory.Api.Tests.Infrastructure;
using Xunit;

namespace Inventory.Api.Tests.GraphQL;

public class GraphQlRoomsTests : IClassFixture<InventoryWebAppFactory>
{
    private readonly HttpClient _client;

    public GraphQlRoomsTests(InventoryWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rooms_Query_ReturnsOkResult()
    {
        var data = await SendGraphQl("{ rooms { id name } }");

        data.TryGetProperty("rooms", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Rooms_Query_ContainsCreatedRoom()
    {
        var name = $"GQL-Room-{Guid.NewGuid()}";
        await SendGraphQl($$"""mutation { createRoom(input: { name: "{{name}}" }) { id } }""");

        var data = await SendGraphQl("{ rooms { id name } }");
        var names = data.GetProperty("rooms")
                        .EnumerateArray()
                        .Select(r => r.GetProperty("name").GetString())
                        .ToList();

        names.Should().Contain(name);
    }

    [Fact]
    public async Task CreateRoom_Mutation_ReturnsCreatedRoom()
    {
        var name = $"GQL-Room-{Guid.NewGuid()}";

        var data = await SendGraphQl($$"""
            mutation { createRoom(input: { name: "{{name}}" }) { id name } }
            """);

        var room = data.GetProperty("createRoom");
        room.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        room.GetProperty("name").GetString().Should().Be(name);
    }

    [Fact]
    public async Task UpdateRoom_Mutation_UpdatesName()
    {
        var createData = await SendGraphQl($$"""
            mutation { createRoom(input: { name: "GQL-Room-{{Guid.NewGuid()}}" }) { id } }
            """);
        var id = createData.GetProperty("createRoom").GetProperty("id").GetInt32();
        var updatedName = $"GQL-Updated-{Guid.NewGuid()}";

        var updateData = await SendGraphQl($$"""
            mutation { updateRoom(id: {{id}}, input: { name: "{{updatedName}}" }) { id name } }
            """);

        var room = updateData.GetProperty("updateRoom");
        room.GetProperty("id").GetInt32().Should().Be(id);
        room.GetProperty("name").GetString().Should().Be(updatedName);
    }

    [Fact]
    public async Task DeleteRoom_Mutation_ReturnsTrue()
    {
        var createData = await SendGraphQl($$"""
            mutation { createRoom(input: { name: "GQL-ToDelete-{{Guid.NewGuid()}}" }) { id } }
            """);
        var id = createData.GetProperty("createRoom").GetProperty("id").GetInt32();

        var deleteData = await SendGraphQl($$"""
            mutation { deleteRoom(id: {{id}}) }
            """);

        deleteData.GetProperty("deleteRoom").GetBoolean().Should().BeTrue();
    }

    // ── Helper ───────────────────────────────────────────────────────────────

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
