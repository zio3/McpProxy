#!/bin/bash

# Test script for OpenAPI-MCP-Proxy schema expansion

echo "Building the project..."
dotnet build OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -c Release

echo -e "\n=== Testing with Petstore API ==="
echo "Sending tools/list request to see the expanded schema..."

# Test tools/list to see the expanded schema
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Release/net9.0/OpenAPI-MCP-Proxy https://petstore.swagger.io/v2/swagger.json | \
    jq '.result.tools[] | select(.name == "addPet") | .inputSchema'

echo -e "\n=== Testing with a sample OpenAPI spec with complex schema ==="
echo "Creating a test OpenAPI spec with nested objects..."

# Create a test OpenAPI spec with complex nested schema
cat > test-openapi.json << 'EOF'
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
        "summary": "Search for items",
        "requestBody": {
          "required": true,
          "description": "Search parameters",
          "content": {
            "application/json": {
              "schema": {
                "type": "object",
                "required": ["query"],
                "properties": {
                  "query": {
                    "type": "string",
                    "description": "Search query string"
                  },
                  "filters": {
                    "type": "object",
                    "description": "Optional filters",
                    "properties": {
                      "category": {
                        "type": "string",
                        "enum": ["electronics", "books", "clothing"]
                      },
                      "priceRange": {
                        "type": "object",
                        "properties": {
                          "min": {
                            "type": "number",
                            "minimum": 0
                          },
                          "max": {
                            "type": "number",
                            "minimum": 0
                          }
                        }
                      }
                    }
                  },
                  "pagination": {
                    "type": "object",
                    "properties": {
                      "page": {
                        "type": "integer",
                        "minimum": 1,
                        "default": 1
                      },
                      "limit": {
                        "type": "integer",
                        "minimum": 1,
                        "maximum": 100,
                        "default": 20
                      }
                    }
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

# Start a simple HTTP server to serve the test spec
python3 -m http.server 8000 > /dev/null 2>&1 &
SERVER_PID=$!
sleep 2

echo -e "\nTesting with complex nested schema..."
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | \
    OpenAPI-MCP-Proxy/bin/Release/net9.0/OpenAPI-MCP-Proxy http://localhost:8000/test-openapi.json | \
    jq '.result.tools[] | select(.name == "searchItems") | .inputSchema'

# Clean up
kill $SERVER_PID 2>/dev/null
rm -f test-openapi.json

echo -e "\n=== Schema expansion test complete ==="