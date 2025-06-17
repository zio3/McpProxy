#!/usr/bin/env python3
import json
import subprocess
import sys

# Run the OpenAPI-MCP-Proxy and get tools list
proxy_path = "./OpenAPI-MCP-Proxy/bin/Debug/net9.0/OpenAPI-MCP-Proxy.exe"
api_url = "https://raw.githubusercontent.com/spdustin/instructions-mcp-server/refs/heads/main/src/instructions/instructions-openapi.json"

# Initialize
init_msg = {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}
proc = subprocess.Popen([proxy_path, api_url], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
proc.stdin.write(json.dumps(init_msg) + '\n')
proc.stdin.flush()
init_response = proc.stdout.readline()

# Get tools list
tools_msg = {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
proc.stdin.write(json.dumps(tools_msg) + '\n')
proc.stdin.flush()
tools_response = proc.stdout.readline()

# Parse and find postapisearchsingle
try:
    tools_data = json.loads(tools_response)
    if "result" in tools_data and "tools" in tools_data["result"]:
        for tool in tools_data["result"]["tools"]:
            if tool["name"] == "postapisearchsingle":
                print("=== Tool: postapisearchsingle ===")
                print(f"Description: {tool.get('description', 'N/A')}")
                print("\nInput Schema:")
                print(json.dumps(tool.get("inputSchema", {}), indent=2))
                
                # Show example usage
                print("\n=== Expected Arguments Structure ===")
                if "inputSchema" in tool and "properties" in tool["inputSchema"]:
                    props = tool["inputSchema"]["properties"]
                    if "body" in props and "properties" in props["body"]:
                        print("The 'body' parameter expects an object with these properties:")
                        print(json.dumps(props["body"]["properties"], indent=2))
                break
    else:
        print("Error: No tools found in response")
        print(tools_response)
except Exception as e:
    print(f"Error parsing response: {e}")
    print(f"Response: {tools_response}")

proc.terminate()