using OpenAPI_MCP_Proxy.Models;
using System.Text.Json;

namespace OpenAPI_MCP_Proxy.Services;

public class McpService
{
    private readonly OpenApiService _openApiService;
    private readonly HttpProxyService _httpProxyService;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<Tool> _tools = new();

    public McpService(OpenApiService openApiService, HttpProxyService httpProxyService)
    {
        _openApiService = openApiService;
        _httpProxyService = httpProxyService;
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

    private Task<McpResponse> HandleInitializeAsync(McpRequest request)
    {
        // Generate tools after initialization
        _tools = _openApiService.GenerateTools();

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

        return Task.FromResult(new McpResponse
        {
            Id = request.Id,
            Result = response
        });
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        // If tools haven't been generated yet, generate them now
        if (_tools.Count == 0)
        {
            _tools = _openApiService.GenerateTools();
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
            
            var result = await _httpProxyService.ExecuteRequestAsync(method, url, body);

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
