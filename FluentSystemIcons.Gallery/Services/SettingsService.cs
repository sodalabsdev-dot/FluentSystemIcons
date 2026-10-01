using FluentSystemIcons.Gallery.UserControls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics;
using Windows.Storage;

namespace FluentSystemIcons.Gallery.Services
{
    public enum BackdropType
    {
        Mica,
        MicaAlt,
        Acrylic,
        None
    }

    public static class SettingsService
    {
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        private const string GridSizeModeKey = "GridSizeMode";
        private const string ThemeKey = "AppTheme";
        private const string BackdropKey = "AppBackdrop";
        private const string WindowPosXKey = "WindowPositionX";
        private const string WindowPosYKey = "WindowPositionY";
        private const string WindowWidthKey = "WindowWidth";
        private const string WindowHeightKey = "WindowHeight";
        private const string LanguageKey = "AppLanguage";
        private const string FavoritesKey = "AppFavorites";
        private const string IsMaximizedKey = "AppIsMaximized";

        #region Posición y Tamaño de la Ventana

        public static void SaveWindowLayout(AppWindow appWindow, RectInt32 normalBounds)
        {
            if (appWindow == null) return;
            LocalSettings.Values[WindowPosXKey] = normalBounds.X;
            LocalSettings.Values[WindowPosYKey] = normalBounds.Y;
            LocalSettings.Values[WindowWidthKey] = normalBounds.Width;
            LocalSettings.Values[WindowHeightKey] = normalBounds.Height;

            if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                LocalSettings.Values[IsMaximizedKey] = overlappedPresenter.State == OverlappedPresenterState.Maximized;
            }
            else
            {
                LocalSettings.Values[IsMaximizedKey] = false;
            }
        }

        public static RectInt32? LoadWindowLayout()
        {
            if (LocalSettings.Values.ContainsKey(WindowWidthKey) &&
                LocalSettings.Values.ContainsKey(WindowHeightKey) &&
                LocalSettings.Values.ContainsKey(WindowPosXKey) &&
                LocalSettings.Values.ContainsKey(WindowPosYKey))
            {
                int width = (int)LocalSettings.Values[WindowWidthKey];
                int height = (int)LocalSettings.Values[WindowHeightKey];
                int posX = (int)LocalSettings.Values[WindowPosXKey];
                int posY = (int)LocalSettings.Values[WindowPosYKey];

                return new RectInt32(posX, posY, width, height);
            }
            return null;
        }

        public static bool LoadIsMaximized()
        {
            if (LocalSettings.Values.TryGetValue(IsMaximizedKey, out object? value) && value is bool isMaximized)
            {
                return isMaximized;
            }
            return false;
        }

        #endregion

        #region Modo de Tamaño de la Cuadrícula
        public static void SaveGridSizeMode(GridSizeMode mode)
        {
            LocalSettings.Values[GridSizeModeKey] = mode.ToString();
        }

        public static GridSizeMode LoadGridSizeMode()
        {
            if (LocalSettings.Values.TryGetValue(GridSizeModeKey, out object? value))
            {
                if (value is string modeString)
                {
                    if (System.Enum.TryParse<GridSizeMode>(modeString, out var mode))
                    {
                        return mode;
                    }
                }
            }
            return GridSizeMode.Medium;
        }

        #endregion

        #region Tema de la Aplicación
        public static void SaveTheme(ElementTheme theme)
        {
            LocalSettings.Values[ThemeKey] = theme.ToString();
        }

        public static ElementTheme LoadTheme()
        {
            if (LocalSettings.Values.TryGetValue(ThemeKey, out object? value))
            {
                if (value is string themeString)
                {
                    if (System.Enum.TryParse<ElementTheme>(themeString, out var theme))
                    {
                        return theme;
                    }
                }
            }
            return ElementTheme.Default;
        }

        #endregion

        #region Fondo de la Aplicación
        public static void SaveBackdrop(BackdropType backdrop)
        {
            LocalSettings.Values[BackdropKey] = backdrop.ToString();
        }

        public static BackdropType LoadBackdrop()
        {
            if (LocalSettings.Values.TryGetValue(BackdropKey, out object? value))
            {
                if (value is string backdropString)
                {
                    if (System.Enum.TryParse<BackdropType>(backdropString, out var backdrop))
                    {
                        return backdrop;
                    }
                }
            }
            return BackdropType.Mica;
        }

        #endregion

        #region Idioma de la Aplicación
        public static void SaveLanguage(string langTag)
        {
            LocalSettings.Values[LanguageKey] = langTag;
        }

        public static string LoadLanguage()
        {
            if (LocalSettings.Values.TryGetValue(LanguageKey, out object? value) && value is string langTag)
            {
                return langTag;
            }
            return "System";
        }

        #endregion

        #region Sistema de Favoritos
        public static void SaveFavorites(List<string> favorites)
        {
            LocalSettings.Values[FavoritesKey] = string.Join(",", favorites);
        }

        public static List<string> LoadFavorites()
        {
            if (LocalSettings.Values.TryGetValue(FavoritesKey, out object? value) && value is string favString)
            {
                if (string.IsNullOrWhiteSpace(favString))
                {
                    return new List<string>();
                }
                return favString.Split(',').ToList();
            }
            return new List<string>();
        }

        #endregion
    }
}