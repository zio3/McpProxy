#!/bin/bash

echo "=== Testing Schema Flattening Logic ==="

# Create a test OpenAPI spec to confirm flattening behavior
cat > test-flatten.json << 'EOF'
{
  "openapi": "3.0.0",
  "info": {"title": "Test", "version": "1.0.0"},
  "servers": [{"url": "https://example.com"}],
  "paths": {
    "/test": {
      "post": {
        "operationId": "testOperation",
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": {
                "type": "object",
                "properties": {
                  "name": {"type": "string"},
                  "value": {"type": "number"}
                },
                "required": ["name"]
              }
            }
          }
        }
      }
    }
  }
}
EOF

# Start HTTP server
python3 -m http.server 8003 > /dev/null 2>&1 &
SERVER_PID=$!
sleep 2

echo "Testing with simple object schema..."
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy http://localhost:8003/test-flatten.json 2>/dev/null | \
    python3 -c "
import json
import sys
data = json.load(sys.stdin)
for tool in data['result']['tools']:
    print(f'Tool: {tool[\"name\"]}')
    schema = tool['inputSchema']
    if 'properties' in schema:
        print('Properties:')
        for prop, value in schema['properties'].items():
            print(f'  - {prop}: {value.get(\"type\", \"?\")}')
    if 'required' in schema:
        print(f'Required: {schema[\"required\"]}')
"

# Kill server
kill $SERVER_PID 2>/dev/null
rm -f test-flatten.json

echo -e "\n=== Checking code logic ==="
echo "The issue appears to be that the schema has properties, but they are not being flattened."
echo "Looking at the condition in GenerateInputSchema:"
echo "  if (contentEntry.Value.Schema.Type == \"object\" && contentEntry.Value.Schema.Properties?.Count > 0)"
echo ""
echo "This should flatten the schema, but it's not happening for the InstructionStore API."