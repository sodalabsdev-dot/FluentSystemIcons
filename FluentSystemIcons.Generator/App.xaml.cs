using FluentSystemIcons.Generator.Services;
using Windows.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace FluentSystemIcons.Generator
{
    /// <summary>
    /// Proporciona un comportamiento específico de la aplicación para complementar la clase Application predeterminada.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Inicializa el objeto de aplicación singleton. Esta es la primera línea de código escrito
        /// ejecutado y, como tal, es el equivalente lógico de main() o WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // APLICAR IDIOMA GUARDADO AL INICIO
            var savedLang = SettingsService.LoadLanguage();
            if (!string.IsNullOrEmpty(savedLang) && savedLang != "System")
            {
                ApplicationLanguages.PrimaryLanguageOverride = savedLang;
            }
        }

        /// <summary>
        /// Se invoca cuando la aplicación se inicia normalmente con el usuario final.
        /// </summary>
        /// <param name="args">Detalles sobre la solicitud de inicio y el proceso.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}