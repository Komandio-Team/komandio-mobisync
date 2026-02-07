using System.Windows;
using Komandio.Tools.Mobisync.Components.Areas;
using Komandio.Tools.Mobisync.Components.Contracts;
using Komandio.Tools.Mobisync.Components.Dashboard;
using Komandio.Tools.Mobisync.Components.Kills;
using Komandio.Tools.Mobisync.Components.Main;
using Komandio.Tools.Mobisync.Components.Processors;
using Komandio.Tools.Mobisync.Components.SystemIntel;
using Komandio.Tools.Mobisync.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using VehiclesViewModel = Komandio.Tools.Mobisync.Components.Vehicles.VehiclesViewModel;

namespace Komandio.Tools.Mobisync;

public partial class App
{
    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSerilog((serviceProvider, configuration) =>
                {
                    configuration
#if DEBUG
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
#else
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
#endif
                    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Extensions.Http", Serilog.Events.LogEventLevel.Warning)
                    .Filter.ByExcluding(logEvent =>
                        logEvent.RenderMessage().Contains("processing HTTP request") ||
                        logEvent.RenderMessage().Contains("Sending HTTP request") ||
                        logEvent.RenderMessage().Contains("Received HTTP response") ||
                        logEvent.RenderMessage().Contains("End processing HTTP request") ||
                        logEvent.RenderMessage().StartsWith("LOG (VoskAPI:")
                    )
                    .Enrich.FromLogContext()
                    .WriteTo.Console(
                        theme: AnsiConsoleTheme.Sixteen,
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose,
                        applyThemeToRedirectedOutput: true);
                });

                // Services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<ILogMonitorService, LogMonitorService>();

                // Root Shell
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();

                // Feature ViewModels - Made Singletons to ensure state persistence
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<DashboardFeedViewModel>();
                services.AddSingleton<SessionOverviewViewModel>();
                services.AddSingleton<DataSourceViewModel>();
                services.AddSingleton<ReplayConfigViewModel>();
                services.AddSingleton<SystemIntelViewModel>();
                services.AddSingleton<LocationStatViewModel>();
                services.AddSingleton<SecurityStatViewModel>();
                services.AddSingleton<KillsStatViewModel>();
                services.AddSingleton<VehicleStatViewModel>();
                services.AddSingleton<MissionStatViewModel>();

                services.AddSingleton<ProcessorsViewModel>();
                services.AddSingleton<RuleListViewModel>();
                services.AddSingleton<RuleEditorViewModel>();
                services.AddSingleton<RegexGuideViewModel>();

                services.AddSingleton<AreasViewModel>();
                services.AddSingleton<KillsViewModel>();
                services.AddSingleton<VehiclesViewModel>();
                services.AddSingleton<ContractsViewModel>();
                services.AddSingleton<ContractsView>();
            })
            .Build();
    }

    public static IHost? Host { get; private set; }
    public static string[] Args { get; private set; } = Array.Empty<string>();

    protected override async void OnStartup(StartupEventArgs e)
    {
        Args = e.Args;
        // Add global exception handlers to catch crashes during startup/runtime
        System.AppDomain.CurrentDomain.UnhandledException += (s, ev) => 
            HandleGlobalException(ev.ExceptionObject as System.Exception, "AppDomain");
        
        this.DispatcherUnhandledException += (s, ev) => 
        {
            HandleGlobalException(ev.Exception, "Dispatcher");
            ev.Handled = true;
        };

        try
        {
            await Host!.StartAsync();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (System.Exception ex)
        {
            HandleGlobalException(ex, "Startup");
        }

        base.OnStartup(e);
    }

    private bool _isHandlingException;
    private void HandleGlobalException(System.Exception? ex, string source)
    {
        if (_isHandlingException) return;
        _isHandlingException = true;

        var message = $"A critical error occurred ({source}):\n{ex?.Message}\n\nCheck logs for details.";
        Log.Fatal(ex, "Global Exception ({Source}): {Message}", source, ex?.Message);
        
        try
        {
            MessageBox.Show(message, "MobiSync - Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isHandlingException = false;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (Host)
        {
            await Host!.StopAsync();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}