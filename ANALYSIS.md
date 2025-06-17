# OpenAPI-MCP-Proxy Argument Handling Analysis

## The Problem

When testing the OpenAPI-MCP-Proxy with the InstructionStore API, users are asked for "JSON arguments" but the expected structure is unclear. The issue appears to be in how the proxy handles request body parameters.

## Current Implementation

### 1. Schema Generation (OpenApiService.GenerateInputSchema)
- Creates a "body" parameter in the tool schema for request bodies
- The "body" parameter contains the full schema of the request body
- For example, if the API expects `{ "query": "string" }`, the tool schema will have:
  ```json
  {
    "properties": {
      "body": {
        "type": "object",
        "properties": {
          "query": {
            "type": "string",
            "description": "..."
          }
        }
      }
    }
  }
  ```

### 2. Argument Processing (OpenApiService.PrepareHttpRequest)
- Looks for a "body" key in the arguments
- Takes the value of that key as the request body
- Expected argument structure:
  ```json
  {
    "body": {
      "query": "search term"
    }
  }
  ```

## The Issue

The current implementation requires users to nest their actual API parameters inside a "body" object, which is not intuitive. When MCP Inspector or other clients ask for "arguments", users might naturally try:
- `{"query": "search term"}` - Won't work, missing "body" wrapper
- `"search term"` - Won't work, not the right structure
- `{"body": "search term"}` - Won't work, body needs to be an object

## Recommended Solutions

### Solution 1: Flatten Request Body Parameters (Recommended)
Instead of nesting request body parameters under "body", flatten them at the top level:

```csharp
// In GenerateInputSchema
if (operation.RequestBody?.Content != null)
{
    var contentEntry = operation.RequestBody.Content.FirstOrDefault();
    if (contentEntry.Value?.Schema?.Properties != null)
    {
        // Add each property directly to the schema
        foreach (var prop in contentEntry.Value.Schema.Properties)
        {
            schema.Properties[prop.Key] = ConvertOpenApiSchemaToJsonSchema(prop.Value);
            if (contentEntry.Value.Schema.Required?.Contains(prop.Key) == true)
            {
                schema.Required.Add(prop.Key);
            }
        }
    }
}
```

This would allow users to provide arguments as: `{"query": "search term"}`

### Solution 2: Better Documentation in Tool Description
Update the tool description to include example usage:

```csharp
var description = $"{operation.Summary ?? operation.Description ?? "No description"}";
if (operation.RequestBody != null)
{
    description += $"\n\nArguments should be provided as: {{\"body\": {{...request body...}}}}";
    // Add specific example based on schema
}
```

### Solution 3: Smart Argument Detection
In PrepareHttpRequest, detect if arguments match the expected body schema directly:

```csharp
// If no "body" key but arguments match expected body schema, use arguments as body
if (!arguments.ContainsKey("body") && operation.RequestBody != null)
{
    // Check if arguments structure matches expected body schema
    body = arguments;
}
```

## Testing the Current Implementation

To test with the current implementation, use this structure:

```bash
# For postapisearchsingle tool
echo '{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "body": {
        "query": "test search"
      }
    }
  }
}' | OpenAPI-MCP-Proxy.exe <api-url>
```