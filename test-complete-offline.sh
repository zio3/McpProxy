#!/bin/bash

echo "Complete Offline Mode Test for OpenAPI-MCP-Proxy"
echo "================================================"

# Test with a URL that will fail
FAKE_URL="http://192.0.2.1:9999/openapi.json" # Non-routable IP
CACHE_HASH="3a0927ddd62cdfa9" # Hash for petstore URL we cached

echo ""
echo "1. Creating a fake cache file for testing"
echo "-----------------------------------------"

# Copy the existing petstore cache to simulate cache for our fake URL
CACHE_FILE="OpenAPI-MCP-Proxy/bin/Debug/net9.0/tools_cache_${CACHE_HASH}.json"
if [ -f "$CACHE_FILE" ]; then
    # Calculate hash for our fake URL
    FAKE_HASH=$(echo -n "$FAKE_URL" | sha256sum | cut -c1-16)
    FAKE_CACHE="OpenAPI-MCP-Proxy/bin/Debug/net9.0/tools_cache_${FAKE_HASH}.json"
    
    # Copy and update the cache file
    cp "$CACHE_FILE" "$FAKE_CACHE"
    # Update the openApiUrl in the cache
    sed -i "s|https://petstore.swagger.io/v2/swagger.json|$FAKE_URL|g" "$FAKE_CACHE"
    
    echo "Created fake cache file: $FAKE_CACHE"
else
    echo "ERROR: Petstore cache not found. Run with real Petstore URL first."
    exit 1
fi

echo ""
echo "2. Testing initialize in offline mode (should use cache)"
echo "--------------------------------------------------------"

echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | \
    timeout 10 dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$FAKE_URL" 2>&1 | \
    grep -E "Warning:|INFO|serverInfo" | head -5

echo ""
echo "3. Testing tools/list in offline mode (should work from cache)"
echo "--------------------------------------------------------------"

(echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}'; \
 sleep 0.5; \
 echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}') | \
    timeout 10 dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$FAKE_URL" 2>&1 | \
    grep -E "Using cached tools|\"tools\":|INFO" | head -5

echo ""
echo "4. Testing tools/call in offline mode (should show offline error)"
echo "-----------------------------------------------------------------"

(echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}'; \
 sleep 0.5; \
 echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"findPetsByStatus","arguments":{"status":"available"}}}') | \
    timeout 10 dotnet run --project OpenAPI-MCP-Proxy/OpenAPI-MCP-Proxy.csproj -- "$FAKE_URL" 2>&1 | \
    grep -E "オフライン|error|Operation mode changed" | head -5

echo ""
echo "5. Cleanup"
echo "----------"
rm -f "$FAKE_CACHE"
echo "Removed temporary cache file"

echo ""
echo "Test completed."