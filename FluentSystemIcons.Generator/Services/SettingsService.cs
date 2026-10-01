using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.ApplicationModel;
using System;
using System.IO;
using System.Text.Json;
using Windows.Graphics;
using Microsoft.UI.Windowing;

namespace FluentSystemIcons.Generator.Services
{
    public static class SettingsService
    {
        private const string SettingsFileName = "generator_settings.json";
        private static readonly string UnpackagedSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);

        private class SettingsData
        {
            public string Theme { get; set; } = "Default";
            public string Backdrop { get; set; } = "Mica";
            public string Language { get; set; } = "System";
            public string LastSyncVersion { get; set; } = "Desconocida (No sincronizada)";
            public int? WindowX { get; set; }
            public int? WindowY { get; set; }
            public int? WindowWidth { get; set; }
            public int? WindowHeight { get; set; }
            public bool IsMaximized { get; set; } = false;
        }

        private static bool IsPackaged()
        {
            try
            {
                return Package.Current != null && !string.IsNullOrEmpty(Package.Current.Id.FamilyName);
            }
            catch
            {
                return false;
            }
        }

        private static SettingsData LoadData()
        {
            if (IsPackaged())
            {
                var data = new SettingsData();
                var localSettings = ApplicationData.Current.LocalSettings;

                if (localSettings.Values.TryGetValue("Theme", out var themeValue) && themeValue is string t) data.Theme = t;
                if (localSettings.Values.TryGetValue("Backdrop", out var backdropValue) && backdropValue is string b) data.Backdrop = b;
                if (localSettings.Values.TryGetValue("Language", out var langValue) && langValue is string l) data.Language = l;
                if (localSettings.Values.TryGetValue("LastSyncVersion", out var versionValue) && versionValue is string v) data.LastSyncVersion = v;

                if (localSettings.Values.TryGetValue("WindowX", out var x) && x is int xi) data.WindowX = xi;
                if (localSettings.Values.TryGetValue("WindowY", out var y) && y is int yi) data.WindowY = yi;
                if (localSettings.Values.TryGetValue("WindowWidth", out var w) && w is int wi) data.WindowWidth = wi;
                if (localSettings.Values.TryGetValue("WindowHeight", out var h) && h is int hi) data.WindowHeight = hi;
                if (localSettings.Values.TryGetValue("IsMaximized", out var max) && max is bool mb) data.IsMaximized = mb;

                return data;
            }
            else
            {
                if (!File.Exists(UnpackagedSettingsPath))
                {
                    return new SettingsData();
                }

                try
                {
                    string json = File.ReadAllText(UnpackagedSettingsPath);
                    return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                }
                catch
                {
                    return new SettingsData();
                }
            }
        }

        private static void SaveData(SettingsData data)
        {
            if (IsPackaged())
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["Theme"] = data.Theme;
                localSettings.Values["Backdrop"] = data.Backdrop;
                localSettings.Values["Language"] = data.Language;
                localSettings.Values["LastSyncVersion"] = data.LastSyncVersion;

                if (data.WindowX.HasValue) localSettings.Values["WindowX"] = data.WindowX.Value;
                if (data.WindowY.HasValue) localSettings.Values["WindowY"] = data.WindowY.Value;
                if (data.WindowWidth.HasValue) localSettings.Values["WindowWidth"] = data.WindowWidth.Value;
                if (data.WindowHeight.HasValue) localSettings.Values["WindowHeight"] = data.WindowHeight.Value;
                localSettings.Values["IsMaximized"] = data.IsMaximized;
            }
            else
            {
                try
                {
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(UnpackagedSettingsPath, json);
                }
                catch
                {
                    // Previene caídas en caso de restricciones de escritura en disco
                }
            }
        }

        public static ElementTheme LoadTheme()
        {
            var data = LoadData();
            return data.Theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        public static void SaveTheme(ElementTheme theme)
        {
            var data = LoadData();
            data.Theme = theme.ToString();
            SaveData(data);
        }

        public static BackdropType LoadBackdrop()
        {
            var data = LoadData();
            if (Enum.TryParse<BackdropType>(data.Backdrop, out var backdrop))
            {
                return backdrop;
            }
            return BackdropType.Mica;
        }

        public static void SaveBackdrop(BackdropType backdrop)
        {
            var data = LoadData();
            data.Backdrop = backdrop.ToString();
            SaveData(data);
        }

        public static string LoadLanguage()
        {
            var data = LoadData();
            return data.Language;
        }

        public static void SaveLanguage(string langTag)
        {
            var data = LoadData();
            data.Language = langTag;
            SaveData(data);
        }

        public static string LoadLastSyncVersion()
        {
            var data = LoadData();
            return data.LastSyncVersion;
        }

        public static void SaveLastSyncVersion(string version)
        {
            var data = LoadData();
            data.LastSyncVersion = version;
            SaveData(data);
        }

        public static RectInt32? LoadWindowLayout()
        {
            var data = LoadData();
            if (data.WindowX.HasValue && data.WindowY.HasValue && data.WindowWidth.HasValue && data.WindowHeight.HasValue)
            {
                return new RectInt32(data.WindowX.Value, data.WindowY.Value, data.WindowWidth.Value, data.WindowHeight.Value);
            }
            return null;
        }

        public static bool LoadIsMaximized()
        {
            var data = LoadData();
            return data.IsMaximized;
        }

        public static void SaveWindowLayout(AppWindow appWindow, RectInt32 normalBounds)
        {
            var data = LoadData();

            data.WindowX = normalBounds.X;
            data.WindowY = normalBounds.Y;
            data.WindowWidth = normalBounds.Width;
            data.WindowHeight = normalBounds.Height;

            if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                data.IsMaximized = overlappedPresenter.State == OverlappedPresenterState.Maximized;
            }
            else
            {
                data.IsMaximized = false;
            }

            SaveData(data);
        }
    }
}