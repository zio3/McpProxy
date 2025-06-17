#!/bin/bash

echo "=== InstructionStore API - Detailed Schema Analysis ==="

SWAGGER_URL="https://instructionstore20250614211800-hhfbe3dgaje7cfhr.japaneast-01.azurewebsites.net/swagger/v1/swagger.json"

# Get tools list
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy "$SWAGGER_URL" 2>/dev/null > instructionstore-detailed.json

# Analyze specific tools with complex schemas
python3 << 'EOF'
import json

with open('instructionstore-detailed.json', 'r') as f:
    data = json.load(f)

if 'result' in data and 'tools' in data['result']:
    tools = data['result']['tools']
    
    # Look for specific tools with potentially complex schemas
    target_tools = [
        'postapisearchsingle',
        'postapisearchbatch',
        'createdocument',
        'copyitems',
        'bulkoperations'
    ]
    
    print(f"Total tools: {len(tools)}\n")
    
    for tool in tools:
        if tool['name'] in target_tools:
            print("=" * 60)
            print(f"Tool: {tool['name']}")
            print(f"Description: {tool['description']}")
            
            if 'inputSchema' in tool:
                schema = tool['inputSchema']
                
                # Check for path/query parameters
                params_count = 0
                body_exists = False
                
                if 'properties' in schema:
                    for prop_name, prop_value in schema['properties'].items():
                        if prop_name == 'body':
                            body_exists = True
                        else:
                            params_count += 1
                
                if params_count > 0:
                    print(f"\nPath/Query Parameters: {params_count}")
                    for prop_name, prop_value in schema['properties'].items():
                        if prop_name != 'body':
                            print(f"  - {prop_name}: {prop_value.get('type', 'unknown')}")
                            if 'description' in prop_value:
                                print(f"    Description: {prop_value['description']}")
                
                if body_exists:
                    print("\nRequest Body Schema:")
                    body_schema = schema['properties']['body']
                    print(json.dumps(body_schema, indent=2, ensure_ascii=False))
                
                if 'required' in schema:
                    print(f"\nRequired fields: {schema['required']}")
            
            print()
else:
    print("No tools found in response")
EOF

# Clean up
rm -f instructionstore-detailed.json

echo -e "\n=== Detailed analysis complete ==="