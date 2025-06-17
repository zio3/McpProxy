# OpenAPI-MCP-Proxy Usage Guide

## Current Implementation (Workaround Required)

Due to how the current implementation handles request bodies, you need to wrap your API parameters in a "body" object when calling tools.

### Example: Using the InstructionStore API

1. **Initialize the proxy:**
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | OpenAPI-MCP-Proxy.exe <api-url>
```

2. **List available tools:**
```bash
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | OpenAPI-MCP-Proxy.exe <api-url>
```

3. **Call a tool with request body parameters:**

For the `postapisearchsingle` tool which expects a `query` parameter:

**Current workaround (wrap in "body"):**
```bash
echo '{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "body": {
        "query": "your search term"
      }
    }
  }
}' | OpenAPI-MCP-Proxy.exe <api-url>
```

### Understanding the Arguments Structure

When the tool schema shows:
```json
{
  "inputSchema": {
    "type": "object",
    "properties": {
      "body": {
        "type": "object",
        "properties": {
          "query": {
            "type": "string",
            "description": "Search query"
          }
        }
      }
    }
  }
}
```

You need to provide arguments that match this structure exactly:
```json
{
  "body": {
    "query": "your value here"
  }
}
```

### Common Patterns

1. **Tools with no parameters (like gethealth):**
```json
{
  "arguments": {}
}
```

2. **Tools with query parameters:**
```json
{
  "arguments": {
    "limit": 10,
    "offset": 0
  }
}
```

3. **Tools with request body (current implementation):**
```json
{
  "arguments": {
    "body": {
      "field1": "value1",
      "field2": "value2"
    }
  }
}
```

4. **Tools with both query params and request body:**
```json
{
  "arguments": {
    "limit": 10,
    "body": {
      "query": "search term"
    }
  }
}
```

## Future Improvement

We've prepared an improvement that will flatten request body parameters, allowing more intuitive usage:

**Future (after improvement):**
```json
{
  "arguments": {
    "query": "your search term"
  }
}
```

But for now, always wrap request body parameters in a "body" object.