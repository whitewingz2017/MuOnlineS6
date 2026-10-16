#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Main.Networking.Services;

public sealed class RegistrationService
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };

    private readonly HttpClient _client;
    public RegistrationService(HttpClient? client = null) => _client=client??Client;

    public static string? Validate(string username, string email, string password, string confirmation)
    {
        if(username.Length is <3 or >10 || username.Any(c=>c<'!' || c>'~'))
            return "Username must be 3–10 ASCII characters without spaces.";
        if(string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email,out var address) || address.Address!=email)
            return "Enter a valid email address.";
        if(password.Length is <3 or >20 || password.Any(c=>c<' ' || c>'~'))
            return "Password must be 3–20 ASCII characters.";
        if(password!=confirmation)return "Passwords do not match.";
        return null;
    }

    public async Task<(bool Success,string Message)> RegisterAsync(string url,string username,string email,string password,CancellationToken cancellationToken)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var endpoint) || endpoint.Scheme!=Uri.UriSchemeHttps || !string.IsNullOrEmpty(endpoint.UserInfo))
            return (false,"Registration is not configured. Set RegistrationUrl to the server’s HTTPS registration endpoint.");
        try
        {
            using var response=await _client.PostAsJsonAsync(endpoint,new { LoginName=username, EMail=email, Password=password },cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.Created => (true,"Account created. You can now log in."),
                HttpStatusCode.Conflict => (false,"That username is already registered."),
                HttpStatusCode.BadRequest => (false,"Check the username, email and password requirements."),
                HttpStatusCode.TooManyRequests => (false,"Too many registration attempts. Please try again later."),
                HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => (false,"Test registration is not enabled on this server."),
                _ => (false,"Registration failed. Please try again later.")
            };
        }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested)
        { return (false,"Registration timed out. You can retry without leaving this form."); }
        catch(HttpRequestException)
        { return (false,"Unable to reach registration. Check your connection and try again."); }
    }
}
