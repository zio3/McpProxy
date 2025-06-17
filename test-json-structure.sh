#!/bin/bash

# Test script to understand the JSON structure issue

echo "=== Testing JSON Structure for postapisearchsingle ==="
echo
echo "Looking at the OpenAPI spec to understand what's expected..."
echo

# Download and inspect the OpenAPI spec
curl -s "https://raw.githubusercontent.com/spdustin/instructions-mcp-server/refs/heads/main/src/instructions/instructions-openapi.json" | jq '.paths."/api/search/single".post' > api-spec.json

echo "Request body schema for /api/search/single:"
cat api-spec.json | jq '.requestBody.content."application/json".schema'
echo

echo "The request body expects:"
cat api-spec.json | jq '.requestBody.content."application/json".schema.properties'
echo

echo "Required fields:"
cat api-spec.json | jq '.requestBody.content."application/json".schema.required'
echo

# Clean up
rm -f api-spec.json