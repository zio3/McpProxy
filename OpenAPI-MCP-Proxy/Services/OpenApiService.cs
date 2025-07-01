using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using OpenAPI_MCP_Proxy.Models;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace OpenAPI_MCP_Proxy.Services;

public class OpenApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _toolPrefix;
    private OpenApiDocument? _document;
    private string? _baseUrl;
    private readonly Dictionary<string, OpenApiOperation> _operations = new();

    public OpenApiService(string toolPrefix = "")
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // Set reasonable timeout
        _toolPrefix = toolPrefix;
    }

    public async Task LoadSpecificationAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new OpenApiStreamReader();
            var readResult = await reader.ReadAsync(stream);

            if (readResult.OpenApiDiagnostic.Errors.Any())
            {
                throw new Exception($"Failed to parse OpenAPI document: {string.Join(", ", readResult.OpenApiDiagnostic.Errors)}");
            }

            _document = readResult.OpenApiDocument;
            _baseUrl = _document.Servers?.FirstOrDefault()?.Url ?? new Uri(url).GetLeftPart(UriPartial.Authority);

            // Remove trailing slash from base URL
            if (_baseUrl.EndsWith("/"))
            {
                _baseUrl = _baseUrl.TrimEnd('/');
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load OpenAPI specification: {ex.Message}", ex);
        }
    }

    public List<Tool> GenerateTools()
    {
        var tools = new List<Tool>();

        if (_document?.Paths == null)
            return tools;

        foreach (var pathItem in _document.Paths)
        {
            var path = pathItem.Key;
            
            foreach (var operation in pathItem.Value.Operations)
            {
                var method = operation.Key;
                var op = operation.Value;

                var toolName = GenerateToolName(method, path, op);
                _operations[toolName] = op;

                var tool = new Tool
                {
                    Name = toolName,
                    Description = op.Summary ?? op.Description ?? $"{method} {path}",
                    InputSchema = GenerateInputSchema(path, op)
                };

                tools.Add(tool);
            }
        }

        return tools;
    }

    private string GenerateToolName(OperationType method, string path, OpenApiOperation operation)
    {
        // Use operationId if available
        string baseName;
        if (!string.IsNullOrEmpty(operation.OperationId))
        {
            baseName = SanitizeToolName(operation.OperationId);
        }
        else
        {
            // Generate from method and path
            var name = $"{method}_{path}".ToLower();
            baseName = SanitizeToolName(name);
        }
        
        // Apply prefix if specified
        if (!string.IsNullOrEmpty(_toolPrefix))
        {
            return _toolPrefix + baseName;
        }
        
        return baseName;
    }

    private string SanitizeToolName(string name)
    {
        // Replace special characters with underscores
        name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
        
        // Remove consecutive underscores
        name = Regex.Replace(name, @"_+", "_");
        
        // Trim underscores from start and end
        name = name.Trim('_');
        
        // Ensure it starts with a letter
        if (name.Length > 0 && char.IsDigit(name[0]))
        {
            name = "op_" + name;
        }

        return name.ToLower();
    }

    private ToolInputSchema GenerateInputSchema(string path, OpenApiOperation operation)
    {
        var schema = new ToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, object>(),
            Required = new List<string>()
        };

        // Add path parameters
        foreach (var parameter in operation.Parameters ?? Enumerable.Empty<OpenApiParameter>())
        {
            if (parameter.In == ParameterLocation.Path || parameter.In == ParameterLocation.Query)
            {
                var paramSchema = new Dictionary<string, object>
                {
                    ["type"] = GetJsonType(parameter.Schema?.Type ?? "string"),
                    ["description"] = parameter.Description ?? $"{parameter.Name} parameter"
                };

                schema.Properties[parameter.Name] = paramSchema;

                if (parameter.Required)
                {
                    schema.Required.Add(parameter.Name);
                }
            }
        }

        // Add request body parameters directly to the schema (flattened)
        if (operation.RequestBody?.Content != null)
        {
            // Get the first content type (usually application/json)
            var contentEntry = operation.RequestBody.Content.FirstOrDefault();
            if (contentEntry.Value?.Schema != null)
            {
                // If the body schema is an object with properties, flatten them
                // Note: Some OpenAPI specs don't specify type="object" explicitly
                if ((contentEntry.Value.Schema.Type == "object" || contentEntry.Value.Schema.Type == null) && 
                    contentEntry.Value.Schema.Properties?.Count > 0)
                {
                    // Add each property from the request body directly to the tool schema
                    foreach (var prop in contentEntry.Value.Schema.Properties)
                    {
                        var propSchema = ConvertOpenApiSchemaToJsonSchema(prop.Value);
                        schema.Properties[prop.Key] = propSchema;
                        
                        // Add to required if needed
                        if (operation.RequestBody.Required && contentEntry.Value.Schema.Required?.Contains(prop.Key) == true)
                        {
                            schema.Required.Add(prop.Key);
                        }
                    }
                }
                else
                {
                    // For non-object schemas or schemas without properties, keep as "body" parameter
                    var bodySchema = ConvertOpenApiSchemaToJsonSchema(contentEntry.Value.Schema);
                    
                    // Add description from request body if not already in schema
                    if (bodySchema is Dictionary<string, object> bodyDict && !bodyDict.ContainsKey("description"))
                    {
                        bodyDict["description"] = operation.RequestBody.Description ?? "Request body";
                    }

                    schema.Properties["body"] = bodySchema;
                    
                    if (operation.RequestBody.Required)
                    {
                        schema.Required.Add("body");
                    }
                }
            }
            else
            {
                // Fallback to simple object if no schema found
                var bodySchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["description"] = operation.RequestBody.Description ?? "Request body"
                };
                schema.Properties["body"] = bodySchema;
                
                if (operation.RequestBody.Required)
                {
                    schema.Required.Add("body");
                }
            }
        }

        // If no properties, remove the properties field
        if (schema.Properties.Count == 0)
        {
            schema.Properties = null;
        }

        // If no required fields, remove the required field
        if (schema.Required.Count == 0)
        {
            schema.Required = null;
        }

        return schema;
    }

    private string GetJsonType(string openApiType)
    {
        return openApiType switch
        {
            "integer" => "number",
            "boolean" => "boolean",
            "array" => "array",
            "object" => "object",
            _ => "string"
        };
    }

    private object ConvertOpenApiSchemaToJsonSchema(OpenApiSchema openApiSchema, HashSet<string>? visitedRefs = null)
    {
        // Handle circular references
        visitedRefs ??= new HashSet<string>();
        
        // Check for reference
        if (openApiSchema.Reference != null)
        {
            var refId = openApiSchema.Reference.Id;
            if (visitedRefs.Contains(refId))
            {
                // Circular reference detected, return a simple object type
                return new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["description"] = $"Circular reference to {refId}"
                };
            }
            visitedRefs.Add(refId);
        }

        var jsonSchema = new Dictionary<string, object>();

        // Type
        if (!string.IsNullOrEmpty(openApiSchema.Type))
        {
            jsonSchema["type"] = GetJsonType(openApiSchema.Type);
        }

        // Description
        if (!string.IsNullOrEmpty(openApiSchema.Description))
        {
            jsonSchema["description"] = openApiSchema.Description;
        }

        // Handle different schema types
        if (openApiSchema.Type == "object" && openApiSchema.Properties != null && openApiSchema.Properties.Count > 0)
        {
            // Convert properties
            var properties = new Dictionary<string, object>();
            foreach (var prop in openApiSchema.Properties)
            {
                properties[prop.Key] = ConvertOpenApiSchemaToJsonSchema(prop.Value, visitedRefs);
            }
            jsonSchema["properties"] = properties;

            // Required fields
            if (openApiSchema.Required != null && openApiSchema.Required.Count > 0)
            {
                jsonSchema["required"] = openApiSchema.Required.ToList();
            }
        }
        else if (openApiSchema.Type == "array" && openApiSchema.Items != null)
        {
            // Convert array items
            jsonSchema["items"] = ConvertOpenApiSchemaToJsonSchema(openApiSchema.Items, visitedRefs);
        }
        else if (openApiSchema.Enum != null && openApiSchema.Enum.Count > 0)
        {
            // Handle enums
            var enumValues = new List<object>();
            foreach (var enumValue in openApiSchema.Enum)
            {
                if (enumValue is Microsoft.OpenApi.Any.IOpenApiPrimitive primitive)
                {
                    enumValues.Add(ConvertOpenApiPrimitive(primitive));
                }
            }
            if (enumValues.Count > 0)
            {
                jsonSchema["enum"] = enumValues;
            }
        }

        // Format (for additional type information)
        if (!string.IsNullOrEmpty(openApiSchema.Format))
        {
            jsonSchema["format"] = openApiSchema.Format;
        }

        // Default value
        if (openApiSchema.Default != null && openApiSchema.Default is Microsoft.OpenApi.Any.IOpenApiPrimitive defaultPrimitive)
        {
            jsonSchema["default"] = ConvertOpenApiPrimitive(defaultPrimitive);
        }

        // Min/Max values for numbers
        if (openApiSchema.Minimum != null)
        {
            jsonSchema["minimum"] = openApiSchema.Minimum;
        }
        if (openApiSchema.Maximum != null)
        {
            jsonSchema["maximum"] = openApiSchema.Maximum;
        }

        // String constraints
        if (openApiSchema.MinLength != null)
        {
            jsonSchema["minLength"] = openApiSchema.MinLength;
        }
        if (openApiSchema.MaxLength != null)
        {
            jsonSchema["maxLength"] = openApiSchema.MaxLength;
        }
        if (!string.IsNullOrEmpty(openApiSchema.Pattern))
        {
            jsonSchema["pattern"] = openApiSchema.Pattern;
        }

        // Array constraints
        if (openApiSchema.MinItems != null)
        {
            jsonSchema["minItems"] = openApiSchema.MinItems;
        }
        if (openApiSchema.MaxItems != null)
        {
            jsonSchema["maxItems"] = openApiSchema.MaxItems;
        }
        if (openApiSchema.UniqueItems != null)
        {
            jsonSchema["uniqueItems"] = openApiSchema.UniqueItems;
        }

        return jsonSchema;
    }

    private object ConvertOpenApiPrimitive(Microsoft.OpenApi.Any.IOpenApiPrimitive primitive)
    {
        return primitive switch
        {
            Microsoft.OpenApi.Any.OpenApiString str => str.Value,
            Microsoft.OpenApi.Any.OpenApiInteger intVal => intVal.Value,
            Microsoft.OpenApi.Any.OpenApiLong longVal => longVal.Value,
            Microsoft.OpenApi.Any.OpenApiFloat floatVal => floatVal.Value,
            Microsoft.OpenApi.Any.OpenApiDouble doubleVal => doubleVal.Value,
            Microsoft.OpenApi.Any.OpenApiBoolean boolVal => boolVal.Value,
            Microsoft.OpenApi.Any.OpenApiNull => null!,
            _ => primitive.ToString()
        };
    }

    public (string method, string url, object? body) PrepareHttpRequest(string toolName, Dictionary<string, object>? arguments)
    {

        if (!_operations.TryGetValue(toolName, out var operation))
        {
            throw new Exception($"Tool '{toolName}' not found");
        }

        // Find the path and method for this operation
        string? path = null;
        OperationType? method = null;

        foreach (var pathItem in _document!.Paths)
        {
            foreach (var op in pathItem.Value.Operations)
            {
                if (op.Value == operation)
                {
                    path = pathItem.Key;
                    method = op.Key;
                    break;
                }
            }
            if (path != null) break;
        }

        if (path == null || method == null)
        {
            throw new Exception($"Could not find path for tool '{toolName}'");
        }

        // Replace path parameters
        var url = path;
        var queryParams = new List<string>();
        object? body = null;

        // Determine if this operation expects a request body
        bool hasRequestBody = operation.RequestBody?.Content != null;
        var bodyProperties = new Dictionary<string, object>();

        if (arguments != null)
        {
            foreach (var arg in arguments)
            {
                // Check if this is the legacy "body" parameter
                if (arg.Key == "body")
                {
                    body = arg.Value;
                    continue;
                }

                // Check if it's a path parameter
                if (url.Contains($"{{{arg.Key}}}"))
                {
                    url = url.Replace($"{{{arg.Key}}}", Uri.EscapeDataString(arg.Value?.ToString() ?? ""));
                }
                else if (operation.Parameters?.Any(p => p.Name == arg.Key && p.In == ParameterLocation.Query) == true)
                {
                    // It's explicitly a query parameter
                    queryParams.Add($"{Uri.EscapeDataString(arg.Key)}={Uri.EscapeDataString(arg.Value?.ToString() ?? "")}");
                }
                else if (hasRequestBody)
                {
                    // If this operation has a request body and this isn't a path/query param,
                    // assume it's a body property (for flattened schema)
                    bodyProperties[arg.Key] = arg.Value;
                }
                else
                {
                    // No request body expected, treat as query parameter
                    queryParams.Add($"{Uri.EscapeDataString(arg.Key)}={Uri.EscapeDataString(arg.Value?.ToString() ?? "")}");
                }
            }
        }

        // If we collected body properties and no explicit body was provided, use them as body
        if (body == null && bodyProperties.Count > 0)
        {
            body = bodyProperties;
        }

        // Build full URL
        var fullUrl = $"{_baseUrl}{url}";
        if (queryParams.Any())
        {
            fullUrl += "?" + string.Join("&", queryParams);
        }

        return (method?.ToString().ToUpper() ?? "GET", fullUrl, body);
    }
}
