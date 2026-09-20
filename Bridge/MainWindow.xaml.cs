using Microsoft.Identity.Client;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Bridge.Models;
using Bridge.Services.Authentication;
using Bridge.Services.Infrastructure;
using Bridge.Services.Localization;
using Bridge.Services.Settings;
using Bridge.Services.Translation;
using Bridge.Services.Windows;
using Windows.Graphics;
using Windows.System;

namespace Bridge;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IAuthenticationService _authentication;
    private readonly IStartupService _startupService;
    private readonly ITranslationEngineService _engineService;
    private readonly IDiagnosticLogger _logger;
    private bool _allowClose;
    private bool _isInitializing = true;
    private bool _isUpdatingOnboardingLanguage;

    private static IReadOnlyList<SetupLanguageOption> SetupLanguages { get; } =
    [
        new("es", "Español"),
        new("en", "English"),
        new("pt", "Português")
    ];

    public MainWindow(
        ISettingsService settingsService,
        IAuthenticationService authentication,
        IStartupService startupService,
        ITranslationEngineService engineService,
        IDiagnosticLogger logger)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _authentication = authentication;
        _startupService = startupService;
        _engineService = engineService;
        _logger = logger;

        AppWindow.SetIcon(AppBranding.IconPath);
        AppWindow.Resize(new SizeInt32(840, 900));
        AppWindow.Closing += OnClosing;
        ConfigurationLanguageComboBox.ItemsSource = SetupLanguages;
        SettingsLanguageComboBox.ItemsSource = LanguageDefinition.Supported;
        SettingsEngineComboBox.ItemsSource = _engineService.Engines;
        LoadSettingsIntoControls();
        RefreshAuthenticationState();
        _isInitializing = false;
        _ = RefreshAllEngineUiAsync();

        if (_settingsService.Settings.IsOnboardingCompleted)
        {
            ShowSettingsContent();
        }
    }

    public bool ShouldShowOnStartup => !_settingsService.Settings.IsOnboardingCompleted;

    public void ShowSettings()
    {
        ShowSettingsContent();
        RefreshAuthenticationState();
        _ = RefreshEnginePanelAsync(onboarding: false);
        Activate();
    }

    public void RefreshAuthenticationState()
    {
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(RefreshAuthenticationState);
            return;
        }

        var account = _authentication.GetCurrentAccount();
        var configured = _authentication.IsConfigured;
        CopilotStatusText.Text = !configured
            ? "Status: Publisher configuration required"
            : account is null ? "Status: Not connected" : "Status: Connected";
        CopilotAccountText.Text = account is null ? "Account: —" : $"Account: {account.Username}";
        SettingsSignInButton.Visibility = account is null ? Visibility.Visible : Visibility.Collapsed;
        SettingsSignOutButton.Visibility = account is null ? Visibility.Collapsed : Visibility.Visible;
        SettingsSignInButton.IsEnabled = configured;

        if (!_isInitializing)
        {
            _ = RefreshAllEngineUiAsync();
        }
    }

    public void ClosePermanently()
    {
        _allowClose = true;
        Close();
    }

    private void LoadSettingsIntoControls()
    {
        var settings = _settingsService.Settings;
        var language = LanguageDefinition.FindByCode(settings.PrimaryLanguageCode) ?? LanguageDefinition.Spanish;
        var engine = TranslationEngineDefinition.Find(settings.TranslationEngineId);
        var setupLanguage = SetupLanguages.FirstOrDefault(option => option.Code == settings.ConfigurationLanguageCode)
                            ?? SetupLanguages.First(option => option.Code == "en");
        ConfigurationLanguageComboBox.SelectedItem = setupLanguage;
        ApplyOnboardingLanguage(setupLanguage.Code, language.Code, engine.Id);
        SettingsLanguageComboBox.SelectedItem = language;
        SettingsEngineComboBox.SelectedItem = engine;
        OnboardingGemmaTermsCheckBox.IsChecked = settings.AcceptedGemmaTerms;
        SettingsGemmaTermsCheckBox.IsChecked = settings.AcceptedGemmaTerms;
        StartWithWindowsToggle.IsOn = settings.StartWithWindows;
        FailureNotificationToggle.IsOn = settings.ShowFailureNotifications;
        ThemeComboBox.SelectedIndex = settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
        ApplyTheme(settings.Theme);
    }

    private async void ConfigurationLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || ConfigurationLanguageComboBox.SelectedItem is not SetupLanguageOption selected)
        {
            return;
        }

        _settingsService.Settings.ConfigurationLanguageCode = selected.Code;
        ApplyOnboardingLanguage(selected.Code);
        await _settingsService.SaveAsync();
        await RefreshEnginePanelAsync(onboarding: true);
    }

    private void ApplyOnboardingLanguage(
        string languageCode,
        string? selectedPrimaryLanguageCode = null,
        string? selectedEngineId = null)
    {
        selectedPrimaryLanguageCode ??= GetSelectedLanguage(OnboardingLanguageComboBox).Code;
        selectedEngineId ??= GetSelectedEngine(OnboardingEngineComboBox).Id;

        WelcomeTitleText.Text = OnboardingLocalizer.Text(languageCode, "WelcomeTitle");
        WelcomeDescriptionText.Text = OnboardingLocalizer.Text(languageCode, "WelcomeDescription");
        ConfigurationLanguageComboBox.Header = OnboardingLocalizer.Text(languageCode, "SetupLanguage");
        WelcomeStep1Text.Text = OnboardingLocalizer.Text(languageCode, "Step1");
        WelcomeStep2Text.Text = OnboardingLocalizer.Text(languageCode, "Step2");
        WelcomeStep3Text.Text = OnboardingLocalizer.Text(languageCode, "Step3");
        WelcomeStep4Text.Text = OnboardingLocalizer.Text(languageCode, "Step4");
        WelcomeStep5Text.Text = OnboardingLocalizer.Text(languageCode, "Step5");
        WelcomeContinueButton.Content = OnboardingLocalizer.Text(languageCode, "Continue");
        PrimaryLanguageTitleText.Text = OnboardingLocalizer.Text(languageCode, "PrimaryTitle");
        PrimaryLanguageDescriptionText.Text = OnboardingLocalizer.Text(languageCode, "PrimaryDescription");
        OnboardingLanguageComboBox.Header = OnboardingLocalizer.Text(languageCode, "Language");
        LanguageContinueButton.Content = OnboardingLocalizer.Text(languageCode, "Continue");
        EngineTitleText.Text = OnboardingLocalizer.Text(languageCode, "EngineTitle");
        EngineIntroText.Text = OnboardingLocalizer.Text(languageCode, "EngineIntro");
        OnboardingEngineComboBox.Header = OnboardingLocalizer.Text(languageCode, "Engine");
        OnboardingGemmaTermsCheckBox.Content = OnboardingLocalizer.Text(languageCode, "GemmaAccept");
        OnboardingGemmaTermsLink.Content = OnboardingLocalizer.Text(languageCode, "GemmaRead");
        OnboardingProgressText.Text = OnboardingLocalizer.Text(languageCode, "Preparing");
        OnboardingInstallOllamaButton.Content = OnboardingLocalizer.Text(languageCode, "InstallOllama");
        OnboardingSignInButton.Content = OnboardingLocalizer.Text(languageCode, "SignIn");
        FinishOnboardingButton.Content = OnboardingLocalizer.Text(languageCode, "Finish");

        _isUpdatingOnboardingLanguage = true;
        try
        {
            var languages = LanguageDefinition.Supported
                .Select(language => new LocalizedLanguageOption(
                    language,
                    OnboardingLocalizer.LanguageName(languageCode, language)))
                .ToArray();
            OnboardingLanguageComboBox.ItemsSource = languages;
            OnboardingLanguageComboBox.SelectedItem = languages.FirstOrDefault(option =>
                option.Language.Code == selectedPrimaryLanguageCode) ?? languages[0];

            var engines = _engineService.Engines
                .Select(engine => new LocalizedEngineOption(
                    engine,
                    OnboardingLocalizer.EngineName(languageCode, engine)))
                .ToArray();
            OnboardingEngineComboBox.ItemsSource = engines;
            OnboardingEngineComboBox.SelectedItem = engines.FirstOrDefault(option =>
                option.Engine.Id == selectedEngineId) ?? engines[0];
        }
        finally
        {
            _isUpdatingOnboardingLanguage = false;
        }
    }

    private void ShowSettingsContent()
    {
        OnboardingRoot.Visibility = Visibility.Collapsed;
        SettingsRoot.Visibility = Visibility.Visible;
    }

    private void WelcomeContinue_Click(object sender, RoutedEventArgs e)
    {
        WelcomePanel.Visibility = Visibility.Collapsed;
        PrimaryLanguagePanel.Visibility = Visibility.Visible;
    }

    private void LanguageContinue_Click(object sender, RoutedEventArgs e)
    {
        var language = GetSelectedLanguage(OnboardingLanguageComboBox);
        _settingsService.Settings.PrimaryLanguageCode = language.Code;
        SettingsLanguageComboBox.SelectedItem = language;

        PrimaryLanguagePanel.Visibility = Visibility.Collapsed;
        EnginePanel.Visibility = Visibility.Visible;
        _ = RefreshEnginePanelAsync(onboarding: true);
    }

    private async void OnboardingEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitializing && !_isUpdatingOnboardingLanguage)
        {
            await RefreshEnginePanelAsync(onboarding: true);
        }
    }

    private async void SettingsEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitializing)
        {
            await RefreshEnginePanelAsync(onboarding: false);
        }
    }

    private async Task RefreshAllEngineUiAsync()
    {
        await RefreshEnginePanelAsync(onboarding: true);
        await RefreshEnginePanelAsync(onboarding: false);
    }

    private async Task RefreshEnginePanelAsync(bool onboarding)
    {
        var comboBox = onboarding ? OnboardingEngineComboBox : SettingsEngineComboBox;
        var engine = GetSelectedEngine(comboBox);
        var compatibility = _engineService.Hardware.Evaluate(engine);

        if (onboarding)
        {
            var languageCode = CurrentConfigurationLanguageCode;
            OnboardingEngineDescription.Text = OnboardingLocalizer.EngineDescription(languageCode, engine);
            OnboardingEngineRequirements.Text = OnboardingLocalizer.Requirements(languageCode, engine);
            OnboardingEnginePrivacy.Text = OnboardingLocalizer.EnginePrivacy(languageCode, engine);
            OnboardingHardwareSummary.Text = OnboardingLocalizer.HardwareSummary(languageCode, _engineService.Hardware);
            OnboardingCompatibilityText.Text = OnboardingLocalizer.Compatibility(languageCode, _engineService.Hardware, engine);
            OnboardingGemmaTermsPanel.Visibility = engine.UsesGemma ? Visibility.Visible : Visibility.Collapsed;
            OnboardingInstallOllamaButton.Visibility = engine.UsesGemma ? Visibility.Visible : Visibility.Collapsed;
            OnboardingPrepareEngineButton.Visibility = engine.UsesCopilot ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            SettingsEngineDescription.Text = engine.ShortDescription;
            SettingsEngineRequirements.Text = "Recommended hardware: " + engine.Requirements;
            SettingsEnginePrivacy.Text = engine.PrivacyDescription;
            SettingsHardwareSummary.Text = _engineService.Hardware.Summary;
            SettingsCompatibilityText.Text = compatibility.Message;
            SettingsGemmaTermsPanel.Visibility = engine.UsesGemma ? Visibility.Visible : Visibility.Collapsed;
            SettingsInstallOllamaButton.Visibility = engine.UsesGemma ? Visibility.Visible : Visibility.Collapsed;
            SettingsPrepareEngineButton.Visibility = engine.UsesCopilot ? Visibility.Collapsed : Visibility.Visible;
        }

        TranslationEngineStatus status;
        try
        {
            status = await _engineService.GetStatusAsync(engine.Id);
        }
        catch (Exception exception)
        {
            _logger.Error("Checking translation engine status failed.", exception);
            status = new TranslationEngineStatus(false, "Engine status could not be checked.");
        }

        if (onboarding)
        {
            var languageCode = CurrentConfigurationLanguageCode;
            OnboardingEngineStatusText.Text = OnboardingLocalizer.Status(languageCode, status.Message);
            var account = _authentication.GetCurrentAccount();
            OnboardingSignInButton.Visibility = engine.UsesCopilot && account is null
                ? Visibility.Visible
                : Visibility.Collapsed;
            OnboardingSignInButton.IsEnabled = _authentication.IsConfigured;
            FinishOnboardingButton.Visibility = status.IsReady ? Visibility.Visible : Visibility.Collapsed;
            OnboardingPrepareEngineButton.Content = engine.Id == TranslationEngineDefinition.OfflineStandardId
                ? OnboardingLocalizer.Text(languageCode, "DownloadPack")
                : OnboardingLocalizer.Text(languageCode, "DownloadModel");
        }
        else
        {
            SettingsEngineStatusText.Text = "Status: " + status.Message;
        }
    }

    private async void PrepareOnboardingEngine_Click(object sender, RoutedEventArgs e) =>
        await PrepareEngineAsync(GetSelectedEngine(OnboardingEngineComboBox), onboarding: true);

    private async void PrepareSettingsEngine_Click(object sender, RoutedEventArgs e) =>
        await PrepareEngineAsync(GetSelectedEngine(SettingsEngineComboBox), onboarding: false);

    private async Task PrepareEngineAsync(TranslationEngineDefinition engine, bool onboarding)
    {
        var acceptedTerms = onboarding
            ? OnboardingGemmaTermsCheckBox.IsChecked == true
            : SettingsGemmaTermsCheckBox.IsChecked == true;
        if (engine.UsesGemma && !acceptedTerms)
        {
            ShowStatus("Terms required", "Accept the Gemma Terms of Use before downloading this model.", InfoBarSeverity.Warning);
            return;
        }

        if (engine.UsesGemma)
        {
            _settingsService.Settings.AcceptedGemmaTerms = true;
            OnboardingGemmaTermsCheckBox.IsChecked = true;
            SettingsGemmaTermsCheckBox.IsChecked = true;
        }

        await _settingsService.SaveAsync();
        SetEngineBusy(onboarding, true);
        var progress = new Progress<ModelPreparationProgress>(update => UpdateEngineProgress(onboarding, update));
        try
        {
            await _engineService.PrepareAsync(engine.Id, progress);
            _settingsService.Settings.TranslationEngineId = engine.Id;
            await _settingsService.SaveAsync();
            ShowStatus("Engine ready", $"{engine.DisplayName} is ready for offline translation.", InfoBarSeverity.Success);
        }
        catch (TranslationEngineException exception)
        {
            _logger.Error($"Preparing translation engine failed. Engine={engine.Id}.", exception);
            ShowStatus("Engine not ready", exception.Message, InfoBarSeverity.Error);
        }
        catch (Exception exception)
        {
            _logger.Error($"Preparing translation engine failed. Engine={engine.Id}.", exception);
            ShowStatus("Engine not ready", "The engine could not be prepared. Check the connection and available disk space.", InfoBarSeverity.Error);
        }
        finally
        {
            SetEngineBusy(onboarding, false);
            await RefreshAllEngineUiAsync();
        }
    }

    private void SetEngineBusy(bool onboarding, bool isBusy)
    {
        if (onboarding)
        {
            OnboardingProgressPanel.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            OnboardingPrepareEngineButton.IsEnabled = !isBusy;
            OnboardingInstallOllamaButton.IsEnabled = !isBusy;
            OnboardingEngineComboBox.IsEnabled = !isBusy;
        }
        else
        {
            SettingsEngineProgressPanel.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            SettingsPrepareEngineButton.IsEnabled = !isBusy;
            SettingsInstallOllamaButton.IsEnabled = !isBusy;
            SettingsEngineComboBox.IsEnabled = !isBusy;
        }
    }

    private void UpdateEngineProgress(bool onboarding, ModelPreparationProgress update)
    {
        var progressBar = onboarding ? OnboardingProgress : SettingsEngineProgress;
        var text = onboarding ? OnboardingProgressText : SettingsEngineProgressText;
        progressBar.IsIndeterminate = update.Percent is null;
        if (update.Percent is not null)
        {
            progressBar.Value = update.Percent.Value;
        }

        text.Text = update.Message;
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        if (!_authentication.IsConfigured)
        {
            ShowStatus(
                "Publisher configuration required",
                "Copilot is optional. The app publisher must configure a Microsoft Entra application before it can be used.",
                InfoBarSeverity.Warning);
            return;
        }

        SetAuthenticationBusy(true);
        try
        {
            await _authentication.SignInAsync();
            RefreshAuthenticationState();
            ShowStatus("Connected", "Microsoft 365 Copilot account connected.", InfoBarSeverity.Success);
        }
        catch (MsalServiceException exception)
        {
            _logger.Error($"Authentication service error. MSAL={exception.ErrorCode}.");
            var needsConsent = exception.Message.Contains("AADSTS65001", StringComparison.OrdinalIgnoreCase) ||
                               exception.Message.Contains("consent", StringComparison.OrdinalIgnoreCase);
            ShowStatus(
                "Sign-in failed",
                needsConsent
                    ? "Your organization needs to approve Bridge before Copilot can be used. Offline engines need no approval."
                    : "Microsoft sign-in could not be completed. Try again.",
                InfoBarSeverity.Error);
        }
        catch (MsalException exception)
        {
            _logger.Error($"Authentication failed. MSAL={exception.ErrorCode}.");
            ShowStatus("Sign-in failed", "Microsoft sign-in could not be completed. Try again.", InfoBarSeverity.Error);
        }
        catch (Exception exception)
        {
            _logger.Error("Authentication failed.", exception);
            ShowStatus("Sign-in failed", "Microsoft sign-in could not be completed. Try again.", InfoBarSeverity.Error);
        }
        finally
        {
            SetAuthenticationBusy(false);
            await RefreshAllEngineUiAsync();
        }
    }

    private async void SignOut_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _authentication.SignOutAsync();
            RefreshAuthenticationState();
            ShowStatus("Signed out", "The local Microsoft authentication cache entry was removed.", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            _logger.Error("Sign out failed.", exception);
            ShowStatus("Sign out failed", "Unable to sign out. Try again.", InfoBarSeverity.Error);
        }
    }

    private async void FinishOnboarding_Click(object sender, RoutedEventArgs e)
    {
        var engine = GetSelectedEngine(OnboardingEngineComboBox);
        var status = await _engineService.GetStatusAsync(engine.Id);
        if (!status.IsReady)
        {
            ShowStatus("Engine not ready", status.Message, InfoBarSeverity.Warning);
            return;
        }

        _settingsService.Settings.TranslationEngineId = engine.Id;
        _settingsService.Settings.IsOnboardingCompleted = true;
        await _settingsService.SaveAsync();
        SettingsEngineComboBox.SelectedItem = engine;
        ShowSettingsContent();
        AppWindow.Hide();
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var engine = GetSelectedEngine(SettingsEngineComboBox);
            if (engine.UsesGemma && SettingsGemmaTermsCheckBox.IsChecked != true)
            {
                ShowStatus("Terms required", "Accept the Gemma Terms of Use to select this engine.", InfoBarSeverity.Warning);
                return;
            }

            if (engine.UsesGemma)
            {
                _settingsService.Settings.AcceptedGemmaTerms = true;
            }

            var status = await _engineService.GetStatusAsync(engine.Id);
            if (!status.IsReady)
            {
                ShowStatus("Engine not ready", status.Message, InfoBarSeverity.Warning);
                return;
            }

            if (SettingsLanguageComboBox.SelectedItem is LanguageDefinition language)
            {
                _settingsService.Settings.PrimaryLanguageCode = language.Code;
            }

            _settingsService.Settings.TranslationEngineId = engine.Id;
            _settingsService.Settings.StartWithWindows = StartWithWindowsToggle.IsOn;
            _settingsService.Settings.ShowFailureNotifications = FailureNotificationToggle.IsOn;
            _settingsService.Settings.Theme = GetSelectedTheme();
            _startupService.SetEnabled(StartWithWindowsToggle.IsOn);
            await _settingsService.SaveAsync();
            SelectOnboardingEngine(engine.Id);
            ApplyTheme(_settingsService.Settings.Theme);
            ShowStatus("Settings saved", "Your preferences were saved.", InfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            _logger.Error("Saving application settings failed.", exception);
            ShowStatus("Settings not saved", "Unable to save settings. Try again.", InfoBarSeverity.Error);
        }
    }

    private void SetAuthenticationBusy(bool isBusy)
    {
        OnboardingSignInButton.IsEnabled = !isBusy && _authentication.IsConfigured;
        SettingsSignInButton.IsEnabled = !isBusy && _authentication.IsConfigured;
    }

    private async void OpenGemmaTerms_Click(object sender, RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://ai.google.dev/gemma/terms"));

    private async void OpenOllama_Click(object sender, RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://ollama.com/download/windows"));

    private void ShowStatus(string title, string message, InfoBarSeverity severity)
    {
        WindowInfoBar.Title = title;
        WindowInfoBar.Message = message;
        WindowInfoBar.Severity = severity;
        WindowInfoBar.IsOpen = true;
    }

    private static TranslationEngineDefinition GetSelectedEngine(ComboBox comboBox) =>
        comboBox.SelectedItem switch
        {
            LocalizedEngineOption option => option.Engine,
            TranslationEngineDefinition engine => engine,
            _ => TranslationEngineDefinition.Available[0]
        };

    private static LanguageDefinition GetSelectedLanguage(ComboBox comboBox) =>
        comboBox.SelectedItem switch
        {
            LocalizedLanguageOption option => option.Language,
            LanguageDefinition language => language,
            _ => LanguageDefinition.Spanish
        };

    private string CurrentConfigurationLanguageCode =>
        (ConfigurationLanguageComboBox.SelectedItem as SetupLanguageOption)?.Code
        ?? _settingsService.Settings.ConfigurationLanguageCode;

    private void SelectOnboardingEngine(string engineId)
    {
        if (OnboardingEngineComboBox.ItemsSource is IEnumerable<LocalizedEngineOption> options)
        {
            OnboardingEngineComboBox.SelectedItem = options.FirstOrDefault(option => option.Engine.Id == engineId);
        }
    }

    private string GetSelectedTheme() => ThemeComboBox.SelectedIndex switch
    {
        1 => "Light",
        2 => "Dark",
        _ => "System"
    };

    private void ApplyTheme(string theme) => RootGrid.RequestedTheme = theme switch
    {
        "Light" => ElementTheme.Light,
        "Dark" => ElementTheme.Dark,
        _ => ElementTheme.Default
    };

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        AppWindow.Hide();
    }

    private void Hide_Click(object sender, RoutedEventArgs e) => AppWindow.Hide();

    private sealed record SetupLanguageOption(string Code, string DisplayName);

    private sealed record LocalizedLanguageOption(LanguageDefinition Language, string DisplayName);

    private sealed record LocalizedEngineOption(TranslationEngineDefinition Engine, string DisplayName);
}
