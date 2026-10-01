using FluentSystemIcons.Gallery.Helpers;
using FluentSystemIcons.Gallery.Models;
using FluentSystemIcons.Gallery.Services;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;
using Microsoft.Windows.ApplicationModel.Resources;
using FluentSystemIcons;

namespace FluentSystemIcons.Gallery
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private AppWindow _appWindow;
        private List<IconInfo> _allIcons = new();
        public ObservableCollection<IconInfo> FilteredIcons { get; set; }

        private bool _isWindowLoaded = false;
        private bool _isSidePanelVisible = false;

        private string _selectedStyleFilter = "All";
        private string _selectedCategoryFilter = "Todas";
        private string _selectedSizeFilter = "Todas";
        private HashSet<string> _favoriteIconNames = new HashSet<string>();
        private readonly string[] StandardSizes = { "Todas", "12 px", "16 px", "20 px", "24 px", "28 px", "32 px", "40 px", "48 px" };
        private const byte HOVER_OPACITY = 0x15;
        private const byte PRESSED_OPACITY = 0x22;

        private int _currentSearchVersion = 0;

        private RectInt32 _normalBounds = new RectInt32(0, 0, 1280, 800);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _themeButtonToolTip = string.Empty;

        public string ThemeButtonToolTip
        {
            get => _themeButtonToolTip;
            set { if (_themeButtonToolTip != value) { _themeButtonToolTip = value; OnPropertyChanged(nameof(ThemeButtonToolTip)); } }
        }

        // Propiedad que expone el icono de búsqueda regular de 20px de la librería para XAML
        public string SearchIconGlyph => FluentSystemIconsRegular.Search20;

        public MainWindow()
        {
            this.InitializeComponent();
            SidePanel.IsFavoriteChecker = icon => _favoriteIconNames.Contains($"{icon.Name}|{icon.FontKey}");
            SidePanel.FavoriteToggled += SidePanel_FavoriteToggled;
            SidePanel.ParentWindow = this;
            _appWindow = this.AppWindow;
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
            RootGrid.Loaded += RootGrid_Loaded;
            FilteredIcons = new ObservableCollection<IconInfo>();
            Activated += MainWindow_Activated;

            // REGISTRAR EVENTO DE UNIFICACIÓN VISUAL DE LA CAJA DE BÚSQUEDA
            RegisterSearchBoxEvents();
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
                _appWindow.Resize(new SizeInt32(1280, 800));
                _normalBounds = new RectInt32(_appWindow.Position.X, _appWindow.Position.Y, 1280, 800);
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
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(HOVER_OPACITY, 0xFF, 0xFF, 0xFF);
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(PRESSED_OPACITY, 0xFF, 0xFF, 0xFF);
            }
            else
            {
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(HOVER_OPACITY, 0x00, 0x00, 0x00);
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(PRESSED_OPACITY, 0x00, 0x00, 0x00);
            }
        }

        private void UpdateRootBackground()
        {
            var backdrop = SettingsService.LoadBackdrop();

            if (backdrop == BackdropType.None)
            {
                var theme = SettingsService.LoadTheme();

                ElementTheme finalTheme;
                if (theme == ElementTheme.Default)
                {
                    finalTheme = (this.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;
                }
                else
                {
                    finalTheme = theme;
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

        #region Lógica de Sincronización de Tema y Fondo
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
                var resourceManager = new ResourceManager();
                var resourceKey = rootElement.ActualTheme == ElementTheme.Dark
                    ? "ThemeButton_Dark_ToolTip"
                    : "ThemeButton_Light_ToolTip";
                ThemeButtonToolTip = resourceManager.MainResourceMap.GetValue($"Resources/{resourceKey}").ValueAsString;
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

                var resourceManager = new ResourceManager();
                var dialog = new ContentDialog
                {
                    Title = resourceManager.MainResourceMap.GetValue("Resources/Settings_RestartRequired_Title").ValueAsString,
                    Content = resourceManager.MainResourceMap.GetValue("Resources/Settings_RestartRequired_Message").ValueAsString,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
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

        #endregion

        #region Lógica de la Barra de Título Personalizada
        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e) { SetRegionsForCustomTitleBar(); }
        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e) { SetRegionsForCustomTitleBar(); }

        private void SetRegionsForCustomTitleBar()
        {
            if (ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;
                RightPaddingColumn.Width = new GridLength(_appWindow.TitleBar.RightInset / scaleAdjustment);
                var rects = new List<RectInt32>
                {
                    GetRect(AppTitlePanel, scaleAdjustment),
                    GetRect(IconSearchBox, scaleAdjustment),
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

        #endregion

        #region Carga de la Aplicación y Lógica Principal
        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                // Aplicar maximización de manera segura ahora que el árbol visual está listo
                if (SettingsService.LoadIsMaximized())
                {
                    if (_appWindow.Presenter is OverlappedPresenter overlappedPresenter)
                    {
                        overlappedPresenter.Maximize();
                    }
                }

                await PerformInitialLoadAsync();
            });
        }

        private async Task PerformInitialLoadAsync()
        {
            var savedMode = SettingsService.LoadGridSizeMode();
            IconViewSwitcher.CurrentSizeMode = savedMode;
            _favoriteIconNames = new HashSet<string>(SettingsService.LoadFavorites());

            var resourceManager = new ResourceManager();
            CategoryComboBox.Items.Clear();
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_All").ValueAsString, Tag = "Todas" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_Navigation").ValueAsString, Tag = "Navegación" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_Multimedia").ValueAsString, Tag = "Multimedia" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_Communication").ValueAsString, Tag = "Comunicación" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_Documents").ValueAsString, Tag = "Documentos y Archivos" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_System").ValueAsString, Tag = "Sistema y Hardware" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_Users").ValueAsString, Tag = "Usuarios y Personas" });
            CategoryComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_General").ValueAsString, Tag = "General" });
            CategoryComboBox.SelectedIndex = 0;

            await LoadAllIconsAsync();
            var splashFadeOutStoryboard = new Storyboard();
            var splashFadeOutAnimation = new DoubleAnimation { To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(300)), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(splashFadeOutAnimation, SplashGrid);
            Storyboard.SetTargetProperty(splashFadeOutAnimation, "Opacity");
            splashFadeOutStoryboard.Children.Add(splashFadeOutAnimation);

            var contentFadeInStoryboard = new Storyboard();
            var contentFadeInAnimation = new DoubleAnimation { To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(500)), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(contentFadeInAnimation, ContentContainer);
            Storyboard.SetTargetProperty(contentFadeInAnimation, "Opacity");
            contentFadeInStoryboard.Children.Add(contentFadeInAnimation);

            var titleBarFadeInStoryboard = new Storyboard();
            var titleBarFadeInAnimation = new DoubleAnimation { To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(500)), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(titleBarFadeInAnimation, AppTitleBar);
            Storyboard.SetTargetProperty(titleBarFadeInAnimation, "Opacity");
            titleBarFadeInStoryboard.Children.Add(titleBarFadeInAnimation);
            AppTitleBar.Visibility = Visibility.Visible;
            ContentContainer.Visibility = Visibility.Visible;
            splashFadeOutStoryboard.Begin();
            contentFadeInStoryboard.Begin();
            titleBarFadeInStoryboard.Begin();
            await splashFadeOutStoryboard.BeginAsync();

            SplashGrid.Visibility = Visibility.Collapsed;
            _isWindowLoaded = true;
        }

        private async Task LoadAllIconsAsync()
        {
            _allIcons = new List<IconInfo>();
            var regularFont = Application.Current.Resources["FluentIconsRegular"] as FontFamily;
            var filledFont = Application.Current.Resources["FluentIconsFilled"] as FontFamily;
            var lightFont = Application.Current.Resources["FluentIconsLight"] as FontFamily;
            var resizableFont = Application.Current.Resources["FluentIconsResizable"] as FontFamily;
            await Task.Run(() =>
            {
                if (regularFont != null)
                {
                    LoadIconsFromType(typeof(FluentSystemIconsRegular), regularFont, "FluentIconsRegular");
                }
                if (filledFont != null)
                {
                    LoadIconsFromType(typeof(FluentSystemIconsFilled), filledFont, "FluentIconsFilled");
                }
                if (lightFont != null)
                {
                    LoadIconsFromType(typeof(FluentSystemIconsLight), lightFont, "FluentIconsLight");
                }
                if (resizableFont != null)
                {
                    LoadIconsFromType(typeof(FluentSystemIconsResizableRegular), resizableFont, "FluentIconsResizable");
                    LoadIconsFromType(typeof(FluentSystemIconsResizableFilled), resizableFont, "FluentIconsResizable");
                }

                // ORDENACIÓN ALFABÉTICA INICIAL POR NOMBRE AMIGABLE
                _allIcons = _allIcons.OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            });
            this.DispatcherQueue.TryEnqueue(() =>
            {
                var resourceManager = new ResourceManager();
                SizeComboBox.Items.Clear();
                SizeComboBox.Items.Add(new ComboBoxItem { Content = resourceManager.MainResourceMap.GetValue("Resources/Category_All").ValueAsString, Tag = "Todas" });
                foreach (var size in StandardSizes)
                {
                    if (size != "Todas")
                    {
                        SizeComboBox.Items.Add(new ComboBoxItem { Content = size, Tag = size });
                    }
                }
                SizeComboBox.SelectedIndex = 0;

                ApplyFiltering();
            });
        }

        private void LoadIconsFromType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type, FontFamily fontFamily, string fontKey)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));
            var iconGroups = fields.GroupBy(f => f.Name.Replace("XamlCode", "").Replace("DisplayName", ""));
            foreach (var group in iconGroups)
            {
                var nameField = group.FirstOrDefault(f => !f.Name.EndsWith("XamlCode") && !f.Name.EndsWith("DisplayName"));
                var xamlCodeField = group.FirstOrDefault(f => f.Name.EndsWith("XamlCode"));
                var displayNameField = group.FirstOrDefault(f => f.Name.EndsWith("DisplayName"));
                if (nameField != null && xamlCodeField != null && displayNameField != null)
                {
                    var icon = new IconInfo
                    {
                        Name = nameField.Name,
                        DisplayName = displayNameField.GetValue(null) as string ?? string.Empty,
                        Glyph = nameField.GetValue(null) as string ?? string.Empty,
                        XamlCode = xamlCodeField.GetValue(null) as string ?? string.Empty,
                        FontFamily = fontFamily,
                        FontKey = fontKey
                    };
                    AssignMetadata(icon);
                    var match = System.Text.RegularExpressions.Regex.Match(nameField.Name, @"(12|16|20|24|28|32|40|48)$");
                    icon.Size = match.Success ? match.Value : "24";

                    _allIcons.Add(icon);
                }
            }
        }

        private void AssignMetadata(IconInfo icon)
        {
            string name = icon.Name.ToLowerInvariant();

            if (name.Contains("arrow") || name.Contains("back") || name.Contains("forward") ||
                name.Contains("home") || name.Contains("menu") || name.Contains("navigation") ||
                name.Contains("close") || name.Contains("exit") || name.Contains("search") || name.Contains("find"))
            {
                icon.Category = "Navegación";
                if (name.Contains("search") || name.Contains("find"))
                {
                    icon.Keywords.AddRange(new[] { "buscar", "lupa", "encontrar", "localizar", "magnifier", "find" });
                }
                if (name.Contains("home"))
                {
                    icon.Keywords.AddRange(new[] { "inicio", "casa", "principal", "house", "start" });
                }
                if (name.Contains("close"))
                {
                    icon.Keywords.AddRange(new[] { "cerrar", "salir", "eliminar", "tachar", "cancel", "exit" });
                }
                if (name.Contains("arrow"))
                {
                    icon.Keywords.AddRange(new[] { "flecha", "direccion", "puntero", "pointer", "direction" });
                }
            }
            else if (name.Contains("play") || name.Contains("pause") || name.Contains("stop") ||
                     name.Contains("video") || name.Contains("music") || name.Contains("volume") ||
                     name.Contains("mic") || name.Contains("sound") || name.Contains("record") || name.Contains("camera"))
            {
                icon.Category = "Multimedia";
                if (name.Contains("play")) icon.Keywords.AddRange(new[] { "reproducir", "iniciar", "start" });
                if (name.Contains("pause")) icon.Keywords.AddRange(new[] { "pausar", "detener", "suspend" });
                if (name.Contains("volume") || name.Contains("sound")) icon.Keywords.AddRange(new[] { "volumen", "sonido", "audio", "parlante", "speaker" });
                if (name.Contains("camera")) icon.Keywords.AddRange(new[] { "camara", "foto", "video", "photo" });
            }
            else if (name.Contains("mail") || name.Contains("chat") || name.Contains("phone") ||
                     name.Contains("call") || name.Contains("send") || name.Contains("message") || name.Contains("contact"))
            {
                icon.Category = "Comunicación";
                if (name.Contains("mail")) icon.Keywords.AddRange(new[] { "correo", "email", "mensaje", "carta", "envelope" });
                if (name.Contains("chat") || name.Contains("message")) icon.Keywords.AddRange(new[] { "mensaje", "conversacion", "globo", "talk", "sms" });
                if (name.Contains("phone") || name.Contains("call")) icon.Keywords.AddRange(new[] { "telefono", "llamar", "celular", "mobile" });
            }
            else if (name.Contains("file") || name.Contains("folder") || name.Contains("document") ||
                     name.Contains("page") || name.Contains("copy") || name.Contains("paste") ||
                     name.Contains("delete") || name.Contains("trash") || name.Contains("edit") || name.Contains("save"))
            {
                icon.Category = "Documentos y Archivos";
                if (name.Contains("file") || name.Contains("document") || name.Contains("page")) icon.Keywords.AddRange(new[] { "archivo", "documento", "pagina", "hoja", "paper" });
                if (name.Contains("folder")) icon.Keywords.AddRange(new[] { "carpeta", "directorio", "directory" });
                if (name.Contains("copy")) icon.Keywords.AddRange(new[] { "copiar", "duplicar", "duplicate" });
                if (name.Contains("paste")) icon.Keywords.AddRange(new[] { "pegar", "insertar" });
                if (name.Contains("delete") || name.Contains("trash")) icon.Keywords.AddRange(new[] { "eliminar", "borrar", "basura", "tacho", "remove", "erase" });
                if (name.Contains("edit")) icon.Keywords.AddRange(new[] { "editar", "escribir", "lapiz", "pencil", "pen", "modify" });
                if (name.Contains("save")) icon.Keywords.AddRange(new[] { "guardar", "disco", "disk" });
            }
            else if (name.Contains("settings") || name.Contains("gear") || name.Contains("device") ||
                     name.Contains("battery") || name.Contains("wifi") || name.Contains("network") ||
                     name.Contains("bluetooth") || name.Contains("power"))
            {
                icon.Category = "Sistema y Hardware";
                if (name.Contains("settings") || name.Contains("gear")) icon.Keywords.AddRange(new[] { "configuracion", "ajustes", "engrane", "preferencias", "options", "preferences", "setup" });
                if (name.Contains("wifi") || name.Contains("network")) icon.Keywords.AddRange(new[] { "red", "conexion", "internet", "signal" });
                if (name.Contains("power")) icon.Keywords.AddRange(new[] { "encender", "apagar", "energia", "shutdown" });
            }
            else if (name.Contains("user") || name.Contains("people") || name.Contains("group") ||
                     name.Contains("person") || name.Contains("profile") || name.Contains("avatar"))
            {
                icon.Category = "Usuarios y Personas";
                icon.Keywords.AddRange(new[] { "usuario", "persona", "perfil", "avatar", "gente", "grupo", "member" });
            }
            else
            {
                icon.Category = "General";
            }
        }

        #endregion

        #region Lógica de Búsqueda y Filtrado Unificado

        private async Task ApplyFilteringAsync()
        {
            int searchVersion = ++_currentSearchVersion;

            string searchTerm = IconSearchBox.Text;
            string selectedStyle = _selectedStyleFilter;
            string selectedCategory = _selectedCategoryFilter;
            string selectedSize = _selectedSizeFilter;

            var finalResults = await Task.Run(() =>
            {
                IEnumerable<IconInfo> categoryFiltered = _allIcons;
                if (selectedCategory != "Todas")
                {
                    categoryFiltered = _allIcons.Where(icon => icon.Category == selectedCategory);
                }

                IEnumerable<IconInfo> styleFiltered = categoryFiltered;
                if (selectedStyle != "All")
                {
                    styleFiltered = categoryFiltered.Where(icon =>
                    {
                        if (selectedStyle == "Regular") return icon.FontKey == "FluentIconsRegular";
                        if (selectedStyle == "Filled") return icon.FontKey == "FluentIconsFilled";
                        if (selectedStyle == "Light") return icon.FontKey == "FluentIconsLight";
                        if (selectedStyle == "Resizable") return icon.FontKey == "FluentIconsResizable";
                        if (selectedStyle == "Favorites") return _favoriteIconNames.Contains($"{icon.Name}|{icon.FontKey}");
                        return true;
                    });
                }

                string sizeValue = selectedSize == "Todas" ? "Todas" : selectedSize.Replace(" px", "");
                IEnumerable<IconInfo> sizeFiltered = styleFiltered;
                if (sizeValue != "Todas")
                {
                    sizeFiltered = styleFiltered.Where(icon => icon.Size == sizeValue);
                }

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return sizeFiltered.ToList();
                }
                else
                {
                    return sizeFiltered
                        .Select(icon => new { Icon = icon, Score = CalculateScore(icon, searchTerm) })
                        .Where(item => item.Score > 0)
                        .OrderByDescending(item => item.Score)
                        .Select(item => item.Icon)
                        .Take(100)
                        .ToList();
                }
            });

            if (searchVersion != _currentSearchVersion)
            {
                return;
            }

            FilteredIcons.Clear();
            foreach (var icon in finalResults)
            {
                FilteredIcons.Add(icon);
            }

            UpdateIconCountText(finalResults.Count);

            var resourceManager = new ResourceManager();

            // Caso 1: Estamos en la pestaña Favoritos y no hay ninguno
            if (selectedStyle == "Favorites" && finalResults.Count == 0 && string.IsNullOrWhiteSpace(searchTerm))
            {
                NoFavoritesStackPanel.Visibility = Visibility.Visible;
                NoResultsStackPanel.Visibility = Visibility.Collapsed;
                IconGridView.Visibility = Visibility.Collapsed;
            }
            // Caso 2: Búsqueda sin resultados (en cualquier pestaña)
            else if (finalResults.Count == 0 && !string.IsNullOrWhiteSpace(searchTerm))
            {
                string noResultsFormat;
                try { noResultsFormat = resourceManager.MainResourceMap.GetValue("Resources/NoResults_Text").ValueAsString; }
                catch { noResultsFormat = "No hay resultados para \"{0}\""; }

                NoResultsTextBlock.Text = string.Format(noResultsFormat, searchTerm);
                NoResultsStackPanel.Visibility = Visibility.Visible;
                NoFavoritesStackPanel.Visibility = Visibility.Collapsed;
                IconGridView.Visibility = Visibility.Collapsed;
            }
            // Caso 3: Hay iconos para mostrar
            else
            {
                NoResultsStackPanel.Visibility = Visibility.Collapsed;
                NoFavoritesStackPanel.Visibility = Visibility.Collapsed;
                IconGridView.Visibility = Visibility.Visible;
            }
        }

        private async void ApplyFiltering()
        {
            await ApplyFilteringAsync();
        }

        private void UpdateIconCountText(int count)
        {
            try
            {
                var resourceManager = new ResourceManager();
                var formatString = resourceManager.MainResourceMap.GetValue("Resources/IconCount_Text").ValueAsString;
                IconCountTextBlock.Text = string.Format(formatString, count);
            }
            catch
            {
                IconCountTextBlock.Text = $"{count} iconos";
            }
        }

        private void IconSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchTerm = sender.Text;
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    sender.ItemsSource = null;
                }
                else
                {
                    sender.ItemsSource = GetRankedSearchResults(searchTerm);
                }
                ApplyFiltering();
            }
        }

        private async void IconSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var chosenSuggestion = args.ChosenSuggestion as IconInfo;
            if (chosenSuggestion != null)
            {
                // Mantenemos el texto ingresado original de búsqueda (ej: "acc") sin modificarlo a la familia para que no cambie el searchbox
                // Buscamos directamente la coincidencia exacta dentro de la colección existente 'FilteredIcons'
                var match = FilteredIcons.FirstOrDefault(i => i.Name == chosenSuggestion.Name && i.FontKey == chosenSuggestion.FontKey);
                if (match == null)
                {
                    // En caso extremo de desfase por escritura muy veloz, forzamos un procesado asíncrono para asegurar que se cargue en la cuadrícula
                    await ApplyFilteringAsync();
                    match = FilteredIcons.FirstOrDefault(i => i.Name == chosenSuggestion.Name && i.FontKey == chosenSuggestion.FontKey);
                }

                if (match != null)
                {
                    // Primero desplazamos el GridView al elemento para forzar que XAML realice su contenedor visual en la GPU
                    IconGridView.ScrollIntoView(match);

                    // Asignamos la selección. Esto disparará automáticamente IconGridView_SelectionChanged en C# para pintar el borde
                    IconGridView.SelectedItem = match;

                    // Abrimos el panel lateral de detalles de forma automática
                    OpenSidePanel();
                }
            }
            else
            {
                await ApplyFilteringAsync();
            }
        }

        private void IconSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // No hacemos nada para prevenir la colisión asíncrona concurrente con QuerySubmitted, según el diagnóstico.
        }

        private void UpdateFilteredIcons(string searchTerm)
        {
            ApplyFiltering();
        }

        private List<IconInfo> GetRankedSearchResults(string searchTerm)
        {
            string selectedStyle = _selectedStyleFilter;
            string selectedCategory = _selectedCategoryFilter;
            string selectedSize = _selectedSizeFilter;
            IEnumerable<IconInfo> categoryFiltered = _allIcons;
            if (selectedCategory != "Todas")
            {
                categoryFiltered = _allIcons.Where(icon => icon.Category == selectedCategory);
            }
            using (var task = Task.Run(() =>
            {
                IEnumerable<IconInfo> styleFiltered = categoryFiltered;
                if (selectedStyle != "All")
                {
                    styleFiltered = categoryFiltered.Where(icon =>
                    {
                        if (selectedStyle == "Regular") return icon.FontKey == "FluentIconsRegular";
                        if (selectedStyle == "Filled") return icon.FontKey == "FluentIconsFilled";
                        if (selectedStyle == "Light") return icon.FontKey == "FluentIconsLight";
                        if (selectedStyle == "Resizable") return icon.FontKey == "FluentIconsResizable";
                        if (selectedStyle == "Favorites") return _favoriteIconNames.Contains($"{icon.Name}|{icon.FontKey}");
                        return true;
                    });
                }

                string sizeValue = selectedSize == "Todas" ? "Todas" : selectedSize.Replace(" px", "");
                IEnumerable<IconInfo> sizeFiltered = styleFiltered;
                if (sizeValue != "Todas")
                {
                    sizeFiltered = styleFiltered.Where(icon => icon.Size == sizeValue);
                }

                return sizeFiltered
                    .Select(icon => new { Icon = icon, Score = CalculateScore(icon, searchTerm) })
                    .Where(item => item.Score > 0)
                    .OrderByDescending(item => item.Score)
                    .Select(item => item.Icon)
                    .Take(100)
                    .ToList();
            }))
            {
                task.Wait();
                return task.Result;
            }
        }

        private string NormalizeHexQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return string.Empty;

            string cleaned = query.Trim().ToLowerInvariant();
            if (cleaned.StartsWith("\\u"))
            {
                cleaned = cleaned.Substring(2);
            }
            else if (cleaned.StartsWith("u+"))
            {
                cleaned = cleaned.Substring(2);
            }
            else if (cleaned.StartsWith("&#x"))
            {
                cleaned = cleaned.Substring(3);
                if (cleaned.EndsWith(";"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 1);
                }
            }
            else if (cleaned.StartsWith("0x"))
            {
                cleaned = cleaned.Substring(2);
            }

            return cleaned;
        }

        private int CalculateScore(IconInfo icon, string searchTerm)
        {
            var name = icon.Name.ToLowerInvariant();
            var displayName = icon.DisplayName.ToLowerInvariant();
            var term = searchTerm.ToLowerInvariant();
            if (name.Equals(term) || displayName.Equals(term)) return 100;
            if (!string.IsNullOrEmpty(icon.Glyph))
            {
                string glyphHex = ((int)icon.Glyph[0]).ToString("x4");
                string normalizedSearchHex = NormalizeHexQuery(searchTerm);
                if (!string.IsNullOrEmpty(normalizedSearchHex))
                {
                    if (glyphHex.Equals(normalizedSearchHex)) return 95;
                    if (glyphHex.StartsWith(normalizedSearchHex)) return 65;
                }
            }

            if (!string.IsNullOrEmpty(icon.XamlCode))
            {
                var hexCode = icon.XamlCode.Replace("&#x", "").Replace(";", "").ToLowerInvariant();
                if (hexCode.Equals(term)) return 90;
                if (hexCode.StartsWith(term)) return 60;
            }
            if (icon.Keywords.Any(k => k.Equals(term, StringComparison.OrdinalIgnoreCase))) return 80;
            if (icon.Keywords.Any(k => k.StartsWith(term, StringComparison.OrdinalIgnoreCase))) return 40;
            if (icon.Keywords.Any(k => k.Contains(term, StringComparison.OrdinalIgnoreCase))) return 15;
            if (name.StartsWith(term)) return 50;
            if (displayName.StartsWith(term)) return 40;
            if (name.Contains(term)) return 10;
            if (displayName.Contains(term)) return 5;

            return 0;
        }

        private void StyleSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var selectedItem = sender.SelectedItem;
            if (selectedItem != null && selectedItem.Tag is string style)
            {
                _selectedStyleFilter = style;
                ApplyFiltering();
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is ComboBoxItem item && item.Tag is string category)
            {
                _selectedCategoryFilter = category;
                ApplyFiltering();
            }
        }

        private void SizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string size)
            {
                _selectedSizeFilter = size;
                ApplyFiltering();
            }
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            CategoryComboBox.SelectedIndex = 0;
            SizeComboBox.SelectedIndex = 0;
        }

        private void SidePanel_FavoriteToggled(IconInfo icon, bool isFavorite)
        {
            string compositeKey = $"{icon.Name}|{icon.FontKey}";
            if (isFavorite)
            {
                _favoriteIconNames.Add(compositeKey);
            }
            else
            {
                _favoriteIconNames.Remove(compositeKey);
            }

            SettingsService.SaveFavorites(_favoriteIconNames.ToList());

            if (_selectedStyleFilter == "Favorites")
            {
                ApplyFiltering();
            }
        }

        private void IconSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox searchBox && !string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.ItemsSource = GetRankedSearchResults(searchBox.Text);
                searchBox.IsSuggestionListOpen = true;
            }
        }

        #endregion

        #region Manejadores de Eventos de la UI (Panel Lateral, GridView, etc.)
        private void ShowTemporaryTooltipOnContainer(IconInfo icon)
        {
            if (IconGridView.ContainerFromItem(icon) is GridViewItem container)
            {
                var resourceManager = new ResourceManager();
                string copiedText = resourceManager.MainResourceMap.GetValue("Resources/SidePanel_Copied_Text").ValueAsString;
                var tempToolTip = new ToolTip
                {
                    Content = copiedText,
                    Placement = PlacementMode.Top
                };

                ToolTipService.SetToolTip(container, tempToolTip);
                tempToolTip.IsOpen = true;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    tempToolTip.IsOpen = false;
                    ToolTipService.SetToolTip(container, null);
                };
                timer.Start();
            }
        }

        private void CopyGlyph_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is IconInfo icon)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(icon.XamlCode);
                Clipboard.SetContent(dataPackage);
                ShowTemporaryTooltipOnContainer(icon);
            }
        }

        private void CopyFontIcon_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is IconInfo icon)
            {
                string fontIconXaml = $"<FontIcon FontFamily=\"{{StaticResource {icon.FontKey}}}\" Glyph=\"{icon.XamlCode}\" />";
                var dataPackage = new DataPackage();
                dataPackage.SetText(fontIconXaml);
                Clipboard.SetContent(dataPackage);
                ShowTemporaryTooltipOnContainer(icon);
            }
        }

        private void CopySvg_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is IconInfo icon)
            {
                string svgContent = SidePanel.GenerateSvgMarkup(icon);
                var dataPackage = new DataPackage();
                dataPackage.SetText(svgContent);
                Clipboard.SetContent(dataPackage);
                ShowTemporaryTooltipOnContainer(icon);
            }
        }

        private async void ExportSvg_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is IconInfo icon)
            {
                try
                {
                    var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                    savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                    savePicker.FileTypeChoices.Add("Scalable Vector Graphics", new List<string>() { ".svg" });
                    savePicker.SuggestedFileName = $"{icon.Name}.svg";

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        string svgContent = SidePanel.GenerateSvgMarkup(icon);
                        await Windows.Storage.FileIO.WriteTextAsync(file, svgContent);
                    }
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Error de Exportación",
                        Content = $"No se pudo exportar el archivo SVG: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }

        private async void IconViewSwitcher_SelectionChanged(UserControls.GridSizeSwitcher sender, UserControls.GridSizeMode args)
        {
            if (!_isWindowLoaded) { ApplyTemplateWithoutAnimation(args); return; }

            var fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation { To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(150)), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            Storyboard.SetTarget(fadeOutAnimation, IconGridView);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
            fadeOutStoryboard.Children.Add(fadeOutAnimation);

            await fadeOutStoryboard.BeginAsync();

            ApplyTemplateWithoutAnimation(args);
            SettingsService.SaveGridSizeMode(args);

            var fadeInStoryboard = new Storyboard();
            var fadeInAnimation = new DoubleAnimation { To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(250)), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            Storyboard.SetTarget(fadeInAnimation, IconGridView);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
            fadeInStoryboard.Children.Add(fadeInAnimation);
            fadeInStoryboard.Begin();
        }

        private void ApplyTemplateWithoutAnimation(UserControls.GridSizeMode mode)
        {
            string templateKey;
            switch (mode) { case UserControls.GridSizeMode.Large: templateKey = "LargeIconTemplate"; break; case UserControls.GridSizeMode.Small: templateKey = "SmallIconTemplate"; break; case UserControls.GridSizeMode.Medium: default: templateKey = "MediumIconTemplate"; break; }
            if (this.ContentContainer.Resources.TryGetValue(templateKey, out var template)) { IconGridView.ItemTemplate = template as DataTemplate; }
        }

        private void ToggleSidePanelButton_Click(object sender, RoutedEventArgs e)
        {
            var animationHelper = MainContentGrid.Resources["ColumnAnimationHelper"] as GridLengthAnimationHelper;
            if (animationHelper == null) return;
            animationHelper.TargetColumn = SidePanelColumn;

            var storyboard = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(300);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            var animation = new DoubleAnimation { Duration = duration, EasingFunction = easing, EnableDependentAnimation = true };
            Storyboard.SetTarget(animation, animationHelper);
            Storyboard.SetTargetProperty(animation, "AnimatedValue");

            if (_isSidePanelVisible) { animation.From = SidePanelColumn.ActualWidth; animation.To = 0; } else { animation.From = 0; animation.To = 400; }

            storyboard.Children.Add(animation);
            storyboard.Begin();
            _isSidePanelVisible = !_isSidePanelVisible;
        }

        private void OpenSidePanel()
        {
            if (!_isSidePanelVisible)
            {
                var animationHelper = MainContentGrid.Resources["ColumnAnimationHelper"] as GridLengthAnimationHelper;
                if (animationHelper == null) return;
                animationHelper.TargetColumn = SidePanelColumn;

                var storyboard = new Storyboard();
                var duration = TimeSpan.FromMilliseconds(300);
                var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
                var animation = new DoubleAnimation { Duration = duration, EasingFunction = easing, EnableDependentAnimation = true };
                Storyboard.SetTarget(animation, animationHelper);
                Storyboard.SetTargetProperty(animation, "AnimatedValue");

                animation.From = 0;
                animation.To = 400;

                storyboard.Children.Add(animation);
                storyboard.Begin();
                _isSidePanelVisible = true;
            }
        }

        private void IconCardFlyout_Opening(object sender, object e)
        {
            if (sender is MenuFlyout flyout)
            {
                if (flyout.Target is FrameworkElement target && target.DataContext is IconInfo icon)
                {
                    var menuItem = flyout.Items.OfType<MenuFlyoutItem>()
                        .FirstOrDefault(item => item.Tag as string == "ToggleFavorite");

                    if (menuItem != null)
                    {
                        string compositeKey = $"{icon.Name}|{icon.FontKey}";
                        bool isFavorite = _favoriteIconNames.Contains(compositeKey);

                        var resourceManager = new ResourceManager();

                        if (isFavorite)
                        {
                            menuItem.Text = resourceManager.MainResourceMap.GetValue("Resources/ContextMenu_Favorite_Remove").ValueAsString;
                            if (menuItem.Icon is FontIcon fontIcon)
                            {
                                fontIcon.FontFamily = Application.Current.Resources["FluentIconsFilled"] as FontFamily ?? fontIcon.FontFamily;
                                fontIcon.Glyph = FluentSystemIconsFilled.Star20;
                            }
                        }
                        else
                        {
                            menuItem.Text = resourceManager.MainResourceMap.GetValue("Resources/ContextMenu_Favorite_Add").ValueAsString;
                            if (menuItem.Icon is FontIcon fontIcon)
                            {
                                fontIcon.FontFamily = Application.Current.Resources["FluentIconsRegular"] as FontFamily ?? fontIcon.FontFamily;
                                fontIcon.Glyph = FluentSystemIconsRegular.Star20;
                            }
                        }
                    }
                }
            }
        }

        private void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is IconInfo icon)
            {
                string compositeKey = $"{icon.Name}|{icon.FontKey}";
                bool isFavorite = _favoriteIconNames.Contains(compositeKey);
                bool newFavoriteState = !isFavorite;

                if (newFavoriteState)
                {
                    _favoriteIconNames.Add(compositeKey);
                }
                else
                {
                    _favoriteIconNames.Remove(compositeKey);
                }

                SettingsService.SaveFavorites(_favoriteIconNames.ToList());

                if (_selectedStyleFilter == "Favorites")
                {
                    ApplyFiltering();
                }

                if (SidePanel.SelectedItem != null &&
                    SidePanel.SelectedItem.Name == icon.Name &&
                    SidePanel.SelectedItem.FontKey == icon.FontKey)
                {
                    var current = SidePanel.SelectedItem;
                    SidePanel.SelectedItem = null;
                    SidePanel.SelectedItem = current;
                }
            }
        }

        #endregion

        #region Lógica de Animación de Selección en GridView
        private void IconGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Any())
            {
                var unselectedItem = e.RemovedItems.FirstOrDefault();
                if (IconGridView.ContainerFromItem(unselectedItem) is GridViewItem unselectedContainer)
                {
                    AnimateSelectionBorder(unselectedContainer, 0);
                }
            }
            if (e.AddedItems.Any())
            {
                var selectedItem = e.AddedItems.FirstOrDefault();
                if (IconGridView.ContainerFromItem(selectedItem) is GridViewItem selectedContainer)
                {
                    AnimateSelectionBorder(selectedContainer, 1);
                }
            }
            SidePanel.SelectedItem = IconGridView.SelectedItem as IconInfo;
        }

        private void AnimateSelectionBorder(Control container, double toOpacity)
        {
            if (FindVisualChildByName<Border>(container, "SelectionIndicator") is Border selectionBorder)
            {
                var storyboard = new Storyboard();
                var animation = new DoubleAnimation { To = toOpacity, Duration = TimeSpan.FromMilliseconds(200), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                Storyboard.SetTarget(animation, selectionBorder);
                Storyboard.SetTargetProperty(animation, "Opacity");
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
        }

        public static T? FindVisualChildByName<T>(DependencyObject? parent, string childName) where T : FrameworkElement
        {
            if (parent == null) return null;
            T? foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is not T childType) { foundChild = FindVisualChildByName<T>(child, childName); if (foundChild != null) break; }
                else if (!string.IsNullOrEmpty(childName)) { if (childType.Name == childName) { foundChild = childType; break; } else { foundChild = FindVisualChildByName<T>(child, childName); if (foundChild != null) break; } }
            }
            return foundChild;
        }

        #endregion

        #region Unificación Visual de la Barra de Búsqueda
        private void RegisterSearchBoxEvents()
        {
            // Escucha cambios en la propiedad IsSuggestionListOpenProperty del buscador
            IconSearchBox.RegisterPropertyChangedCallback(AutoSuggestBox.IsSuggestionListOpenProperty, OnIsSuggestionListOpenChanged);
        }

        private void OnIsSuggestionListOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is AutoSuggestBox searchBox)
            {
                if (searchBox.IsSuggestionListOpen)
                {
                    // Al abrirse, aplana las esquinas inferiores del contenedor (16, 16, 0, 0)
                    var flatCorner = new CornerRadius(16, 16, 0, 0);
                    searchBox.CornerRadius = flatCorner;
                    searchBox.Resources["ControlCornerRadius"] = flatCorner;
                    searchBox.Resources["TextControlCornerRadius"] = flatCorner;

                    // Modifica el cuadro de texto hijo del control compuesto para sincronizar la forma
                    if (FindVisualChildByName<TextBox>(searchBox, "TextBox") is TextBox textBox)
                    {
                        textBox.CornerRadius = flatCorner;
                    }
                }
                else
                {
                    // Al cerrarse, restaura la forma de píldora completa (18)
                    var pillCorner = new CornerRadius(18);
                    searchBox.CornerRadius = pillCorner;
                    searchBox.Resources["ControlCornerRadius"] = pillCorner;
                    searchBox.Resources["TextControlCornerRadius"] = pillCorner;

                    if (FindVisualChildByName<TextBox>(searchBox, "TextBox") is TextBox textBox)
                    {
                        textBox.CornerRadius = pillCorner;
                    }
                }
            }
        }
        #endregion
    }
}