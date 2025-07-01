using System.Collections.Generic;

namespace ClaudeBridgeController.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

public class SessionsResponse
{
    public List<Session> Sessions { get; set; } = new();
    public int TotalCount { get; set; }
}