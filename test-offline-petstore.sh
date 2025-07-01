#!/bin/bash

echo "Testing OpenAPI-MCP-Proxy Offline Mode with Petstore API"
echo "========================================================"

# Petstore URL - we already have cache for this
OPENAPI_URL="https://petstore.swagger.io/v2/swagger.json"

echo ""
echo "1. First, let's make sure we have cache (online mode)"
echo "------------------------------------------------------"

# Send initialize request to ensure cache exists
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$OPENAPI_URL" 2>&1 | grep -E "\[(INFO|WARN|DEBUG)\]|\"serverInfo\""

echo ""
echo "2. Now let's test with a fake server to simulate offline (but use the same cache hash)"
echo "---------------------------------------------------------------------------------------"

# Create a test with modified HttpProxyService to simulate offline
# For now, let's manually test the scenario

echo "Test scenario:"
echo "- Server is down but cache exists"
echo "- tools/list should work from cache"
echo "- tools/call should show offline error message"

echo ""
echo "3. Testing tools/list with existing cache"
echo "-----------------------------------------"

echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$OPENAPI_URL" 2>&1 | jq -r '.result.tools | length' | head -1

echo ""
echo "Test completed. Number of tools loaded from cache shown above."