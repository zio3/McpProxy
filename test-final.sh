#!/bin/bash

echo "=== OpenAPI-MCP-Proxy Schema Expansion Test ==="

# Build the project
dotnet build OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -q

# Create test API spec
cat > test-api-final.json << 'EOF'
{
  "openapi": "3.0.0",
  "info": {
    "title": "Test API",
    "version": "1.0.0"
  },
  "servers": [
    {"url": "https://api.example.com"}
  ],
  "paths": {
    "/search": {
      "post": {
        "operationId": "searchItems",
        "summary": "Search for items with filters",
        "requestBody": {
          "required": true,
          "description": "Search parameters including query and filters",
          "content": {
            "application/json": {
              "schema": {
                "type": "object",
                "required": ["query"],
                "properties": {
                  "query": {
                    "type": "string",
                    "description": "Search query string",
                    "minLength": 1,
                    "maxLength": 200
                  },
                  "filters": {
                    "type": "object",
                    "description": "Optional search filters",
                    "properties": {
                      "category": {
                        "type": "string",
                        "description": "Product category",
                        "enum": ["electronics", "books", "clothing", "home", "sports"]
                      },
                      "priceRange": {
                        "type": "object",
                        "description": "Price range filter",
                        "properties": {
                          "min": {
                            "type": "number",
                            "description": "Minimum price",
                            "minimum": 0
                          },
                          "max": {
                            "type": "number",
                            "description": "Maximum price",
                            "minimum": 0
                          }
                        }
                      },
                      "inStock": {
                        "type": "boolean",
                        "description": "Only show items in stock",
                        "default": true
                      }
                    }
                  },
                  "sortBy": {
                    "type": "string",
                    "description": "Sort results by",
                    "enum": ["relevance", "price_asc", "price_desc", "rating"],
                    "default": "relevance"
                  },
                  "page": {
                    "type": "integer",
                    "description": "Page number",
                    "minimum": 1,
                    "default": 1
                  },
                  "limit": {
                    "type": "integer",
                    "description": "Results per page",
                    "minimum": 1,
                    "maximum": 100,
                    "default": 20
                  }
                }
              }
            }
          }
        },
        "responses": {
          "200": {
            "description": "Search results"
          }
        }
      }
    }
  }
}
EOF

# Start HTTP server
python3 -m http.server 8002 > /dev/null 2>&1 &
SERVER_PID=$!
sleep 2

echo -e "\n=== Testing schema expansion with complex nested structure ==="

# Get tools list
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy http://localhost:8002/test-api-final.json 2>/dev/null > result.json

# Pretty print the schema using Python
python3 << 'PYTHON_SCRIPT'
import json

with open('result.json', 'r') as f:
    data = json.load(f)

if 'result' in data and 'tools' in data['result']:
    for tool in data['result']['tools']:
        print(f"\n=== Tool: {tool['name']} ===")
        print(f"Description: {tool['description']}")
        print("\nExpanded Input Schema:")
        print(json.dumps(tool['inputSchema'], indent=2))
        
        # Show the body schema specifically
        if 'properties' in tool['inputSchema'] and 'body' in tool['inputSchema']['properties']:
            print("\nRequest Body Schema Details:")
            body_schema = tool['inputSchema']['properties']['body']
            print(json.dumps(body_schema, indent=2))
else:
    print("No tools found in response")
    print(json.dumps(data, indent=2))
PYTHON_SCRIPT

# Cleanup
kill $SERVER_PID 2>/dev/null
rm -f test-api-final.json result.json

echo -e "\n=== Schema expansion test complete! ==="