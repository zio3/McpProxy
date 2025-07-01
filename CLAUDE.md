# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 📚 Development Guidelines Reference

### メインガイドライン
- **統合ガイドライン**: https://gist.githubusercontent.com/zio3/20d171adf94bd3311498c5af428da13c/raw/claude-guidelines.md

### 言語別ガイドライン
- **C#開発**: https://gist.githubusercontent.com/zio3/ee07e8930437ca559f211f53824094f4/raw/claude-csharp-guidelines.md

### 外部ガイドライン読み込み方法
外部ガイドラインにアクセスは、MCP CURLを使用してください：

## Project Overview

This repository contains two .NET 9.0 console applications:

### McpProxy
Simple MCP proxy that forwards messages between MCP Inspector and target executables.

### OpenAPI-MCP-Proxy  
OpenAPI-to-MCP proxy that reads OpenAPI v3 documents and exposes REST API endpoints as MCP tools.

## Architecture

### McpProxy Architecture
Simple single-process per request pattern:
- Receives JSON messages from stdin (from MCP Inspector)
- Spawns a new process for each request to execute the target executable
- Forwards the JSON message to the target process
- Returns responses for requests (not notifications) back to stdout
- Automatically kills target processes after handling each message

### OpenAPI-MCP-Proxy Architecture
Service-oriented architecture with clear separation of concerns:
- **OpenApiService**: Downloads and parses OpenAPI v3 documents, generates MCP tool definitions
- **McpService**: Handles MCP protocol messages (initialize, tools/list, tools/call) via stdin/stdout
- **HttpProxyService**: Executes HTTP requests to target APIs and formats responses
- **Models**: MCP message types and data structures

Key architectural decisions:
- Tool generation from OpenAPI operations (using operationId or generating from method+path)
- Parameter mapping: path parameters, query parameters, and request body
- Synchronous message handling with async HTTP requests

## Common Commands

### Build and Run
```bash
# Build the solution
dotnet build McpProxy.sln

# Run in debug mode
dotnet run --project McpProxy/McpProxy.csproj

# Build release version
dotnet build McpProxy.sln -c Release

# Run release build
dotnet run --project McpProxy/McpProxy.csproj -c Release
```

### Usage

#### McpProxy
```bash
# Basic usage pattern
McpProxy.exe <target-executable> [args...]

# Example with arguments
McpProxy.exe MyServer.exe --verbose --config config.json
```

#### OpenAPI-MCP-Proxy
```bash
# Basic usage pattern
OpenAPI-MCP-Proxy.exe <openapi-url>

# Example with Petstore API
OpenAPI-MCP-Proxy.exe https://petstore.swagger.io/v2/swagger.json

# Test with MCP messages
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0.0"}}}' | OpenAPI-MCP-Proxy.exe https://petstore.swagger.io/v2/swagger.json
```

## Code Structure

### McpProxy
- **Program.cs**: Contains the entire application logic
  - Main loop for receiving JSON messages from stdin
  - Process creation and management for target executables  
  - JSON message classification (notification vs request)
  - Response forwarding logic

### OpenAPI-MCP-Proxy
- **Program.cs**: Application entry point with startup logic
- **Models/McpMessages.cs**: MCP protocol message types and JSON serialization
- **Services/OpenApiService.cs**: OpenAPI document parsing and tool generation
- **Services/McpService.cs**: MCP protocol message handling and routing
- **Services/HttpProxyService.cs**: HTTP request execution and response formatting

## Testing

No test framework is currently configured. To add tests, create a new test project:
```bash
dotnet new xunit -n McpProxy.Tests
dotnet sln add McpProxy.Tests/McpProxy.Tests.csproj
```