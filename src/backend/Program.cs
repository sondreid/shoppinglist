using handleliste.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;


const string allow_origins = "frontend_ports";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShoppingItemDB>(opt => opt.UseInMemoryDatabase("ShoppingList"));


builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shoppingliste API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allow_origins,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:4000", "http://localhost:4001", "http://frontend:4000","http://localhost:3000", "http://172.18.0.3:4000", "http://172.18.0.2:4000", "http://172.18.0.2:5058")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});


var app = builder.Build();


app.UseCors(allow_origins);

app.UseAuthorization();

app.MapHub<ItemHub>("/itemhub");




app.MapGet("/helloworld", () => new { payload = "hello world" });
app.MapPost("/exampleitem", async (ShoppingItemDB db) =>
{
    var existingItem = await db.ShoppingItems.FirstOrDefaultAsync(x => x.Id == 1);
    if (existingItem != null)
    {
        return Results.Conflict("An item with this ID already exists");
    }

    var exampleItem = new ShoppingItem 
    { 
        Id = 1,
        Name = "Example Item",
        IsComplete = false 
    };
    db.ShoppingItems.Add(exampleItem);
    await db.SaveChangesAsync();
    return Results.Created($"/shoppingitems/{exampleItem.Id}", exampleItem);
});


app.MapGet("/activeshoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.Where(item => item.IsComplete).ToListAsync());

app.MapGet("/shoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.ToListAsync());

app.MapPost("/shoppingitem", async (ShoppingItem item, ShoppingItemDB db,  IHubContext<ItemHub> hubContext) => 
{
		
    if (item.Equals(null)) return Results.NotFound();
    if (item.Name == null) return Results.Json(new { success = false, message = "Missing 'item' property in payload." }, statusCode: 400);
    
    db.ShoppingItems.Add(item);
    await db.SaveChangesAsync();

    await hubContext.Clients.All.SendAsync("ItemCreated", item);
    return Results.Json(new { success = true, message = $"Data stored successfully: {item}" });


});


app.MapPut("/shoppingitem/{id}", async (IHubContext<ItemHub> hubContext, int id, ShoppingItem updatedItem, ShoppingItemDB db) =>
{
    var item = await db.ShoppingItems.FindAsync(id);
    if (item == null) return Results.NotFound();
    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;
    await db.SaveChangesAsync();
    await hubContext.Clients.All.SendAsync("ItemUpdated", item);
    return Results.Json(new { success = true, message = $"Updated item  {id}" });

    
});

app.MapDelete("/shoppingitem/{id}", async (int id, ShoppingItemDB db) =>
{
    var item = await db.ShoppingItems.FindAsync(id);
    if (item is null) return Results.NotFound();

    db.ShoppingItems.Remove(item);
    await db.SaveChangesAsync();
    return Results.Json(new { success = true, message = $"Deleted item  {id}" });
});



app.Use(async (context, next) =>    
{
    if(context.Request.Path.Value == "/favicon.ico")
    {
        // Favicon request, return 404
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    
    // No favicon, call next middleware
    await next.Invoke();
});

app.MapGet("/health", () => new { payload = "A-okay" });


if ( Environment.GetEnvironmentVariable("dev") != "true")
{
    app.Run("http://0.0.0.0:5058");
}

else
{
    app.Run("http://localhost:5058");
}

