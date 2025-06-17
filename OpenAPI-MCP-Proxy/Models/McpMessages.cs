using System.Text.Json.Serialization;

namespace OpenAPI_MCP_Proxy.Models;

// Base MCP message types
public abstract class McpMessage
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
}

public class McpRequest : McpMessage
{
    [JsonPropertyName("id")]
    public object Id { get; set; } = null!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Params { get; set; }
}

public class McpResponse : McpMessage
{
    [JsonPropertyName("id")]
    public object Id { get; set; } = null!;

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; set; }
}

public class McpNotification : McpMessage
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Params { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

// Initialize request/response
public class InitializeRequest
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("clientInfo")]
    public ClientInfo ClientInfo { get; set; } = null!;
}

public class InitializeResponse
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; set; } = new();
}

public class McpCapabilities
{
    [JsonPropertyName("experimental")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Experimental { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCapabilities? Tools { get; set; }
}

public class ToolCapabilities
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; } = false;
}

public class ClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "openapi-mcp-proxy";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";
}

// Tools list request/response
public class ToolsListResponse
{
    [JsonPropertyName("tools")]
    public List<Tool> Tools { get; set; } = new();
}

public class Tool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("inputSchema")]
    public ToolInputSchema InputSchema { get; set; } = new();
}

public class ToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Properties { get; set; }

    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Required { get; set; }
}

// Tool call request/response
public class ToolCallRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Arguments { get; set; }
}

public class ToolCallResponse
{
    [JsonPropertyName("content")]
    public List<ToolContent> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError { get; set; }
}

public class ToolContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}
