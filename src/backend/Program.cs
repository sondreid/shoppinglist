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

var ssoOverride = string.Equals(
    Environment.GetEnvironmentVariable("SSO_OVERRIDE"),
    "true", StringComparison.OrdinalIgnoreCase);

var devMode = string.Equals(
    Environment.GetEnvironmentVariable("DEV_MODE"),
    "true", StringComparison.OrdinalIgnoreCase);


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShoppingItemDB>(opt => opt.UseSqlite("Data Source=data/shoppinglist.db"));


builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo{ Title = "Shoppingliste API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "opaque",
        In = ParameterLocation.Header,
        Description = "Paste the sessionToken from POST /auth/dev-login (or /auth/google)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<GoogleAuthService>();

const string allowOrigins = "frontend_ports";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:4000", "http://localhost:4001", "http://frontend:4000",
                    "http://localhost:3000", "http://localhost:5058")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


var app = builder.Build();

if (ssoOverride && !devMode)
{
    app.Logger.LogCritical("SSO_OVERRIDE=true requires DEV_MODE=true. Exiting.");
    Environment.Exit(1);
}
if (ssoOverride)
{
    app.Logger.LogWarning("SSO_OVERRIDE=true — authentication bypassed. DEV ONLY.");
}


using (var scope = app.Services.CreateScope())
{
    Directory.CreateDirectory("data");
    var db = scope.ServiceProvider.GetRequiredService<ShoppingItemDB>();
    db.Database.EnsureCreated();
    // EnsureCreated does nothing when the database already exists, so create newer tables explicitly
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "DinnerPlans" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_DinnerPlans" PRIMARY KEY AUTOINCREMENT,
            "Date" TEXT NOT NULL,
            "Recipe" TEXT NULL,
            "UpdatedAt" TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS "DinnerIngredients" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_DinnerIngredients" PRIMARY KEY AUTOINCREMENT,
            "DinnerPlanId" INTEGER NOT NULL,
            "ShoppingItemId" INTEGER NOT NULL,
            "Name" TEXT NULL
        );
        """);
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

if (ssoOverride)
{
    app.MapPost("/auth/dev-login", async (ShoppingItemDB db) =>
    {
        var sessionToken = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var user = new UserInfo { Email = "dev@local", Name = "Dev User" };
        db.Sessions.Add(new handleliste.Models.Session
        {
            Token = sessionToken,
            Email = user.Email,
            Name = user.Name,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();
        return Results.Json(new AuthResponse { User = user, SessionToken = sessionToken });
    });
}


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





app.MapGet("/completedshoppingitems", async (ShoppingItemDB db, int? skip, int? take) =>
{
    var s = Math.Max(0, skip ?? 0);
    var t = Math.Clamp(take ?? 100, 1, 200);
    var query = db.ShoppingItems.Where(item => item.IsComplete);
    var total = await query.CountAsync();
    var items = await query
        .Include(item => item.Image)
        .OrderByDescending(item => item.UpdatedAt)
        .Skip(s)
        .Take(t)
        .ToListAsync();
    return Results.Json(new { items, total, hasMore = s + items.Count < total });
});

app.MapGet("/uncompletedshoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.Include(item => item.Image)
        .Where(item => !item.IsComplete)
        .OrderBy(item => item.UpdatedAt)
        .ToListAsync());

app.MapGet("/shoppingitems", async (ShoppingItemDB db) =>
    await db.ShoppingItems.Include(item => item.Image).OrderBy(item => item.UpdatedAt).ToListAsync());

app.MapPost("/shoppingitem", async (ShoppingItem item, ShoppingItemDB db, IHubContext<ItemHub> hubContext) =>
{
    if (item.Equals(null)) return Results.NotFound();
    
    if (!item.IsImage && string.IsNullOrEmpty(item.Name)) 
        return Results.Json(new { success = false, message = "Missing 'name' property in payload." }, statusCode: 400);
    

    if (item.IsImage && (item.Image == null || string.IsNullOrEmpty(item.Image.ContentType)))
        return Results.Json(new { success = false, message = "Missing image data for image item." }, statusCode: 400);

    item.Quantity = Math.Max(1, item.Quantity);
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
        item.Quantity = Math.Max(1, updatedItem.Quantity);
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("ItemUpdated", item);
        return Results.Json(new { success = true, message = $"Updated item  {id}" });
    });

app.MapDelete("/shoppingitem/{id}", async (int id, ShoppingItemDB db, IHubContext<ItemHub> hubContext) =>
{
    var item = await db.ShoppingItems.FindAsync(id);
    if (item is null) return Results.NotFound();

    db.ShoppingItems.Remove(item);
    await db.SaveChangesAsync();
    await hubContext.Clients.All.SendAsync("ItemDeleted", id);
    return Results.Json(new { success = true, message = $"Deleted item  {id}" });
});

app.MapGet("/dinnerplans", async (ShoppingItemDB db, string from, string to) =>
{
    var plans = await db.DinnerPlans
        .Where(plan => plan.Date.CompareTo(from) >= 0 && plan.Date.CompareTo(to) <= 0)
        .OrderBy(plan => plan.Date)
        .ToListAsync();
    var planIds = plans.Select(plan => plan.Id).ToList();
    var ingredients = await db.DinnerIngredients
        .Where(ingredient => planIds.Contains(ingredient.DinnerPlanId))
        .ToListAsync();
    return Results.Json(plans.Select(plan => new
    {
        plan.Id,
        plan.Date,
        plan.Recipe,
        Ingredients = ingredients.Where(ingredient => ingredient.DinnerPlanId == plan.Id)
    }));
});

app.MapPost("/dinnerplan", async (DinnerPlan plan, ShoppingItemDB db) =>
{
    if (string.IsNullOrEmpty(plan.Date))
        return Results.Json(new { success = false, message = "Missing 'date' property in payload." }, statusCode: 400);

    var existing = await db.DinnerPlans.FirstOrDefaultAsync(p => p.Date == plan.Date);
    if (existing == null)
    {
        existing = new DinnerPlan { Date = plan.Date };
        db.DinnerPlans.Add(existing);
    }
    existing.Recipe = plan.Recipe;
    existing.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Json(new { success = true, plan = existing });
});

app.MapPost("/dinnerplan/{id}/ingredient",
    async (int id, DinnerIngredient ingredient, ShoppingItemDB db, IHubContext<ItemHub> hubContext) =>
    {
        var plan = await db.DinnerPlans.FindAsync(id);
        if (plan == null) return Results.NotFound();
        if (string.IsNullOrEmpty(ingredient.Name))
            return Results.Json(new { success = false, message = "Missing 'name' property in payload." }, statusCode: 400);

        var item = new ShoppingItem { Name = ingredient.Name, IsComplete = false, UpdatedAt = DateTime.UtcNow };
        db.ShoppingItems.Add(item);
        await db.SaveChangesAsync();

        ingredient.DinnerPlanId = id;
        ingredient.ShoppingItemId = item.Id;
        db.DinnerIngredients.Add(ingredient);
        await db.SaveChangesAsync();

        await hubContext.Clients.All.SendAsync("ItemCreated", item);
        return Results.Json(new { success = true, ingredient });
    });

app.MapDelete("/dinneringredient/{id}", async (int id, ShoppingItemDB db, IHubContext<ItemHub> hubContext) =>
{
    var ingredient = await db.DinnerIngredients.FindAsync(id);
    if (ingredient is null) return Results.NotFound();

    // ExecuteDelete tolerates the row already being gone (e.g. duplicate requests)
    await db.DinnerIngredients.Where(i => i.Id == id).ExecuteDeleteAsync();
    // Remove the linked shopping item too, unless it was already bought
    var removed = await db.ShoppingItems
        .Where(item => item.Id == ingredient.ShoppingItemId && !item.IsComplete)
        .ExecuteDeleteAsync();
    if (removed > 0)
        await hubContext.Clients.All.SendAsync("ItemDeleted", ingredient.ShoppingItemId);
    return Results.Json(new { success = true, message = $"Deleted ingredient  {id}" });
});

app.MapGet("/auth/whoami", (HttpContext ctx) =>
{
    var user = ctx.Items["User"] as UserInfo;
    return user is null ? Results.Unauthorized() : Results.Json(user);
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



if (devMode)
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Access at /swagger
}
app.Run(devMode ? "http://localhost:5058" : "http://0.0.0.0:5058");