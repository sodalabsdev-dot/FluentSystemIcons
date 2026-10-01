using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.ApplicationModel;

namespace FluentSystemIcons.Generator.UserControls
{
    public sealed partial class SettingsControl : UserControl
    {
        public event Action<ElementTheme>? ThemeChanged;

        public event Action<BackdropType>? BackdropChanged;

        public event Action<string>? LanguageChanged;

        public SettingsControl()
        {
            this.InitializeComponent();
            AppVersionTextBlock.Text = GetAppVersion();
        }

        private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is RadioButtons radioButtons && radioButtons.SelectedItem is RadioButton selectedRadioButton)
            {
                var theme = (string)selectedRadioButton.Tag switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default,
                };

                ThemeChanged?.Invoke(theme);
            }
        }

        private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (Enum.TryParse<BackdropType>((string)selectedItem.Tag, out var backdrop))
                {
                    BackdropChanged?.Invoke(backdrop);
                }
            }
        }

        public void InitializeTheme(ElementTheme theme)
        {
            switch (theme)
            {
                case ElementTheme.Light:
                    ThemeRadioButtons.SelectedIndex = 0;
                    break;
                case ElementTheme.Dark:
                    ThemeRadioButtons.SelectedIndex = 1;
                    break;
                case ElementTheme.Default:
                default:
                    ThemeRadioButtons.SelectedIndex = 2;
                    break;
            }
        }

        public void InitializeBackdrop(BackdropType backdrop)
        {
            switch (backdrop)
            {
                case BackdropType.MicaAlt:
                    BackdropComboBox.SelectedIndex = 1;
                    break;
                case BackdropType.Acrylic:
                    BackdropComboBox.SelectedIndex = 2;
                    break;
                case BackdropType.None:
                    BackdropComboBox.SelectedIndex = 3;
                    break;
                case BackdropType.Mica:
                default:
                    BackdropComboBox.SelectedIndex = 0;
                    break;
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var langTag = (string)selectedItem.Tag;
                LanguageChanged?.Invoke(langTag);
            }
        }

        public void InitializeLanguage(string langTag)
        {
            var itemToSelect = LanguageComboBox.Items
                                .OfType<ComboBoxItem>()
                                .FirstOrDefault(item => (string)item.Tag == langTag);

            if (itemToSelect != null)
            {
                LanguageComboBox.SelectedItem = itemToSelect;
            }
            else
            {
                LanguageComboBox.SelectedIndex = 0;
            }
        }

        private string GetAppVersion()
        {
            try
            {
                var packageVersion = Package.Current.Id.Version;
                return $"v{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}";
            }
            catch (Exception)
            {
                return "v1.0.0 (Debug)";
            }
        }
    }
}