#!/bin/bash

echo "=== Testing OpenAPI-MCP-Proxy with InstructionStore API ==="

SWAGGER_URL="https://instructionstore20250614211800-hhfbe3dgaje7cfhr.japaneast-01.azurewebsites.net/swagger/v1/swagger.json"

echo -e "\nURL: $SWAGGER_URL"
echo -e "\n=== Initializing MCP connection ==="

# Initialize
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy "$SWAGGER_URL" 2>&1 | \
    grep -v "Loading OpenAPI"

echo -e "\n=== Getting tools list ==="

# Get tools list and save to file
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy "$SWAGGER_URL" 2>/dev/null > instructionstore-tools.json

# Pretty print tools with Python
python3 << 'EOF'
import json

with open('instructionstore-tools.json', 'r') as f:
    data = json.load(f)

if 'result' in data and 'tools' in data['result']:
    tools = data['result']['tools']
    print(f"\nTotal tools found: {len(tools)}")
    
    # List all tool names
    print("\nAvailable tools:")
    for i, tool in enumerate(tools, 1):
        print(f"{i}. {tool['name']} - {tool['description']}")
    
    # Find and display a tool with request body (likely a POST endpoint)
    print("\n=== Looking for tools with request body schemas ===")
    
    for tool in tools:
        if 'inputSchema' in tool and 'properties' in tool['inputSchema']:
            if 'body' in tool['inputSchema']['properties']:
                print(f"\nTool: {tool['name']}")
                print(f"Description: {tool['description']}")
                print("\nRequest Body Schema:")
                body_schema = tool['inputSchema']['properties']['body']
                print(json.dumps(body_schema, indent=2))
                
                # Show first example only
                break
else:
    print("Error or no tools found")
    print(json.dumps(data, indent=2))
EOF

# Clean up
rm -f instructionstore-tools.json

echo -e "\n=== Test complete ==="