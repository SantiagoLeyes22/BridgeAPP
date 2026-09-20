using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Bridge.Models;
using Bridge.Services.Infrastructure;
using Bridge.Services.Settings;
using Bridge.Services.Windows;
using Windows.Graphics;
using Windows.System;

namespace Bridge.UI.Overlay;

public sealed partial class TranslationOverlay : Window
{
    private const int OverlayWidth = 440;
    private const int OverlayHeight = 270;
    private readonly IForegroundWindowService _foregroundWindow;
    private readonly ISettingsService _settingsService;
    private bool _allowClose;
    private bool _isUpdatingResultOptions;

    public TranslationOverlay(
        IForegroundWindowService foregroundWindow,
        ISettingsService settingsService)
    {
        InitializeComponent();
        _foregroundWindow = foregroundWindow;
        _settingsService = settingsService;
        ResultTargetLanguageComboBox.ItemsSource = LanguageDefinition.Supported;
        ResultTargetLanguageComboBox.SelectedItem = LanguageDefinition.English;
        ResultStyleComboBox.ItemsSource = TranslationStyleDefinition.Supported;
        ResultStyleComboBox.SelectedItem = TranslationStyleDefinition.Balanced;
        AppWindow.SetIcon(AppBranding.IconPath);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        AppWindow.Closing += OnClosing;
        AppWindow.Resize(new SizeInt32(OverlayWidth, OverlayHeight));
    }

    public event EventHandler? CopyRequested;
    public event EventHandler? ReplaceRequested;
    public event EventHandler<RetranslationOptions>? RetranslateRequested;

    public void ShowLoading(string providerName)
    {
        DirectionText.Text = $"Translating with {providerName}";
        ProviderText.Text = providerName;
        ProviderText.Visibility = Visibility.Visible;
        ResultOptionsPanel.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;
        ResultPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ResultButtons.Visibility = Visibility.Collapsed;
        ErrorCloseButton.Visibility = Visibility.Collapsed;
        ShowNearPointer();
    }

    public void ShowResult(
        TranslationResult result,
        TranslationStyleDefinition style,
        bool nativeStyleSupport)
    {
        DirectionText.Text = $"{result.DetectedLanguage} → {result.TargetLanguage}";
        TranslatedText.Text = result.TranslatedText;
        _isUpdatingResultOptions = true;
        ResultTargetLanguageComboBox.SelectedItem =
            LanguageDefinition.FindByCode(result.TargetLanguageCode) ?? LanguageDefinition.English;
        ResultStyleComboBox.SelectedItem = style;
        ResultStyleComboBox.IsEnabled = true;
        ToolTipService.SetToolTip(
            ResultStyleComboBox,
            nativeStyleSupport
                ? "Translation style"
                : "Natural and Professional use an installed TranslateGemma model.");
        _isUpdatingResultOptions = false;
        ProviderText.Visibility = Visibility.Collapsed;
        ResultOptionsPanel.Visibility = Visibility.Visible;
        LoadingPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ResultButtons.Visibility = Visibility.Visible;
        ErrorCloseButton.Visibility = Visibility.Collapsed;
        ShowNearPointer();
    }

    public void ShowError(string providerName, string message)
    {
        DirectionText.Text = "Translation unavailable";
        ProviderText.Text = providerName;
        ProviderText.Visibility = Visibility.Visible;
        ResultOptionsPanel.Visibility = Visibility.Collapsed;
        ErrorMessage.Text = message;
        LoadingPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ResultButtons.Visibility = Visibility.Collapsed;
        ErrorCloseButton.Visibility = Visibility.Visible;
        ShowNearPointer();
    }

    public void HideOverlay() => AppWindow.Hide();

    public void ClosePermanently()
    {
        _allowClose = true;
        Close();
    }

    private void ShowNearPointer()
    {
        RootGrid.RequestedTheme = _settingsService.Settings.Theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        var (x, y) = _foregroundWindow.GetOverlayPosition(OverlayWidth, OverlayHeight);
        AppWindow.MoveAndResize(new RectInt32(x, y, OverlayWidth, OverlayHeight));
        Activate();
        RootGrid.Focus(FocusState.Programmatic);
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        AppWindow.Hide();
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => CopyRequested?.Invoke(this, EventArgs.Empty);

    private void Replace_Click(object sender, RoutedEventArgs e) => ReplaceRequested?.Invoke(this, EventArgs.Empty);

    private void Close_Click(object sender, RoutedEventArgs e) => HideOverlay();

    private void ResultOption_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_isUpdatingResultOptions &&
            ResultTargetLanguageComboBox.SelectedItem is LanguageDefinition language &&
            ResultStyleComboBox.SelectedItem is TranslationStyleDefinition style)
        {
            RetranslateRequested?.Invoke(this, new RetranslationOptions(language, style));
        }
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            HideOverlay();
            e.Handled = true;
        }
    }
}
