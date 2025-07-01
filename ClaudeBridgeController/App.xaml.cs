using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClaudeBridgeController.Services;
using ClaudeBridgeController.ViewModels;
using ClaudeBridgeController.Views;

namespace ClaudeBridgeController;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddHttpClient<IApiService, ApiService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                
                // Register ViewModels
                services.AddTransient<MainViewModel>();
                
                // Register Views
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Initialize the view model
        if (mainWindow.DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        base.OnExit(e);
    }
}