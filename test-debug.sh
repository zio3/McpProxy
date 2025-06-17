#!/bin/bash

# Debug test script to understand argument handling

PROXY_PATH="./OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy.exe"
API_URL="https://raw.githubusercontent.com/spdustin/instructions-mcp-server/refs/heads/main/src/instructions/instructions-openapi.json"

echo "=== Debug Test for Argument Handling ==="
echo

# First, let's see what the tool schema looks like
echo "1. Getting tool schema for postapisearchsingle..."
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | $PROXY_PATH "$API_URL" > /dev/null 2>&1

echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | $PROXY_PATH "$API_URL" | grep -A 30 "postapisearchsingle" || echo "Tool not found in list"
echo
echo "---"
echo

# Now test the actual call with proper structure
echo "2. Testing tool call with body containing query..."
cat << 'EOF' | $PROXY_PATH "$API_URL" 2>&1
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "body": {
        "query": "test search"
      }
    }
  }
}
EOF