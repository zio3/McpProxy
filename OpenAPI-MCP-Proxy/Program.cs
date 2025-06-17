using OpenAPI_MCP_Proxy.Services;

namespace OpenAPI_MCP_Proxy;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            Console.Error.WriteLine("Usage: OpenAPI-MCP-Proxy <openapi-spec-url> [tool-prefix]");
            Console.Error.WriteLine("Example: OpenAPI-MCP-Proxy https://petstore.swagger.io/v2/swagger.json");
            Console.Error.WriteLine("Example: OpenAPI-MCP-Proxy https://api.example.com/openapi.json myapp_");
            Environment.Exit(1);
        }

        var openApiUrl = args[0];
        var toolPrefix = args.Length > 1 ? args[1] : "";
        
        // Ensure prefix ends with underscore if not empty
        if (!string.IsNullOrEmpty(toolPrefix) && !toolPrefix.EndsWith("_"))
        {
            toolPrefix += "_";
        }

        // Set console encoding to UTF-8
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Also set the console code page to UTF-8 (65001)
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c chcp 65001",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi)?.WaitForExit();
            }
            catch { /* Ignore if it fails */ }
        }

        try
        {
            // Initialize services
            var openApiService = new OpenApiService(toolPrefix);
            var httpProxyService = new HttpProxyService();
            var mcpService = new McpService(openApiService, httpProxyService);

            // Load OpenAPI specification
            Console.Error.WriteLine($"Loading OpenAPI specification from: {openApiUrl}");
            if (!string.IsNullOrEmpty(toolPrefix))
            {
                Console.Error.WriteLine($"Using tool prefix: {toolPrefix}");
            }
            await openApiService.LoadSpecificationAsync(openApiUrl);
            Console.Error.WriteLine("OpenAPI specification loaded successfully");

            // Run MCP service
            await mcpService.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
