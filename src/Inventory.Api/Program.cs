using Inventory.Api.GraphQL;
using Inventory.Api.Services;
using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Controllers
builder.Services.AddControllers();

// DbContext — connection string supplied via appsettings or App Service Configuration
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

// Background service — warranty expiry reminders
builder.Services.AddHostedService<WarrantyReminderService>();

var app = builder.Build();

// Required when running behind Azure App Service's reverse proxy so that
// UseHttpsRedirection reads the correct scheme and host from forwarded headers.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Swagger UI (all environments)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGraphQL();

app.MapControllers();

app.Run();

// Exposes the Program class to the test assembly so WebApplicationFactory<Program> can reference it.
public partial class Program { }
