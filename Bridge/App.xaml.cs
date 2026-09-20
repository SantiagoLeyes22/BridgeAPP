using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Bridge.Services.Authentication;
using Bridge.Services.Infrastructure;
using Bridge.Services.Settings;
using Bridge.Services.Translation;
using Bridge.Services.Windows;
using Bridge.UI.Overlay;

namespace Bridge;

public partial class App : Application
{
    private ServiceProvider? _services;
    private MainWindow? _mainWindow;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            WriteStartupFailure(eventArgs.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception."));
        UnhandledException += (_, eventArgs) => WriteStartupFailure(eventArgs.Exception);
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            AppBranding.MigrateLegacyDataDirectory();
            var configuration = AppConfiguration.Load();
            var services = new ServiceCollection();
            services.AddSingleton(configuration);
            services.AddSingleton<IDiagnosticLogger, DiagnosticLogger>();
            services.AddSingleton<HardwareProfileService>();
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<NativeWindowService>();
            services.AddSingleton<IWindowHandleProvider, WindowHandleProvider>();
            services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
            services.AddSingleton<IInputSimulationService, InputSimulationService>();
            services.AddSingleton<IForegroundWindowService, ForegroundWindowService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IStartupService, StartupService>();
            services.AddSingleton<IAuthenticationService, MicrosoftAuthenticationService>();
            services.AddSingleton<OfflineLanguageDetector>();
            services.AddHttpClient<CopilotApiClient>(client =>
            {
                client.BaseAddress = new Uri(configuration.Copilot.GraphBaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(10, configuration.Copilot.RequestTimeoutSeconds));
            });
            services.AddHttpClient("FirefoxModels", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Bridge/2.0");
            });
            services.AddHttpClient("Ollama", client =>
            {
                client.BaseAddress = new Uri("http://127.0.0.1:11434/", UriKind.Absolute);
                client.Timeout = Timeout.InfiniteTimeSpan;
            });
            services.AddSingleton(provider => new FirefoxModelManager(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("FirefoxModels"),
                provider.GetRequiredService<IDiagnosticLogger>()));
            services.AddSingleton(provider => new OllamaService(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama"),
                provider.GetRequiredService<IDiagnosticLogger>()));
            services.AddSingleton<FirefoxOfflineTranslationProvider>();
            services.AddSingleton<OllamaTranslationProvider>();
            services.AddSingleton<Microsoft365CopilotTranslationProvider>();
            services.AddSingleton<ITranslationEngineService, TranslationEngineService>();
            services.AddSingleton<TranslationProviderRouter>();
            services.AddSingleton<ITranslationProvider>(provider =>
                provider.GetRequiredService<TranslationProviderRouter>());
            services.AddSingleton<TranslationOverlay>();
            services.AddSingleton<TranslationCoordinator>();
            services.AddSingleton<ApplicationController>();
            services.AddSingleton<MainWindow>();
            _services = services.BuildServiceProvider();

            await _services.GetRequiredService<ISettingsService>().LoadAsync();

            _mainWindow = _services.GetRequiredService<MainWindow>();
            _services.GetRequiredService<IWindowHandleProvider>().SetMainWindow(_mainWindow);
            var controller = _services.GetRequiredService<ApplicationController>();
            controller.ExitRequested += OnExitRequested;
            if (_mainWindow.ShouldShowOnStartup)
            {
                _mainWindow.Activate();
            }

            await controller.StartAsync(_mainWindow);
        }
        catch (Exception exception)
        {
            WriteStartupFailure(exception);
            throw;
        }
    }

    private static void WriteStartupFailure(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Bridge",
                "Logs");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(
                Path.Combine(logDirectory, "startup-crash.log"),
                $"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Startup diagnostics must never hide the original failure.
        }
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        if (_services is null)
        {
            return;
        }

        _services.GetRequiredService<TranslationOverlay>().ClosePermanently();
        _mainWindow?.ClosePermanently();
        _services.Dispose();
        _services = null;
        Exit();
    }
}
