#!/bin/bash

echo "=== Analyzing OpenAPI Schema Structure ==="

# Download and analyze the OpenAPI spec
curl -s https://instructionstore20250614211800-hhfbe3dgaje7cfhr.japaneast-01.azurewebsites.net/swagger/v1/swagger.json | \
python3 << 'EOF'
import json
import sys

data = json.load(sys.stdin)

# Look for the search/single endpoint
for path, methods in data.get('paths', {}).items():
    if '/search/single' in path:
        print(f"Path: {path}")
        for method, operation in methods.items():
            if method == 'post':
                print(f"Method: {method}")
                print(f"OperationId: {operation.get('operationId', 'N/A')}")
                
                if 'requestBody' in operation:
                    rb = operation['requestBody']
                    print("\nRequest Body:")
                    print(f"  Required: {rb.get('required', False)}")
                    
                    if 'content' in rb:
                        for content_type, content in rb['content'].items():
                            print(f"  Content Type: {content_type}")
                            if 'schema' in content:
                                schema = content['schema']
                                print(f"  Schema Type: {schema.get('type', 'N/A')}")
                                if 'properties' in schema:
                                    print(f"  Properties Count: {len(schema['properties'])}")
                                    print("  Properties:")
                                    for prop_name, prop_schema in schema['properties'].items():
                                        print(f"    - {prop_name}: {prop_schema.get('type', 'N/A')}")
                                        if prop_name == 'query' and 'properties' in prop_schema:
                                            print(f"      (has {len(prop_schema['properties'])} sub-properties)")
                                else:
                                    print("  No properties in schema")
                                    print(f"  Schema: {json.dumps(schema, indent=4)}")
EOF