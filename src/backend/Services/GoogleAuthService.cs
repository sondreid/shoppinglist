using Google.Apis.Auth;
using handleliste.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace handleliste.Services;

public class GoogleAuthService
{
    private readonly string? _clientId;
    private readonly HashSet<string> _allowedEmails;
    private const string AllowedUsersPath = "allowed_users.json";

    public GoogleAuthService(IConfiguration configuration)
    {
        _clientId = GetClientId(configuration);
        _allowedEmails = LoadAllowedUsers();
    }

    public async Task<UserInfo?> VerifyTokenAsync(string idToken)
    {
        if (string.IsNullOrEmpty(_clientId))
        {
            throw new InvalidOperationException("Google Client ID not configured");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (!IsEmailAllowed(payload.Email))
            {
                return null;
            }

            return new UserInfo
            {
                Email = payload.Email,
                Name = payload.Name ?? payload.Email,
                Picture = payload.Picture
            };
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }

    private bool IsEmailAllowed(string email)
    {
        if (_allowedEmails.Count == 0)
            return true;

        return _allowedEmails.Contains(email.ToLowerInvariant());
    }

    private static HashSet<string> LoadAllowedUsers()
    {
        if (!File.Exists(AllowedUsersPath))
            return new HashSet<string>();

        try
        {
            var json = File.ReadAllText(AllowedUsersPath);
            var config = JsonSerializer.Deserialize<AllowedUsersConfig>(json);
            return config?.Emails?
                .Select(e => e.ToLowerInvariant())
                .ToHashSet() ?? new HashSet<string>();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private string? GetClientId(IConfiguration configuration)
    {
        var envClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        if (!string.IsNullOrEmpty(envClientId))
        {
            return envClientId;
        }

        var clientSecretPath = configuration["GoogleClientSecretPath"] ?? "client_secret.json";
        
        if (!File.Exists(clientSecretPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(clientSecretPath);
            var clientSecret = System.Text.Json.JsonSerializer.Deserialize<GoogleClientSecret>(json);
            return clientSecret?.Web?.ClientId;
        }
        catch
        {
            return null;
        }
    }
}
