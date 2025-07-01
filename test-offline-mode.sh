#!/bin/bash

echo "Testing OpenAPI-MCP-Proxy Offline Mode"
echo "======================================"

# Test URL (using a fake URL to simulate offline)
OPENAPI_URL="http://localhost:9999/openapi.json"

echo ""
echo "1. Testing with offline server (should use cache if exists)"
echo "------------------------------------------------------------"

# Send initialize request
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$OPENAPI_URL" 2>&1 | grep -E "\[(INFO|WARN|DEBUG)\]|error"

echo ""
echo "2. Testing tools/list request (should work from cache)"
echo "-------------------------------------------------------"

# Send tools/list request
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$OPENAPI_URL" 2>&1 | grep -E "\[(INFO|WARN|DEBUG)\]|tools"

echo ""
echo "3. Testing tools/call request (should return offline error)"
echo "-----------------------------------------------------------"

# Send tools/call request
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"test_tool","arguments":{}}}' | dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$OPENAPI_URL" 2>&1 | grep -E "\[(INFO|WARN|DEBUG)\]|error"

echo ""
echo "Test completed."