using OpenAPI_MCP_Proxy.Models;
using System.Text.Json;

namespace OpenAPI_MCP_Proxy.Services;

public class McpService
{
    private readonly OpenApiService _openApiService;
    private readonly HttpProxyService _httpProxyService;
    private readonly CacheService _cacheService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _openApiUrl;
    private List<Tool> _tools = new();
    private OperationMode _currentMode = OperationMode.Online;

    public McpService(OpenApiService openApiService, HttpProxyService httpProxyService, CacheService cacheService, string openApiUrl)
    {
        _cacheService = cacheService;
        _openApiService = openApiService;
        _httpProxyService = httpProxyService;
        _openApiUrl = openApiUrl;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public async Task RunAsync()
    {
        string? line;
        while ((line = await Console.In.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var request = JsonSerializer.Deserialize<McpRequest>(line, _jsonOptions);
                if (request == null)
                    continue;

                var response = await HandleRequestAsync(request);
                if (response != null)
                {
                    var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                    Console.WriteLine(responseJson);
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Failed to parse JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error handling request: {ex.Message}");
            }
        }
    }

    private async Task<McpResponse?> HandleRequestAsync(McpRequest request)
    {
        switch (request.Method)
        {
            case "initialize":
                return await HandleInitializeAsync(request);

            case "tools/list":
                return HandleToolsList(request);

            case "tools/call":
                return await HandleToolCallAsync(request);

            case "notifications/initialized":
                // This is a notification, no response needed
                return null;

            default:
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32601,
                        Message = $"Method not found: {request.Method}"
                    }
                };
        }
    }

    private async Task<McpResponse> HandleInitializeAsync(McpRequest request)
    {
        // Try to generate tools online, fall back to cache if offline
        try
        {
            _tools = _openApiService.GenerateTools();
            _currentMode = OperationMode.Online;
            // Save to cache for offline use
            await _cacheService.SaveToolsCacheAsync(_openApiUrl, _tools);
        }
        catch (HttpRequestException)
        {
            // Switch to offline mode and try to load from cache
            var previousMode = _currentMode;
            _currentMode = OperationMode.Offline;
            Console.Error.WriteLine($"[INFO] Operation mode changed: {previousMode} -> {_currentMode}");
            
            var cachedTools = await _cacheService.LoadToolsCacheAsync(_openApiUrl);
            if (cachedTools != null)
            {
                _tools = cachedTools;
            }
            else
            {
                _tools = new List<Tool>();
            }
        }

        var initParams = request.Params != null
            ? JsonSerializer.Deserialize<InitializeRequest>(JsonSerializer.Serialize(request.Params), _jsonOptions)
            : new InitializeRequest();

        var response = new InitializeResponse
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new McpCapabilities
            {
                Tools = new ToolCapabilities { ListChanged = false }
            },
            ServerInfo = new ServerInfo
            {
                Name = "openapi-mcp-proxy",
                Version = "0.1.0"
            }
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = response
        };
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        // If tools haven't been generated yet, try to generate or load from cache
        if (_tools.Count == 0)
        {
            if (_currentMode == OperationMode.Online)
            {
                try
                {
                    _tools = _openApiService.GenerateTools();
                    // Save to cache asynchronously
                    Task.Run(async () => await _cacheService.SaveToolsCacheAsync(_openApiUrl, _tools));
                }
                catch (HttpRequestException)
                {
                    // Switch to offline mode
                    var previousMode = _currentMode;
                    _currentMode = OperationMode.Offline;
                    Console.Error.WriteLine($"[INFO] Operation mode changed: {previousMode} -> {_currentMode}");
                    
                    // Try to load from cache
                    var cachedTools = _cacheService.LoadToolsCacheAsync(_openApiUrl).GetAwaiter().GetResult();
                    if (cachedTools != null)
                    {
                        _tools = cachedTools;
                    }
                }
            }
            else
            {
                // Offline mode - try to load from cache
                var cachedTools = _cacheService.LoadToolsCacheAsync(_openApiUrl).GetAwaiter().GetResult();
                if (cachedTools != null)
                {
                    _tools = cachedTools;
                }
            }
        }
        
        return new McpResponse
        {
            Id = request.Id,
            Result = new ToolsListResponse { Tools = _tools }
        };
    }

    private async Task<McpResponse> HandleToolCallAsync(McpRequest request)
    {
        try
        {
            var toolCall = request.Params != null
                ? JsonSerializer.Deserialize<ToolCallRequest>(JsonSerializer.Serialize(request.Params), _jsonOptions)
                : null;

            if (toolCall == null)
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32602,
                        Message = "Invalid params"
                    }
                };
            }

            // Debug: Log the arguments received
            Console.Error.WriteLine($"[DEBUG] Tool: {toolCall.Name}");
            Console.Error.WriteLine($"[DEBUG] Arguments: {JsonSerializer.Serialize(toolCall.Arguments, _jsonOptions)}");

            // Prepare and execute HTTP request
            var (method, url, body) = _openApiService.PrepareHttpRequest(toolCall.Name, toolCall.Arguments);
            
            // Debug: Log the prepared request
            Console.Error.WriteLine($"[DEBUG] Method: {method}");
            Console.Error.WriteLine($"[DEBUG] URL: {url}");
            Console.Error.WriteLine($"[DEBUG] Body: {JsonSerializer.Serialize(body, _jsonOptions)}");
            
            // Pass the current mode and callback for mode changes
            var result = await _httpProxyService.ExecuteRequestAsync(method, url, body, _currentMode, (newMode) => 
            {
                if (_currentMode != newMode)
                {
                    var previousMode = _currentMode;
                    _currentMode = newMode;
                    Console.Error.WriteLine($"[INFO] Operation mode changed: {previousMode} -> {_currentMode}");
                }
            });

            // Check if result contains an error
            var isError = false;
            try
            {
                var resultDoc = JsonDocument.Parse(result);
                if (resultDoc.RootElement.TryGetProperty("error", out _))
                {
                    isError = true;
                }
            }
            catch { }

            return new McpResponse
            {
                Id = request.Id,
                Result = new ToolCallResponse
                {
                    Content = new List<ToolContent>
                    {
                        new ToolContent
                        {
                            Type = "text",
                            Text = result
                        }
                    },
                    IsError = isError
                }
            };
        }
        catch (Exception ex)
        {
            return new McpResponse
            {
                Id = request.Id,
                Result = new ToolCallResponse
                {
                    Content = new List<ToolContent>
                    {
                        new ToolContent
                        {
                            Type = "text",
                            Text = JsonSerializer.Serialize(new { error = ex.Message })
                        }
                    },
                    IsError = true
                }
            };
        }
    }
}
