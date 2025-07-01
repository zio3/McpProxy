using System.Diagnostics;

// コマンドライン引数をチェック
if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: SimpleMcpProxy.exe <target-executable> [args...]");
    Console.Error.WriteLine("Example: SimpleMcpProxy.exe MyServer.exe --verbose --config config.json");
    return;
}

string targetExe = args[0];
string[] targetArgs = args.Length > 1 ? args[1..] : [];

Console.Error.WriteLine($"Simple MCP Proxy started...");
Console.Error.WriteLine($"Target: {targetExe}");
if (targetArgs.Length > 0)
{
    Console.Error.WriteLine($"Args: {string.Join(" ", targetArgs)}");
}

try
{
    while (true)
    {
        // MCP Inspector からの JSON メッセージを受信
        var input = await Console.In.ReadLineAsync();
        if (input == null) break; // EOF

        Console.Error.WriteLine($"Received: {input}");

        // ターゲット .exe を起動
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = targetExe,
                Arguments = string.Join(" ", targetArgs),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        Console.Error.WriteLine("Process started");

        // リクエストを転送
        await process.StandardInput.WriteLineAsync(input);
        await process.StandardInput.FlushAsync();
        Console.Error.WriteLine("StandardInput Flushed");

        // JSON パース（簡易）でnotificationかどうかチェック
        bool isNotification = !input.Contains("\"id\":");

        if (isNotification)
        {
            Console.Error.WriteLine("Notification detected - no response expected");
            // Notificationの場合はレスポンスを待たずにプロセス終了
        }
        else
        {
            // Request/Responseの場合はレスポンスを受信
            var output = await process.StandardOutput.ReadLineAsync();
            Console.Error.WriteLine("StandardOutput ReadLineAsync Flushed");

            Console.Error.WriteLine($"Sending: {output}");

            // MCP Inspector にレスポンスを返す
            if (output != null)
            {
                Console.WriteLine(output);
                await Console.Out.FlushAsync();
            }
        }

        // プロセスを終了
try
{
if (!process.HasExited)
{
process.Kill();
}
await process.WaitForExitAsync();
}
catch (InvalidOperationException)
{
// プロセスが既に終了している場合は無視
Console.Error.WriteLine("Process already exited");
}
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
