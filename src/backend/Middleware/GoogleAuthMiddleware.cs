using handleliste.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace handleliste.Middleware;

public class GoogleAuthMiddleware
{
    private readonly RequestDelegate _next;

    public GoogleAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ShoppingItemDB db)
    {
        if (ShouldSkipAuth(context))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        var token = string.Empty;

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            token = authHeader.Substring("Bearer ".Length).Trim();
        }
        else if (context.Request.Query.ContainsKey("access_token"))
        {
            token = context.Request.Query["access_token"].ToString();
        }

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        if (session == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired session" });
            return;
        }

        context.Items["User"] = new UserInfo
        {
            Email = session.Email,
            Name = session.Name,
            Picture = session.Picture
        };
        await _next(context);
    }

    private static bool ShouldSkipAuth(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        return path == "/health" ||
               path == "/config" ||
               path == "/auth/google" ||
               path?.StartsWith("/swagger") == true ||
               path?.StartsWith("/itemhub") == true;
    }
}
