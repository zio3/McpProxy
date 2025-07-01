using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeBridgeController.Models;
using ClaudeBridgeController.Services;

namespace ClaudeBridgeController.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IClipboardService _clipboardService;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<Session> sessions = new();

    [ObservableProperty]
    private Session? selectedSession;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string lastRefreshTime = "Never";

    [ObservableProperty]
    private int sessionCount;

    [ObservableProperty]
    private bool isMonitoringClipboard;

    [ObservableProperty]
    private string clipboardText = string.Empty;

    [ObservableProperty]
    private string clipboardCapturedAt = "N/A";

    [ObservableProperty]
    private bool hasClipboardContent;

    public ICommand RefreshCommand { get; }
    public ICommand SendClipboardCommand { get; }

    public MainViewModel(IApiService apiService, IClipboardService clipboardService)
    {
        _apiService = apiService;
        _clipboardService = clipboardService;

        RefreshCommand = new AsyncRelayCommand(RefreshSessionsAsync);
        SendClipboardCommand = new AsyncRelayCommand<Session>(SendClipboardToSessionAsync);

        _clipboardService.ClipboardChanged += OnClipboardChanged;
        
        // Start with clipboard monitoring enabled
        IsMonitoringClipboard = true;
        
        // Set up auto-refresh timer (every 30 seconds)
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshSessionsAsync();
        _refreshTimer.Start();
    }

    partial void OnIsMonitoringClipboardChanged(bool value)
    {
        if (value)
        {
            _clipboardService.StartMonitoring();
            StatusMessage = "Clipboard monitoring started";
        }
        else
        {
            _clipboardService.StopMonitoring();
            StatusMessage = "Clipboard monitoring stopped";
        }
    }

    private void OnClipboardChanged(object? sender, ClipboardContent content)
    {
        ClipboardText = content.Text;
        ClipboardCapturedAt = content.CapturedAt.ToString("HH:mm:ss");
        HasClipboardContent = content.HasContent;
    }

    private async Task RefreshSessionsAsync()
    {
        try
        {
            StatusMessage = "Refreshing sessions...";
            var sessionList = await _apiService.GetSessionsAsync();
            
            Sessions.Clear();
            foreach (var session in sessionList)
            {
                Sessions.Add(session);
            }

            SessionCount = Sessions.Count;
            LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
            StatusMessage = $"Loaded {SessionCount} sessions";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task SendClipboardToSessionAsync(Session? session)
    {
        if (session == null || string.IsNullOrWhiteSpace(ClipboardText))
        {
            StatusMessage = "No session selected or clipboard is empty";
            return;
        }

        try
        {
            StatusMessage = $"Sending clipboard to session {session.Id}...";
            var success = await _apiService.SendClipboardToSessionAsync(session.Id, ClipboardText);
            
            if (success)
            {
                StatusMessage = $"Successfully sent clipboard content to {session.UserId}";
                
                // Refresh sessions to update message count
                await RefreshSessionsAsync();
            }
            else
            {
                StatusMessage = "Failed to send clipboard content";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task InitializeAsync()
    {
        // Initial load
        await RefreshSessionsAsync();
        
        // Check current clipboard
        var currentText = _clipboardService.GetClipboardText();
        if (!string.IsNullOrEmpty(currentText))
        {
            ClipboardText = currentText;
            ClipboardCapturedAt = DateTime.Now.ToString("HH:mm:ss");
            HasClipboardContent = true;
        }
    }
}