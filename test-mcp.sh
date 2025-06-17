#!/bin/bash

# Start the OpenAPI-MCP-Proxy and send test commands
cd "OpenAPI-MCP-Proxy"

# Create a named pipe for communication
exec 3< <(dotnet run https://petstore.swagger.io/v2/swagger.json)

# Wait a moment for the process to start
sleep 2

# Send initialize command
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | dotnet run https://petstore.swagger.io/v2/swagger.json &
wait

# Send tools/list command
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' | dotnet run https://petstore.swagger.io/v2/swagger.json &
wait

exec 3<&-