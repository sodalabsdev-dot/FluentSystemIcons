using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;
using Microsoft.Windows.ApplicationModel.Resources;
using FluentSystemIcons.Generator.Services;

namespace FluentSystemIcons.Generator
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private AppWindow _appWindow;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private readonly ResourceLoader _resourceLoader = new ResourceLoader();

        private const string GitHubBaseUrl = "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/fonts/";

        private string? _latestGitHubVersion;
        private string? _localVersion;
        private bool _isWindowLoaded = false;
        private string _themeButtonToolTip = string.Empty;

        private const byte HoverOpacity = 0x15;
        private const byte PressedOpacity = 0x22;

        private RectInt32 _normalBounds = new RectInt32(0, 0, 1100, 750);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string ThemeButtonToolTip
        {
            get => _themeButtonToolTip;
            set
            {
                if (_themeButtonToolTip != value)
                {
                    _themeButtonToolTip = value;
                    OnPropertyChanged(nameof(ThemeButtonToolTip));
                }
            }
        }

        private string GetLocalizedText(string key, string fallback)
        {
            try
            {
                string text = _resourceLoader.GetString(key);
                return string.IsNullOrEmpty(text) ? fallback : text;
            }
            catch
            {
                return fallback;
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();

            // Cargar título de ventana usando el wrapper tolerante a excepciones NamedResource
            this.Title = GetLocalizedText("MainWindow_Title", "Fluent System Icons - SDK Generator");

            var windowHandle = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Closing += AppWindow_Closing;
            _appWindow.Changed += AppWindow_Changed;

            InitializeWindowLayout();

            var theme = SettingsService.LoadTheme();
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }

            var backdrop = SettingsService.LoadBackdrop();
            SetSystemBackdrop(backdrop);

            UpdateThemeButtonToolTip();

            SettingsFlyoutContent.InitializeTheme(theme);
            SettingsFlyoutContent.InitializeBackdrop(backdrop);
            SettingsFlyoutContent.InitializeLanguage(SettingsService.LoadLanguage());

            SettingsFlyoutContent.ThemeChanged += OnThemeChangedFromSettings;
            SettingsFlyoutContent.BackdropChanged += OnBackdropChangedFromSettings;
            SettingsFlyoutContent.LanguageChanged += OnLanguageChangedFromSettings;

            SetTitleBarColors();
            UpdateRootBackground();

            AppTitleBar.Loaded += AppTitleBar_Loaded;
            AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
            ExtendsContentIntoTitleBar = true;

            Activated += MainWindow_Activated;

            var detectedPath = DetectWinUIProjectPath();
            if (!string.IsNullOrEmpty(detectedPath))
            {
                WinUIPathTextBox.Text = detectedPath;
                AppendLog(string.Format(GetLocalizedText("Log_WinUIPathDetected", "[Info] Ruta de proyecto WinUI detectada automáticamente: {0}"), detectedPath));

                string defaultPackagesPath = Path.Combine(detectedPath, "LocalPackages");
                NuGetOutputPathTextBox.Text = defaultPackagesPath;
            }
            else
            {
                AppendLog(GetLocalizedText("Log_WinUIPathWarning", "[Advertencia] No se pudo encontrar el proyecto FluentSystemIcons.WinUI automáticamente. Selecciónelo de forma manual."));
            }

            InitializeLocalVersion();
            UpdateMainButtonText();

            _isWindowLoaded = true;
        }

        private void InitializeWindowLayout()
        {
            var savedLayout = SettingsService.LoadWindowLayout();
            if (savedLayout.HasValue)
            {
                _appWindow.MoveAndResize(savedLayout.Value);
                _normalBounds = savedLayout.Value;
            }
            else
            {
                _appWindow.Resize(new SizeInt32(1100, 750));
                _normalBounds = new RectInt32(_appWindow.Position.X, _appWindow.Position.Y, 1100, 750);
            }

            if (SettingsService.LoadIsMaximized())
            {
                if (_appWindow.Presenter is OverlappedPresenter overlappedPresenter)
                {
                    overlappedPresenter.Maximize();
                }
            }
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            SettingsService.SaveWindowLayout(_appWindow, _normalBounds);
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange || args.DidSizeChange)
            {
                if (sender.Presenter is OverlappedPresenter overlappedPresenter)
                {
                    if (overlappedPresenter.State == OverlappedPresenterState.Restored)
                    {
                        _normalBounds = new RectInt32(sender.Position.X, sender.Position.Y, sender.Size.Width, sender.Size.Height);
                    }
                }
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                SetTitleBarColors();
            }
        }

        private void SetTitleBarColors()
        {
            if (_appWindow == null) return;

            var titleBar = _appWindow.TitleBar;
            if (this.Content is not FrameworkElement rootElement) return;
            var theme = rootElement.ActualTheme;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            if (theme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(HoverOpacity, 0xFF, 0xFF, 0xFF);
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(PressedOpacity, 0xFF, 0xFF, 0xFF);
            }
            else
            {
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(HoverOpacity, 0x00, 0x00, 0x00);
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(PressedOpacity, 0x00, 0x00, 0x00);
            }
        }

        private void UpdateRootBackground()
        {
            var backdrop = SettingsService.LoadBackdrop();

            if (backdrop == BackdropType.None)
            {
                var theme = SettingsService.LoadTheme();
                ElementTheme finalTheme = theme;

                if (theme == ElementTheme.Default)
                {
                    finalTheme = (this.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;
                }

                RootGrid.Background = (SolidColorBrush)Application.Current.Resources[
                    finalTheme == ElementTheme.Dark
                        ? "SolidBackgroundBrushDark"
                        : "SolidBackgroundBrushLight"
                ];
            }
            else
            {
                RootGrid.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }

        private void ChangeTheme(ElementTheme newTheme)
        {
            if (this.Content is FrameworkElement rootElement && rootElement.RequestedTheme != newTheme)
            {
                rootElement.RequestedTheme = newTheme;
                SettingsService.SaveTheme(newTheme);
                UpdateThemeButtonToolTip();
                SettingsFlyoutContent.InitializeTheme(newTheme);
                UpdateRootBackground();
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    SetTitleBarColors();
                });
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Content is FrameworkElement rootElement)
            {
                var newTheme = rootElement.ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
                ChangeTheme(newTheme);
            }
        }

        private void UpdateThemeButtonToolTip()
        {
            if (this.Content is FrameworkElement rootElement)
            {
                var resourceKey = rootElement.ActualTheme == ElementTheme.Dark
                    ? "ThemeButton_Dark_ToolTip"
                    : "ThemeButton_Light_ToolTip";
                var fallbackText = rootElement.ActualTheme == ElementTheme.Dark
                    ? "Cambiar a Tema Claro"
                    : "Cambiar a Tema Oscuro";

                ThemeButtonToolTip = GetLocalizedText(resourceKey, fallbackText);
            }
        }

        private void OnThemeChangedFromSettings(ElementTheme newTheme)
        {
            ChangeTheme(newTheme);
        }

        private void OnBackdropChangedFromSettings(BackdropType newBackdrop)
        {
            SetSystemBackdrop(newBackdrop);
            SettingsService.SaveBackdrop(newBackdrop);
        }

        private async void OnLanguageChangedFromSettings(string newLang)
        {
            var currentLang = SettingsService.LoadLanguage();
            if (currentLang != newLang)
            {
                SettingsService.SaveLanguage(newLang);
                SettingsButton.Flyout.Hide();

                try
                {
                    var dialog = new ContentDialog
                    {
                        Title = GetLocalizedText("Settings_RestartRequired_Title", "Reinicio Requerido"),
                        Content = GetLocalizedText("Settings_RestartRequired_Message", "Por favor, reinicie la aplicación para aplicar el cambio de idioma."),
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                catch
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Reinicio Requerido",
                        Content = "Por favor, reinicie la aplicación para aplicar el cambio de idioma.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private void SetSystemBackdrop(BackdropType type)
        {
            this.SystemBackdrop = null;

            switch (type)
            {
                case BackdropType.Mica:
                    try { this.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base }; } catch { }
                    break;
                case BackdropType.MicaAlt:
                    try { this.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt }; } catch { }
                    break;
                case BackdropType.Acrylic:
                    try { this.SystemBackdrop = new DesktopAcrylicBackdrop(); } catch { }
                    break;
                case BackdropType.None:
                default:
                    break;
            }

            this.DispatcherQueue.TryEnqueue(() =>
            {
                UpdateRootBackground();
            });
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            SetRegionsForCustomTitleBar();
            AppTitleBar.Opacity = 1.0;
            AppTitleBar.Visibility = Visibility.Visible;
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetRegionsForCustomTitleBar();
        }

        private void SetRegionsForCustomTitleBar()
        {
            if (ExtendsContentIntoTitleBar && AppTitleBar.XamlRoot != null)
            {
                double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;
                RightPaddingColumn.Width = new GridLength(_appWindow.TitleBar.RightInset / scaleAdjustment);
                var rects = new List<RectInt32>
                {
                    GetRect(AppTitlePanel, scaleAdjustment),
                    GetRect(ThemeButton, scaleAdjustment),
                    GetRect(SettingsButton, scaleAdjustment)
                };
                InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id).SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
            }
        }

        private RectInt32 GetRect(FrameworkElement element, double scale)
        {
            GeneralTransform transform = element.TransformToVisual(null);
            Rect bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            return new RectInt32((int)Math.Round(bounds.X * scale), (int)Math.Round(bounds.Y * scale), (int)Math.Round(bounds.Width * scale), (int)Math.Round(bounds.Height * scale));
        }

        private string? DetectWinUIProjectPath()
        {
            try
            {
                var currentDir = AppContext.BaseDirectory;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    var targetPath = Path.Combine(currentDir, "FluentSystemIcons.WinUI");
                    if (Directory.Exists(targetPath))
                    {
                        return targetPath;
                    }
                    currentDir = Path.GetDirectoryName(currentDir);
                }
            }
            catch
            {
                // Previene caídas por restricciones de seguridad fuera del entorno de Visual Studio
            }
            return null;
        }

        private void InitializeLocalVersion()
        {
            _localVersion = SettingsService.LoadLastSyncVersion();

            if (LocalVersionRun != null)
            {
                LocalVersionRun.Text = _localVersion;
            }

            if (VersionUpdateInfoBar != null)
            {
                VersionUpdateInfoBar.Title = GetLocalizedText("Status_Initial_Title", "Información del Catálogo");
                VersionUpdateInfoBar.Message = string.Format(GetLocalizedText("Status_Initial_Message", "Versión local instalada: {0}. Presione 'Verificar versión' para comprobar si hay actualizaciones."), _localVersion);
                VersionUpdateInfoBar.Severity = InfoBarSeverity.Informational;
                VersionUpdateInfoBar.IsOpen = true;
            }
        }

        private void AppendLog(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            LogTextBlock.Text = _logBuilder.ToString();
            LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
        }

        private async void BrowseWinUIPath_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                WinUIPathTextBox.Text = folder.Path;
                AppendLog(string.Format(GetLocalizedText("Log_DestPathUpdated", "[Info] Ruta de destino actualizada: {0}"), folder.Path));

                string defaultPackagesPath = Path.Combine(folder.Path, "LocalPackages");
                NuGetOutputPathTextBox.Text = defaultPackagesPath;
            }
        }

        private async void BrowseNuGetOutputPath_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                NuGetOutputPathTextBox.Text = folder.Path;
                AppendLog(string.Format(GetLocalizedText("Log_NuGetPathUpdated", "[Info] Ruta del paquete NuGet actualizada: {0}"), folder.Path));
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string winUIPath = WinUIPathTextBox.Text.Trim();
            if (string.IsNullOrEmpty(winUIPath))
            {
                AppendLog(GetLocalizedText("Log_ErrorWinUIPathEmpty", "[Error] La ruta de proyecto WinUI especificada no puede estar vacía."));
                return;
            }

            string namespaceName = NamespaceTextBox.Text.Trim();
            if (string.IsNullOrEmpty(namespaceName))
            {
                AppendLog(GetLocalizedText("Log_ErrorNamespaceEmpty", "[Error] El nombre del Namespace no puede estar vacío."));
                return;
            }

            string nugetOutputPath = NuGetOutputPathTextBox.Text.Trim();
            if (PackNuGetCheckBox.IsChecked == true && string.IsNullOrEmpty(nugetOutputPath))
            {
                AppendLog(GetLocalizedText("Log_ErrorNuGetPathEmpty", "[Error] Debe especificar una ruta de salida válida para el paquete NuGet."));
                return;
            }

            GenerateButton.IsEnabled = false;
            GenerationProgressBar.Visibility = Visibility.Visible;
            ProgressStatusTextBlock.Visibility = Visibility.Visible;
            GenerationProgressBar.Value = 0;
            _logBuilder.Clear();
            LogTextBlock.Text = string.Empty;

            try
            {
                AppendLog(GetLocalizedText("Log_StartSync", "🚀 Iniciando proceso de compilación y sincronización de Fluent System Icons..."));

                EnsureWinUIProjectExists(winUIPath);

                string tempFontsFolder = Path.Combine(Path.GetTempPath(), "FluentIcons_Temp");
                if (Directory.Exists(tempFontsFolder))
                {
                    Directory.Delete(tempFontsFolder, true);
                }
                Directory.CreateDirectory(tempFontsFolder);

                string targetTtfFolder = Path.Combine(winUIPath, "Assets", "Fonts");
                string targetCsFolder = Path.Combine(winUIPath, "Symbols");
                string targetXamlFile = Path.Combine(winUIPath, "Resources", "FluentIcons.xaml");

                Directory.CreateDirectory(targetTtfFolder);
                Directory.CreateDirectory(targetCsFolder);

                if (AutoDownloadCheckBox.IsChecked == true)
                {
                    await DownloadStylesheetsAndFontsAsync(tempFontsFolder);
                }
                else
                {
                    AppendLog(GetLocalizedText("Log_GitHubDownloadDisabled", "[Info] Descarga por GitHub desactivada. Buscando archivos locales .css en el directorio actual..."));
                    string currentDir = Directory.GetCurrentDirectory();
                    foreach (var file in Directory.GetFiles(currentDir, "*.css"))
                    {
                        File.Copy(file, Path.Combine(tempFontsFolder, Path.GetFileName(file)), true);
                    }
                    foreach (var file in Directory.GetFiles(currentDir, "*.ttf"))
                    {
                        File.Copy(file, Path.Combine(tempFontsFolder, Path.GetFileName(file)), true);
                    }
                }

                string[] cssFiles = Directory.GetFiles(tempFontsFolder, "*.css");
                if (cssFiles.Length == 0)
                {
                    AppendLog(GetLocalizedText("Log_ErrorNoCssFound", "[Error] No se encontraron archivos de hojas de estilo .css para procesar."));
                    return;
                }

                UpdateProgress(40, GetLocalizedText("Progress_AnalyzingCss", "Analizando hojas de estilo y generando clases constantes C#..."));

                foreach (var cssFile in cssFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(cssFile);

                    if (fileName.Contains("Resizable", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ResizableCheckBox.IsChecked == true)
                        {
                            AppendLog(string.Format(GetLocalizedText("Log_ProcessingResizable", "[Procesando] Sincronizando estilo dinámico: {0}"), fileName));
                            ProcessResizable(cssFile, targetCsFolder, namespaceName);
                        }
                    }
                    else
                    {
                        if (fileName.Contains("Regular", StringComparison.OrdinalIgnoreCase) && RegularCheckBox.IsChecked == true)
                        {
                            AppendLog(string.Format(GetLocalizedText("Log_ProcessingRegular", "[Procesando] Sincronizando estilo Regular: {0}"), fileName));
                            ProcessCss(cssFile, targetCsFolder, fileName.Replace("-", ""), "_regular", namespaceName);
                        }
                        else if (fileName.Contains("Filled", StringComparison.OrdinalIgnoreCase) && FilledCheckBox.IsChecked == true)
                        {
                            AppendLog(string.Format(GetLocalizedText("Log_ProcessingFilled", "[Procesando] Sincronizando estilo Filled: {0}"), fileName));
                            ProcessCss(cssFile, targetCsFolder, fileName.Replace("-", ""), "_filled", namespaceName);
                        }
                        else if (fileName.Contains("Light", StringComparison.OrdinalIgnoreCase) && LightCheckBox.IsChecked == true)
                        {
                            AppendLog(string.Format(GetLocalizedText("Log_ProcessingLight", "[Procesando] Sincronizando estilo Light: {0}"), fileName));
                            ProcessCss(cssFile, targetCsFolder, fileName.Replace("-", ""), "_light", namespaceName);
                        }
                    }
                }

                UpdateProgress(80, GetLocalizedText("Progress_SyncingTtf", "Sincronizando fuentes vectoriales TrueType (.ttf)..."));
                string[] ttfFiles = Directory.GetFiles(tempFontsFolder, "*.ttf");
                foreach (var ttfFile in ttfFiles)
                {
                    string targetTtfPath = Path.Combine(targetTtfFolder, Path.GetFileName(ttfFile));
                    File.Copy(ttfFile, targetTtfPath, true);
                    AppendLog(string.Format(GetLocalizedText("Log_TtfMoved", "[Sincronizado] Fuente movida con éxito: {0}"), Path.GetFileName(ttfFile)));
                }

                UpdateProgress(90, GetLocalizedText("Progress_UpdatingXaml", "Actualizando diccionario de recursos XAML (FluentIcons.xaml)..."));
                RegenerateResourcesDictionary(targetXamlFile, ttfFiles.Select(x => Path.GetFileName(x)!).ToList());

                bool nugetSuccess = true;
                if (PackNuGetCheckBox.IsChecked == true)
                {
                    nugetSuccess = await PackNuGetProjectAsync(winUIPath, nugetOutputPath);
                }

                if (AutoDownloadCheckBox.IsChecked == true && !string.IsNullOrEmpty(_latestGitHubVersion) && nugetSuccess)
                {
                    SettingsService.SaveLastSyncVersion(_latestGitHubVersion);
                    _localVersion = _latestGitHubVersion;

                    if (LocalVersionRun != null)
                    {
                        LocalVersionRun.Text = _localVersion;
                    }

                    if (VersionUpdateInfoBar != null)
                    {
                        VersionUpdateInfoBar.Title = GetLocalizedText("Status_CatalogUpToDate_Title", "Catálogo al día");
                        VersionUpdateInfoBar.Message = string.Format(GetLocalizedText("Status_CatalogUpToDate_Message", "Su catálogo local ha sido sincronizado, compilado y empaquetado en la versión {0}."), _localVersion);
                        VersionUpdateInfoBar.Severity = InfoBarSeverity.Success;
                    }
                }
                else if (nugetSuccess)
                {
                    if (VersionUpdateInfoBar != null)
                    {
                        VersionUpdateInfoBar.Title = GetLocalizedText("Status_GenCompleted_Title", "Generación completada");
                        VersionUpdateInfoBar.Message = string.Format(GetLocalizedText("Status_GenCompleted_Message", "Los recursos locales se han generado y empaquetado correctamente para la versión {0}."), _localVersion);
                        VersionUpdateInfoBar.Severity = InfoBarSeverity.Success;
                    }
                }
                else
                {
                    if (VersionUpdateInfoBar != null)
                    {
                        VersionUpdateInfoBar.Title = GetLocalizedText("Status_PackError_Title", "Error de Empaquetado");
                        VersionUpdateInfoBar.Message = GetLocalizedText("Status_PackError_Message", "Los recursos fueron generados, pero falló la compilación del paquete NuGet. Revise la consola.");
                        VersionUpdateInfoBar.Severity = InfoBarSeverity.Error;
                    }
                }

                if (VersionUpdateInfoBar != null)
                {
                    VersionUpdateInfoBar.ActionButton = null;
                }
                UpdateMainButtonText();

                UpdateProgress(100, GetLocalizedText("Progress_Completed", "¡Operación completada!"));
                AppendLog(GetLocalizedText("Log_SuccessFull", "\n✅✅✅ Generación y Sincronización finalizada con éxito. ✅✅✅"));
                AppendLog(GetLocalizedText("Log_SuccessIndexed", "[Info] Los archivos .ttf y las clases estáticas .cs se encuentran indexadas en su proyecto de biblioteca."));
            }
            catch (Exception ex)
            {
                AppendLog(string.Format(GetLocalizedText("Log_CriticalError", "[Error Crítico] Ocurrió una anomalía durante la sincronización: {0}"), ex.Message));
            }
            finally
            {
                GenerateButton.IsEnabled = true;
                GenerationProgressBar.Visibility = Visibility.Collapsed;
                ProgressStatusTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void EnsureWinUIProjectExists(string winUIPath)
        {
            if (!Directory.Exists(winUIPath))
            {
                Directory.CreateDirectory(winUIPath);
            }

            string csprojPath = Path.Combine(winUIPath, "FluentSystemIcons.WinUI.csproj");
            if (!File.Exists(csprojPath))
            {
                AppendLog(GetLocalizedText("Log_RecreatingCsproj", "[Info] El proyecto de biblioteca FluentSystemIcons.WinUI.csproj no existe en la ruta seleccionada. Recreándolo de forma autónoma..."));

                Directory.CreateDirectory(Path.Combine(winUIPath, "Assets", "Fonts"));
                Directory.CreateDirectory(Path.Combine(winUIPath, "Resources"));
                Directory.CreateDirectory(Path.Combine(winUIPath, "Symbols"));

                string csprojTemplate = GetWinUIProjectTemplateContent();
                File.WriteAllText(csprojPath, csprojTemplate, Encoding.UTF8);
                AppendLog(GetLocalizedText("Log_CsprojRecreated", "[XAML] Estructura de proyecto y archivo FluentSystemIcons.WinUI.csproj restablecidos con éxito."));
            }
        }

        private string GetWinUIProjectTemplateContent()
        {
            return @"<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
		<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
		<RootNamespace>FluentSystemIcons.WinUI</RootNamespace>
		<RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
		<UseWinUI>true</UseWinUI>
		<Platforms>AnyCPU;x64;x86;ARM32;ARM64</Platforms>

		<!-- Habilita la estructuración de carpetas de biblioteca recomendada para WinUI 3 -->
		<GenerateLibraryLayout>true</GenerateLibraryLayout>

		<!-- Configuración para enlazar nuestro target personalizado durante la compilación del paquete NuGet -->
		<TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);PackWinUIAssets</TargetsForTfmSpecificContentInPackage>
	</PropertyGroup>

	<!-- Remueve las declaraciones predeterminadas del explorador de soluciones para evitar conflictos -->
	<ItemGroup>
		<None Remove=""Assets\Fonts\FluentSystemIcons-Filled.css"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Filled.json"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Filled.ttf"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Light.css"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Light.json"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Light.ttf"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Regular.css"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Regular.json"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Regular.ttf"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Resizable.css"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Resizable.json"" />
		<None Remove=""Assets\Fonts\FluentSystemIcons-Resizable.ttf"" />
		<None Remove=""Resources\FluentIcons.xaml"" />
	</ItemGroup>

	<!-- Grupo consolidado: Declara los archivos y les asigna su propiedad de copia simultáneamente -->
	<ItemGroup>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Filled.css"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Filled.json"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Filled.ttf"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Light.css"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Light.json"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Light.ttf"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Regular.css"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Regular.json"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Regular.ttf"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Resizable.css"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Resizable.json"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
		<Content Include=""Assets\Fonts\FluentSystemIcons-Resizable.ttf"">
			<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
		</Content>
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include=""Microsoft.Windows.SDK.BuildTools"" Version=""10.0.28000.1839"" />
		<PackageReference Include=""Microsoft.WindowsAppSDK"" Version=""2.2.0"" />
	</ItemGroup>

	<ItemGroup>
		<Page Update=""Resources\FluentIcons.xaml"">
			<Generator>MSBuild:Compile</Generator>
		</Page>
	</ItemGroup>

	<!-- Target personalizado para empaquetar de forma estructurada los recursos dentro de lib -->
	<Target Name=""PackWinUIAssets"">
		<ItemGroup>
			<!-- Incluye el archivo binario compilado de XAML (.xbf) desde la ruta de salida de compilación -->
			<TfmSpecificPackageFile Include=""$(OutputPath)\FluentSystemIcons.WinUI\Resources\FluentIcons.xbf"">
				<PackagePath>lib\net8.0-windows10.0.19041\FluentSystemIcons.WinUI\Resources\</PackagePath>
			</TfmSpecificPackageFile>

			<!-- Incluye todas las fuentes (.ttf), hojas de estilo (.css) y metadatos (.json) desde la carpeta del proyecto -->
			<TfmSpecificPackageFile Include=""Assets\Fonts\**\*.*"">
				<PackagePath>lib\net8.0-windows10.0.19041\FluentSystemIcons.WinUI\Assets\Fonts\</PackagePath>
			</TfmSpecificPackageFile>
		</ItemGroup>
	</Target>

</Project>";
        }

        private async Task<bool> PackNuGetProjectAsync(string winUIProjectPath, string outputPath)
        {
            AppendLog(GetLocalizedText("Log_NuGetStart", "\n[NuGet] Iniciando empaquetado automático de FluentSystemIcons.WinUI..."));
            UpdateProgress(95, GetLocalizedText("Progress_PackingNuGet", "Compilando y generando paquete NuGet (.nupkg) en segundo plano..."));

            string csprojPath = Path.Combine(winUIProjectPath, "FluentSystemIcons.WinUI.csproj");
            if (!File.Exists(csprojPath))
            {
                AppendLog(string.Format(GetLocalizedText("Log_ErrorNoCsproj", "[Error] No se encontró el archivo de proyecto en la ruta absoluta especificada: '{0}'. Verifique la ruta."), csprojPath));
                return false;
            }

            string packageVersion = "1.0.0";
            if (UseCatalogVersionCheckBox.IsChecked == true && !string.IsNullOrEmpty(_latestGitHubVersion))
            {
                packageVersion = _latestGitHubVersion;
            }
            else if (NuGetVersionTextBox != null && !string.IsNullOrEmpty(NuGetVersionTextBox.Text))
            {
                packageVersion = NuGetVersionTextBox.Text.Trim();
            }

            try
            {
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"pack \"{csprojPath}\" -p:PackageVersion={packageVersion} -c Release -o \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    var outputTask = Task.Run(async () =>
                    {
                        using (var reader = process.StandardOutput)
                        {
                            while (!reader.EndOfStream)
                            {
                                string? line = await reader.ReadLineAsync();
                                if (line != null)
                                {
                                    this.DispatcherQueue.TryEnqueue(() => AppendLog($"[dotnet] {line}"));
                                }
                            }
                        }
                    });

                    var errorTask = Task.Run(async () =>
                    {
                        using (var reader = process.StandardError)
                        {
                            while (!reader.EndOfStream)
                            {
                                string? line = await reader.ReadLineAsync();
                                if (line != null)
                                {
                                    this.DispatcherQueue.TryEnqueue(() => AppendLog($"[dotnet Error] {line}"));
                                }
                            }
                        }
                    });

                    await Task.WhenAll(outputTask, errorTask);
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        AppendLog(string.Format(GetLocalizedText("Log_NuGetSuccess", "[NuGet] ¡Empaquetado exitoso! El paquete .nupkg (v{0}) está disponible en: {1}"), packageVersion, outputPath));
                        return true;
                    }
                    else
                    {
                        AppendLog(string.Format(GetLocalizedText("Log_DotnetPackFailed", "[Error] El comando 'dotnet pack' falló con código de salida: {0}"), process.ExitCode));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog(string.Format(GetLocalizedText("Log_DotnetException", "[Error NuGet] Falló la ejecución del empaquetado dotnet: {0}"), ex.Message));
                return false;
            }
        }

        private async Task DownloadStylesheetsAndFontsAsync(string destinationFolder)
        {
            var styles = new List<(string CssName, string TtfName)>();

            if (RegularCheckBox.IsChecked == true) styles.Add(("FluentSystemIcons-Regular", "FluentSystemIcons-Regular"));
            if (FilledCheckBox.IsChecked == true) styles.Add(("FluentSystemIcons-Filled", "FluentSystemIcons-Filled"));
            if (LightCheckBox.IsChecked == true) styles.Add(("FluentSystemIcons-Light", "FluentSystemIcons-Light"));
            if (ResizableCheckBox.IsChecked == true) styles.Add(("FluentSystemIcons-Resizable", "FluentSystemIcons-Resizable"));

            var stylesToDownload = styles.Distinct().ToList();
            double progressStep = 30.0 / (stylesToDownload.Count * 2);
            double currentProgress = 5.0;

            foreach (var style in stylesToDownload)
            {
                string cssFileName = $"{style.CssName}.css";
                string ttfFileName = $"{style.TtfName}.ttf";

                AppendLog(string.Format(GetLocalizedText("Log_DownloadingCss", "[GitHub] Descargando {0}..."), cssFileName));
                UpdateProgress(currentProgress, string.Format(GetLocalizedText("Progress_DownloadingCss", "Descargando {0}..."), cssFileName));
                var cssContent = await _httpClient.GetStringAsync($"{GitHubBaseUrl}{cssFileName}");
                File.WriteAllText(Path.Combine(destinationFolder, cssFileName), cssContent);
                currentProgress += progressStep;

                AppendLog(string.Format(GetLocalizedText("Log_DownloadingTtf", "[GitHub] Descargando {0}..."), ttfFileName));
                UpdateProgress(currentProgress, string.Format(GetLocalizedText("Progress_DownloadingTtf", "Descargando {0}..."), ttfFileName));
                var ttfBytes = await _httpClient.GetByteArrayAsync($"{GitHubBaseUrl}{ttfFileName}");
                File.WriteAllBytes(Path.Combine(destinationFolder, ttfFileName), ttfBytes);
                currentProgress += progressStep;
            }
        }

        private void ProcessCss(string cssFile, string targetFolder, string className, string styleSuffix, string namespaceName)
        {
            string cssContent = File.ReadAllText(cssFile);
            var regex = new Regex($@"\.icon-ic_fluent_(.*?){Regex.Escape(styleSuffix)}:before\s*\{{\s*content:\s*""\\(.*?)"";", RegexOptions.IgnoreCase);

            var icons = new SortedDictionary<string, string>();
            foreach (Match match in regex.Matches(cssContent))
            {
                string propName = ToPascalCase(match.Groups[1].Value);
                string unicode = match.Groups[2].Value;
                icons[propName] = unicode;
            }

            if (icons.Count > 0)
            {
                WriteClassFile(targetFolder, className, icons, namespaceName);
                AppendLog(string.Format(GetLocalizedText("Log_ClassGenerated", "   ✅ Generada clase {0}.cs con {1} iconos."), className, icons.Count));
            }
            else
            {
                AppendLog(string.Format(GetLocalizedText("Log_NoIconsForStyle", "   ⚠️ No se encontraron iconos para el estilo '{0}' en {1}."), styleSuffix, Path.GetFileName(cssFile)));
            }
        }

        private void ProcessResizable(string cssFile, string targetFolder, string namespaceName)
        {
            string cssContent = File.ReadAllText(cssFile);

            var styles = new[]
            {
                new { Suffix = "_regular", Class = "FluentSystemIconsResizableRegular" },
                new { Suffix = "_filled",  Class = "FluentSystemIconsResizableFilled"  },
                new { Suffix = "_light",   Class = "FluentSystemIconsResizableLight"   }
            };

            foreach (var style in styles)
            {
                var regex = new Regex($@"\.icon-ic_fluent_(.*?){Regex.Escape(style.Suffix)}:before\s*\{{\s*content:\s*""\\(.*?)"";", RegexOptions.IgnoreCase);

                var icons = new SortedDictionary<string, string>();
                foreach (Match match in regex.Matches(cssContent))
                {
                    string propName = ToPascalCase(match.Groups[1].Value);
                    string unicode = match.Groups[2].Value;
                    icons[propName] = unicode;
                }

                if (icons.Count > 0)
                {
                    WriteClassFile(targetFolder, style.Class, icons, namespaceName);
                    AppendLog(string.Format(GetLocalizedText("Log_ClassGenerated", "   ✅ Generada clase {0}.cs con {1} iconos."), style.Class, icons.Count));
                }
                else
                {
                    AppendLog(string.Format(GetLocalizedText("Log_NoIconsForResizableStyle", "   ⚠️ No se encontraron iconos para el estilo '{0}' en el archivo Resizable."), style.Suffix));
                }
            }
        }

        private void WriteClassFile(string targetFolder, string className, SortedDictionary<string, string> icons, string namespaceName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {className}");
            sb.AppendLine("    {");

            foreach (var icon in icons)
            {
                string pascalName = icon.Key;
                string hexCodeUpper = icon.Value.ToUpper();
                string displayName = SplitPascalCase(pascalName);
                string csharpGlyph = $"\\u{hexCodeUpper}";
                string xamlCode = $"&#x{hexCodeUpper};";

                sb.AppendLine($"        public const string {pascalName} = \"{csharpGlyph}\";");
                sb.AppendLine();
                sb.AppendLine($"        public const string {pascalName}XamlCode = \"{xamlCode}\";");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Human-readable name for the icon. Value: \"{displayName}\"");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public const string {pascalName}DisplayName = \"{displayName}\";");
                sb.AppendLine();
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string outputPath = Path.Combine(targetFolder, $"{className}.cs");
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private void RegenerateResourcesDictionary(string xamlPath, List<string> ttfFileNames)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ResourceDictionary");
            sb.AppendLine("    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
            sb.AppendLine("    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
            sb.AppendLine();

            foreach (var ttfName in ttfFileNames)
            {
                string keyName = "FluentIcons" + ttfName.Replace("FluentSystemIcons-", "").Replace(".ttf", "");
                string fontName = Path.GetFileNameWithoutExtension(ttfName);

                sb.AppendLine($"    <!-- Familia de fuentes para {ttfName} -->");
                sb.AppendLine($"    <FontFamily x:Key=\"{keyName}\">ms-appx:///FluentSystemIcons.WinUI/Assets/Fonts/{ttfName}#{fontName}</FontFamily>");
                sb.AppendLine();
            }

            sb.AppendLine("</ResourceDictionary>");

            string parentDir = Path.GetDirectoryName(xamlPath) ?? string.Empty;
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            File.WriteAllText(xamlPath, sb.ToString(), Encoding.UTF8);
            AppendLog(GetLocalizedText("Log_XamlUpdated", "[XAML] Diccionario de Recursos FluentIcons.xaml reconstruido y sincronizado con éxito."));
        }

        private string ToPascalCase(string input)
        {
            string processedInput = input.Replace('_', ' ');
            var parts = processedInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                }
            }
            return string.Join("", parts);
        }

        private string SplitPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder();
            sb.Append(input[0]);
            for (int i = 1; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsUpper(c))
                {
                    sb.Append(' ');
                }
                else if (char.IsDigit(c) && !char.IsDigit(input[i - 1]))
                {
                    sb.Append(' ');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private void UpdateProgress(double value, string statusText)
        {
            GenerationProgressBar.Value = value;
            ProgressStatusTextBlock.Text = statusText;
        }

        private async void VerifyVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (VerifyVersionButton != null)
            {
                VerifyVersionButton.IsEnabled = false;
            }

            VersionUpdateInfoBar.Message = GetLocalizedText("Status_Checking_Message", "Comprobando última versión en el repositorio de Microsoft...");

            await CheckVersionsAsync();
        }

        private void UpdateMainButtonText()
        {
            if (GenerateButton == null || PackNuGetCheckBox == null) return;

            bool hasUpdate = (_latestGitHubVersion != null && _latestGitHubVersion != _localVersion);
            bool isPacking = (PackNuGetCheckBox.IsChecked == true);

            if (hasUpdate)
            {
                GenerateButton.Content = isPacking ? GetLocalizedText("Button_UpdatePack", "Actualizar, Generar y Compilar") : GetLocalizedText("Button_UpdateGen", "Actualizar y generar");
            }
            else
            {
                GenerateButton.Content = isPacking ? GetLocalizedText("Button_Pack", "Generar y Compilar") : GetLocalizedText("Button_Gen", "Generar Recursos");
            }
        }

        private void PackNuGetCheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (NuGetVersionPanel != null)
            {
                NuGetVersionPanel.Visibility = (PackNuGetCheckBox.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            }
            UpdateMainButtonText();
        }

        private void UseCatalogVersionCheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (NuGetVersionTextBox != null && UseCatalogVersionCheckBox != null)
            {
                NuGetVersionTextBox.IsEnabled = (UseCatalogVersionCheckBox.IsChecked != true);
                if (UseCatalogVersionCheckBox.IsChecked == true && !string.IsNullOrEmpty(_latestGitHubVersion))
                {
                    NuGetVersionTextBox.Text = _latestGitHubVersion;
                }
            }
        }

        private async Task CheckVersionsAsync()
        {
            _localVersion = SettingsService.LoadLastSyncVersion();

            if (LocalVersionRun != null)
            {
                LocalVersionRun.Text = _localVersion;
            }

            try
            {
                if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
                {
                    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FluentSystemIconsGeneratorApp");
                }

                string jsonResponse = await _httpClient.GetStringAsync("https://api.github.com/repos/microsoft/fluentui-system-icons/tags");

                using (var doc = JsonDocument.Parse(jsonResponse))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        string? latestVersionTag = null;
                        var versionRegex = new Regex(@"^\d+\.\d+\.\d+$");

                        foreach (var tagElement in doc.RootElement.EnumerateArray())
                        {
                            if (tagElement.TryGetProperty("name", out var nameProperty))
                            {
                                string? tagName = nameProperty.GetString();
                                if (tagName != null && versionRegex.IsMatch(tagName))
                                {
                                    latestVersionTag = tagName;
                                    break;
                                }
                            }
                        }

                        if (latestVersionTag != null)
                        {
                            _latestGitHubVersion = latestVersionTag;

                            if (GitHubVersionRun != null)
                            {
                                GitHubVersionRun.Text = _latestGitHubVersion;
                            }

                            if (UseCatalogVersionCheckBox != null && UseCatalogVersionCheckBox.IsChecked == true && NuGetVersionTextBox != null)
                            {
                                NuGetVersionTextBox.Text = _latestGitHubVersion;
                            }

                            if (VersionUpdateInfoBar != null)
                            {
                                if (_latestGitHubVersion != _localVersion)
                                {
                                    VersionUpdateInfoBar.Title = GetLocalizedText("Status_UpdateAvailable_Title", "Actualización disponible de iconos");
                                    VersionUpdateInfoBar.Message = string.Format(GetLocalizedText("Status_UpdateAvailable_Message", "Su versión local es la versión {0}, pero la versión {1} está disponible en GitHub de Microsoft."), _localVersion, _latestGitHubVersion);
                                    VersionUpdateInfoBar.Severity = InfoBarSeverity.Informational;
                                    VersionUpdateInfoBar.IsOpen = true;
                                    VersionUpdateInfoBar.ActionButton = null;
                                }
                                else
                                {
                                    VersionUpdateInfoBar.Title = GetLocalizedText("Status_AlreadyUpToDate_Title", "Catálogo de iconos al día");
                                    VersionUpdateInfoBar.Message = string.Format(GetLocalizedText("Status_AlreadyUpToDate_Message", "Su catálogo local está completamente actualizado en la versión {0}."), _localVersion);
                                    VersionUpdateInfoBar.Severity = InfoBarSeverity.Success;
                                    VersionUpdateInfoBar.IsOpen = true;
                                    VersionUpdateInfoBar.ActionButton = null;
                                }
                            }

                            UpdateMainButtonText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog(string.Format(GetLocalizedText("Log_ErrorCheckVersion", "[Error] No se pudo comprobar la versión de iconos desde GitHub: {0}"), ex.Message));

                if (GitHubVersionRun != null)
                {
                    GitHubVersionRun.Text = GetLocalizedText("Status_ConnectionError", "Error de conexión");
                }

                if (VersionUpdateInfoBar != null)
                {
                    VersionUpdateInfoBar.Title = GetLocalizedText("Status_ConnectionError", "Error de conexión");
                    VersionUpdateInfoBar.Message = GetLocalizedText("Status_ConnectionError_Message", "No se pudo conectar con el servidor para verificar actualizaciones.");
                    VersionUpdateInfoBar.Severity = InfoBarSeverity.Warning;
                    VersionUpdateInfoBar.IsOpen = true;

                    if (VerifyVersionButton != null)
                    {
                        VerifyVersionButton.IsEnabled = true;
                    }
                }
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            _logBuilder.Clear();
            LogTextBlock.Text = string.Empty;
        }

        private void CopyLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(_logBuilder.ToString());
                Clipboard.SetContent(dataPackage);
                AppendLog(GetLocalizedText("Log_CopiedToClipboard", "[Info] Registro de consola copiado al portapapeles."));
            }
            catch (Exception ex)
            {
                AppendLog(string.Format(GetLocalizedText("Log_ErrorCopy", "[Error] No se pudo copiar al portapapeles: {0}"), ex.Message));
            }
        }
    }
}