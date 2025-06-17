#!/bin/bash

# Focused test script for postapisearchsingle tool call

PROXY_PATH="./OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy.exe"
API_URL="https://raw.githubusercontent.com/spdustin/instructions-mcp-server/refs/heads/main/src/instructions/instructions-openapi.json"

echo "=== Testing postapisearchsingle tool call variations ==="
echo

# Initialize first
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | $PROXY_PATH "$API_URL" > /dev/null 2>&1

# Test different argument structures
echo "Test 1: Direct query property"
echo 'Arguments: {"query":"test search"}'
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"postapisearchsingle","arguments":{"query":"test search"}}}' | $PROXY_PATH "$API_URL" 2>&1
echo
echo "---"
echo

echo "Test 2: Nested in body"
echo 'Arguments: {"body":{"query":"test search"}}'
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"postapisearchsingle","arguments":{"body":{"query":"test search"}}}}' | $PROXY_PATH "$API_URL" 2>&1
echo
echo "---"
echo

echo "Test 3: With requestBody"
echo 'Arguments: {"requestBody":{"query":"test search"}}'
echo '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"postapisearchsingle","arguments":{"requestBody":{"query":"test search"}}}}' | $PROXY_PATH "$API_URL" 2>&1
echo
echo "---"
echo

echo "Test 4: JSON string in body"
echo 'Arguments: {"body":"{\"query\":\"test search\"}"}'
echo '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"postapisearchsingle","arguments":{"body":"{\"query\":\"test search\"}"}}}' | $PROXY_PATH "$API_URL" 2>&1
echo