using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClaudeBridgeController.Models;

namespace ClaudeBridgeController.Services;

public interface IClipboardService
{
    event EventHandler<ClipboardContent>? ClipboardChanged;
    ClipboardContent? CurrentContent { get; }
    void StartMonitoring();
    void StopMonitoring();
    string? GetClipboardText();
}

public class ClipboardService : IClipboardService
{
    private Timer? _timer;
    private string? _lastClipboardText;
    private ClipboardContent? _currentContent;

    public event EventHandler<ClipboardContent>? ClipboardChanged;
    public ClipboardContent? CurrentContent => _currentContent;

    public void StartMonitoring()
    {
        // Check clipboard every 500ms
        _timer = new Timer(CheckClipboard, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
    }

    public void StopMonitoring()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CheckClipboard(object? state)
    {
        try
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var text = GetClipboardText();
                if (!string.IsNullOrEmpty(text) && text != _lastClipboardText)
                {
                    _lastClipboardText = text;
                    _currentContent = new ClipboardContent
                    {
                        Text = text,
                        CapturedAt = DateTime.Now,
                        ContentType = "text/plain"
                    };
                    
                    ClipboardChanged?.Invoke(this, _currentContent);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking clipboard: {ex.Message}");
        }
    }

    public string? GetClipboardText()
    {
        try
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                return Clipboard.GetText(TextDataFormat.Text);
            }
            else if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                return Clipboard.GetText(TextDataFormat.UnicodeText);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading clipboard: {ex.Message}");
        }
        
        return null;
    }
}