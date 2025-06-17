#!/usr/bin/env python3
import json
import urllib.request

# Download the OpenAPI spec
url = "https://raw.githubusercontent.com/spdustin/instructions-mcp-server/refs/heads/main/src/instructions/instructions-openapi.json"
with urllib.request.urlopen(url) as response:
    spec = json.loads(response.read())

# Analyze the /api/search/single endpoint
search_endpoint = spec["paths"]["/api/search/single"]["post"]

print("=== /api/search/single POST endpoint ===")
print(f"Summary: {search_endpoint.get('summary', 'N/A')}")
print(f"OperationId: {search_endpoint.get('operationId', 'N/A')}")
print()

# Look at request body
if "requestBody" in search_endpoint:
    request_body = search_endpoint["requestBody"]
    print("Request Body:")
    print(f"  Required: {request_body.get('required', False)}")
    print(f"  Description: {request_body.get('description', 'N/A')}")
    
    if "content" in request_body and "application/json" in request_body["content"]:
        schema = request_body["content"]["application/json"]["schema"]
        print("\n  Schema:")
        print(json.dumps(schema, indent=4))
        
        if "properties" in schema:
            print("\n  Properties:")
            for prop, details in schema["properties"].items():
                print(f"    - {prop}:")
                print(f"        Type: {details.get('type', 'N/A')}")
                print(f"        Description: {details.get('description', 'N/A')}")
                if "required" in schema and prop in schema["required"]:
                    print(f"        Required: True")
                else:
                    print(f"        Required: False")