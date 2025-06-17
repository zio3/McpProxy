#!/bin/bash

echo "Testing OpenAPI-MCP-Proxy schema expansion..."

# Build in debug mode to avoid file lock issues
dotnet build OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj

echo -e "\n=== Testing with Petstore API ==="
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy https://petstore.swagger.io/v2/swagger.json

echo -e "\n=== Getting tools list ==="
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy https://petstore.swagger.io/v2/swagger.json > tools-output.json

echo "Tools list saved to tools-output.json"

# Display a sample of the output
echo -e "\n=== Sample of tools output (first 2000 chars) ==="
head -c 2000 tools-output.json

echo -e "\n\n=== Test complete ==="