using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OpenAPI_MCP_Proxy.Services;

public class HttpProxyService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpProxyService()
    {
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public async Task<string> ExecuteRequestAsync(string method, string url, object? body)
    {
        try
        {
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
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = -1,
                    message = "HTTP request failed",
                    details = ex.Message
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
