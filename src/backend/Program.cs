using handleliste;
using handleliste.Hubs;
using handleliste.Middleware;
using handleliste.Models;
using handleliste.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShoppingItemDB>(opt => opt.UseSqlite("Data Source=data/shoppinglist.db"));


builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo{ Title = "Shoppingliste API", Version = "v1" });
});

builder.Services.AddSingleton<GoogleAuthService>();

const string allowOrigins = "frontend_ports";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:4000", "http://localhost:4001", "http://frontend:4000",
                    "http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    Directory.CreateDirectory("data");
    var db = scope.ServiceProvider.GetRequiredService<ShoppingItemDB>();
    db.Database.EnsureCreated();
}

app.UseCors(allowOrigins);

app.UseMiddleware<GoogleAuthMiddleware>();

app.UseAuthorization();

app.MapHub<ItemHub>("/itemhub");

app.MapGet("/config", () =>
{
    var response = new ConfigResponse();

    var envClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    if (!string.IsNullOrEmpty(envClientId))
    {
        response.GoogleClientId = envClientId;
    }
    else
    {
        var clientSecretPath = "client_secret.json";
        if (File.Exists(clientSecretPath))
        {
            try
            {
                var json = File.ReadAllText(clientSecretPath);
                var clientSecret = JsonSerializer.Deserialize<GoogleClientSecret>(json);
                response.GoogleClientId = clientSecret?.Web?.ClientId;
            }
            catch
            {
            }
        }
    }

    return Results.Json(response);
});

app.MapPost("/auth/google", async (AuthRequest request, GoogleAuthService authService, ShoppingItemDB db) =>
{
    if (string.IsNullOrEmpty(request.Token))
    {
        return Results.BadRequest(new { error = "Token is required" });
    }

    var user = await authService.VerifyTokenAsync(request.Token);

    if (user == null)
    {
        return Results.Unauthorized();
    }

    var sessionToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    db.Sessions.Add(new handleliste.Models.Session
    {
        Token = sessionToken,
        Email = user.Email,
        Name = user.Name,
        Picture = user.Picture,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    });
    await db.SaveChangesAsync();

    return Results.Json(new AuthResponse { User = user, SessionToken = sessionToken });
});


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





app.MapGet("/completedshoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.Include(item => item.Image).Where(item => item.IsComplete).ToListAsync());

app.MapGet("/uncompletedshoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.Include(item => item.Image).Where(item => !item.IsComplete).ToListAsync());

app.MapGet("/shoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.Include(item => item.Image).OrderBy(item => item.UpdatedAt).ToListAsync());

app.MapPost("/shoppingitem", async (ShoppingItem item, ShoppingItemDB db, IHubContext<ItemHub> hubContext) =>
{
    if (item.Equals(null)) return Results.NotFound();
    
    if (!item.IsImage && string.IsNullOrEmpty(item.Name)) 
        return Results.Json(new { success = false, message = "Missing 'name' property in payload." }, statusCode: 400);
    

    if (item.IsImage && (item.Image == null || string.IsNullOrEmpty(item.Image.ContentType)))
        return Results.Json(new { success = false, message = "Missing image data for image item." }, statusCode: 400);

    item.UpdatedAt = DateTime.UtcNow;
    db.ShoppingItems.Add(item);
    await db.SaveChangesAsync();

    await hubContext.Clients.All.SendAsync("ItemCreated", item);
    return Results.Json(new { success = true, item=item,  message = $"Data stored successfully: {item}" });
});


app.MapPut("/shoppingitem/{id}",
    async (IHubContext<ItemHub> hubContext, int id, ShoppingItem updatedItem, ShoppingItemDB db) =>
    {
        var item = await db.ShoppingItems.FindAsync(id);
        if (item == null) return Results.NotFound();
        item.Name = updatedItem.Name;
        item.IsComplete = updatedItem.IsComplete;
        item.UpdatedAt = DateTime.UtcNow;
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
    if (context.Request.Path.Value == "/favicon.ico")
    {
        // Favicon request, return 404
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    // No favicon, call next middleware
    await next.Invoke();
});

app.MapGet("/health", () => new { payload = "A-okay" });


var devMode = Environment.GetEnvironmentVariable("dev_mode")?.ToLower() == "true";
app.Run(devMode ? "http://localhost:5058" : "http://0.0.0.0:5058");