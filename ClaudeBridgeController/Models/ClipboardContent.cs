using System;

namespace ClaudeBridgeController.Models;

public class ClipboardContent
{
    public string Text { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public string ContentType { get; set; } = "text/plain";
    
    public string Preview => Text.Length > 100 ? Text.Substring(0, 100) + "..." : Text;
    public bool HasContent => !string.IsNullOrWhiteSpace(Text);
}