using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClaudeBridgeController.Models;

namespace ClaudeBridgeController.Services;

public interface IApiService
{
    Task<List<Session>> GetSessionsAsync();
    Task<bool> SendClipboardToSessionAsync(Guid sessionId, string content);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _baseUrl;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _baseUrl = "http://localhost:3000/api"; // Default URL, can be configured
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<Session>> GetSessionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/sessions");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SessionsResponse>(json, _jsonOptions);
            
            return result?.Sessions ?? new List<Session>();
        }
        catch (HttpRequestException ex)
        {
            // Log error
            Console.WriteLine($"Error fetching sessions: {ex.Message}");
            return new List<Session>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return new List<Session>();
        }
    }

    public async Task<bool> SendClipboardToSessionAsync(Guid sessionId, string content)
    {
        try
        {
            var payload = new
            {
                sessionId = sessionId,
                message = content,
                source = "ClaudeBridgeController"
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/sessions/{sessionId}/messages", httpContent);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending clipboard content: {ex.Message}");
            return false;
        }
    }
}