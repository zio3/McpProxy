using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using OpenAPI_MCP_Proxy.Models;

namespace OpenAPI_MCP_Proxy.Services;

public class HttpProxyService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpProxyService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout
        _jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public async Task<string> ExecuteRequestAsync(string method, string url, object? body, OperationMode currentMode = OperationMode.Online, Action<OperationMode>? onModeChange = null)
    {
        try
        {
            // If we're in offline mode, first try to check if the server is back online
            if (currentMode == OperationMode.Offline)
            {
                Console.Error.WriteLine($"[DEBUG] Checking server connection: {url}");
                try
                {
                    // Try a simple HEAD or GET request to check connectivity
                    var checkUrl = new Uri(url);
                    var baseUrl = $"{checkUrl.Scheme}://{checkUrl.Host}:{checkUrl.Port}";
                    using var checkRequest = new HttpRequestMessage(HttpMethod.Head, baseUrl);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var checkResponse = await _httpClient.SendAsync(checkRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    
                    // If we reach here, the server is online
                    onModeChange?.Invoke(OperationMode.Online);
                }
                catch
                {
                    // Server is still offline, return offline error
                    return JsonSerializer.Serialize(new
                    {
                        error = new
                        {
                            code = -1,
                            message = "サーバーがオフラインです。WSLやサーバーが起動しているか確認してください。",
                            details = $"接続先: {url}"
                        }
                    });
                }
            }

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        code = (int)response.StatusCode,
                        message = response.ReasonPhrase,
                        details = responseContent
                    }
                });
            }

            // Try to parse as JSON, if fails return as string
            try
            {
                var jsonDoc = JsonDocument.Parse(responseContent);
                return responseContent;
            }
            catch
            {
                // If not JSON, wrap in a simple object
                return JsonSerializer.Serialize(new { result = responseContent });
            }
        }
        catch (HttpRequestException)
        {
            // Switch to offline mode
            onModeChange?.Invoke(OperationMode.Offline);
            
            return JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = -1,
                    message = "サーバーがオフラインです。WSLやサーバーが起動しているか確認してください。",
                    details = $"接続先: {url}"
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Timeout - switch to offline mode
            onModeChange?.Invoke(OperationMode.Offline);
            
            return JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = -1,
                    message = "サーバーがオフラインです。WSLやサーバーが起動しているか確認してください。",
                    details = $"接続先: {url}"
                }
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = -2,
                    message = "Unexpected error",
                    details = ex.Message
                }
            });
        }
    }
}
