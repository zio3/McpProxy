using System;

namespace ClaudeBridgeController.Models;

public class Session
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalMessages { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;

    // For display purposes
    public string DisplayName => $"{UserId} - {ConversationId.Substring(0, Math.Min(8, ConversationId.Length))}...";
    public string CreatedAtDisplay => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}