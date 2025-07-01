using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenAPI_MCP_Proxy.Models;

namespace OpenAPI_MCP_Proxy.Services;

public class CacheService
{
    private readonly string _cacheDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService()
    {
        _cacheDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task SaveToolsCacheAsync(string openApiUrl, List<Tool> tools)
    {
        var cacheFileName = GenerateCacheFileName(openApiUrl);
        var cacheFilePath = Path.Combine(_cacheDirectory, cacheFileName);

        var cacheData = new
        {
            tools,
            cachedAt = DateTime.UtcNow,
            openApiUrl
        };

        var json = JsonSerializer.Serialize(cacheData, _jsonOptions);
        await File.WriteAllTextAsync(cacheFilePath, json);
        
        Console.Error.WriteLine($"[INFO] Tools cached successfully: {cacheFileName}");
    }

    public async Task<List<Tool>?> LoadToolsCacheAsync(string openApiUrl)
    {
        var cacheFileName = GenerateCacheFileName(openApiUrl);
        var cacheFilePath = Path.Combine(_cacheDirectory, cacheFileName);

        if (!File.Exists(cacheFilePath))
        {
            Console.Error.WriteLine($"[WARN] Cache file not found or invalid: {cacheFileName}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(cacheFilePath);
            using var doc = JsonDocument.Parse(json);
            
            var tools = new List<Tool>();
            var toolsArray = doc.RootElement.GetProperty("tools").EnumerateArray();
            
            foreach (var toolElement in toolsArray)
            {
                var tool = JsonSerializer.Deserialize<Tool>(toolElement.GetRawText(), _jsonOptions);
                if (tool != null)
                {
                    tools.Add(tool);
                }
            }

            Console.Error.WriteLine($"[INFO] Using cached tools (Offline mode): {cacheFileName}");
            return tools;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARN] Cache file not found or invalid: {cacheFileName} - {ex.Message}");
            return null;
        }
    }

    public string GenerateCacheFileName(string openApiUrl)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(openApiUrl));
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return $"tools_cache_{hashString.Substring(0, 16)}.json";
    }

    public bool CacheExists(string openApiUrl)
    {
        var cacheFileName = GenerateCacheFileName(openApiUrl);
        var cacheFilePath = Path.Combine(_cacheDirectory, cacheFileName);
        return File.Exists(cacheFilePath);
    }
}