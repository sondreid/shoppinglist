using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;


const string allow_origins = "frontend_ports";



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShoppingItemDB>(opt => opt.UseInMemoryDatabase("ShoppingList"));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Handleliste API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allow_origins,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:4000", "http://localhost:4001", "http://frontend:4000","http://localhost:3000", "http://172.18.0.3:4000", "http://172.18.0.2:4000", "http://172.18.0.2:5058")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


var app = builder.Build();
//app.MapControllers();

app.UseCors(allow_origins);
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


app.MapGet("/shoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.ToListAsync());

    
app.MapPost("/shoppingitem", async (HttpContext context, ShoppingItemDB db) =>
{
    try 
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        using var jsonDocument = System.Text.Json.JsonDocument.Parse(body);

        if (!jsonDocument.RootElement.TryGetProperty("item", out var itemElement))
        {
            return Results.Json(new { success = false, message = "Missing 'item' property in payload." }, statusCode: 400);
        }

        var payloadBody = itemElement.GetString();

        if (string.IsNullOrWhiteSpace(payloadBody))
        {
            return Results.Json(new { success = false, message = "'item' cannot be blank." }, statusCode: 400);
        }

        var shoppingItem = new ShoppingItem
        {
            Name = payloadBody,
            IsComplete = false
        };
        db.ShoppingItems.Add(shoppingItem);
        await db.SaveChangesAsync();
        
        return Results.Json(new { success = true, message = $"Data stored successfully: {payloadBody}" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
});


app.MapPut("/shoppingitem/{id}", async (HttpContext context, int id, ShoppingItem updatedItem, ShoppingItemDB db) =>
{
    var item = await db.ShoppingItems.FindAsync(id);
    if (item == null) return Results.NotFound();
    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;
    await db.SaveChangesAsync();
    return Results.Json(new { success = true, message = $"Updated item  {id}" });

    
});

app.MapDelete("/item/{id}", async (int id, ShoppingItemDB db) =>
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

