#!/bin/bash

echo "=== Testing MCP Tool Call with InstructionStore API ==="

# Build the project
dotnet build OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -q

SWAGGER_URL="https://instructionstore20250614211800-hhfbe3dgaje7cfhr.japaneast-01.azurewebsites.net/swagger/v1/swagger.json"

# First, let's see what the schema looks like for postapisearchsingle
echo -e "\n=== Getting schema for postapisearchsingle tool ==="
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy "$SWAGGER_URL" 2>/dev/null | \
    python3 -c "
import json
import sys
data = json.load(sys.stdin)
for tool in data['result']['tools']:
    if tool['name'] == 'postapisearchsingle':
        print('Tool Schema:')
        print(json.dumps(tool['inputSchema'], indent=2))
        break
"

echo -e "\n=== Testing tool call with flattened arguments ==="
# Test with flattened arguments
cat > test-call-flat.json << 'EOF'
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "query": {
        "queryId": "test123",
        "searchTarget": "documents",
        "searchText": "test search"
      }
    }
  }
}
EOF

echo "Request (flattened):"
cat test-call-flat.json | python3 -m json.tool

echo -e "\nResponse:"
cat test-call-flat.json | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy "$SWAGGER_URL" 2>&1 | \
    grep -E "(error|result|Error|Exception)" | head -20

echo -e "\n=== Testing tool call with nested body ==="
# Test with nested body
cat > test-call-nested.json << 'EOF'
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "postapisearchsingle",
    "arguments": {
      "body": {
        "query": {
          "queryId": "test123",
          "searchTarget": "documents",
          "searchText": "test search"
        }
      }
    }
  }
}
EOF

echo "Request (nested with body):"
cat test-call-nested.json | python3 -m json.tool

echo -e "\nResponse:"
cat test-call-nested.json | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy "$SWAGGER_URL" 2>&1 | \
    grep -E "(error|result|Error|Exception)" | head -20

# Clean up
rm -f test-call-flat.json test-call-nested.json

echo -e "\n=== Test complete ==="