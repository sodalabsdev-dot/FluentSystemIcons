# Manual de Usuario y Guía Técnica: FluentSystemIcons.WinUI

Este documento describe detalladamente los requisitos, la instalación, la arquitectura de los tres proyectos de la solución y la integración práctica de la biblioteca **FluentSystemIcons.WinUI** en aplicaciones de escritorio construidas con **WinUI 3** (`Windows App SDK`).

---

## 1. Arquitectura de la Solución (3 Proyectos)

La solución (`FluentSystemIcons.sln`) se compone de tres proyectos interconectados:

1. **`FluentSystemIcons.WinUI`** (Biblioteca y Paquete NuGet):
   - Contiene los archivos de fuentes TrueType (`.ttf`) en `Assets/Fonts/` (`Regular`, `Filled`, `Light`, `Resizable`).
   - Proporciona el Diccionario de Recursos XAML (`Resources/FluentIcons.xaml`) y clases estáticas con constantes de C# fuertemente tipadas (ej. `FluentSystemIconsRegular.Accessibility24`).
2. **`FluentSystemIcons.Gallery`** (Aplicación de Demostración y Explorador):
   - Una aplicación WinUI 3 moderna para explorar visualmente el catálogo completo de iconos.
   - Permite búsqueda en tiempo real, cambio de tema (Claro/Oscuro), prueba de fondos de Windows (Mica, MicaAlt, Acrylic) y copiado instantáneo de fragmentos de código XAML y C#.
3. **`FluentSystemIcons.Generator`** (Utilidad de Consola):
   - Herramienta para automatizar la sincronización de iconos. Analiza los archivos `.css` oficiales de Microsoft y genera automáticamente las clases estáticas en C# (`.cs`), evitando el mantenimiento manual.

---

## 2. Requisitos del Sistema

| Componente | Requisito |
|---|---|
| **Entorno de Desarrollo** | Visual Studio 2022 con la carga de trabajo "Desarrollo de aplicaciones para Windows" (Windows App SDK) |
| **Framework de Ejecución** | .NET 8.0 SDK o superior |
| **Sistema Operativo / SDK** | Windows 10, version 2004 (Build 19041) o superior |
| **Windows App SDK** | Versión 1.0.0 o superior |

---

## 3. Instalación

Puedes instalar el paquete NuGet en tu proyecto de destino utilizando el Administrador de Paquetes NuGet o la CLI de .NET:

### Administrador de Paquetes NuGet
```shell
Install-Package FluentSystemIcons.WinUI
```

### .NET CLI
```shell
dotnet add package FluentSystemIcons.WinUI
```

---

## 4. Configuración Inicial (Registro de Recursos)

Para que el motor de WinUI 3 pueda renderizar las fuentes incrustadas, debes fusionar el diccionario de recursos de la biblioteca en el archivo global `App.xaml` de tu aplicación:

**Archivo: `App.xaml`**
```xml
<Application
    x:Class="MyApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                
                <!-- Diccionario de recursos de Fluent System Icons -->
                <ResourceDictionary Source="ms-appx:///FluentSystemIcons.WinUI/Resources/FluentIcons.xaml"/>
                
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## 5. Ejemplos de Uso

### Uso en XAML
Agregando el espacio de nombres de los símbolos en tu página XAML, puedes utilizar `FontIcon` junto con enlaces compilados (`{x:Bind}`):

```xml
<Page
    x:Class="MyApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:symbols="using:FluentSystemIcons">

    <StackPanel Spacing="16" Orientation="Horizontal" HorizontalAlignment="Center" VerticalAlignment="Center">
        <!-- Estilo Regular -->
        <FontIcon FontFamily="{StaticResource FluentIconsRegular}"
                  Glyph="{x:Bind symbols:FluentSystemIconsRegular.Accessibility24, Mode=OneTime}"
                  FontSize="48"/>

        <!-- Estilo Filled -->
        <FontIcon FontFamily="{StaticResource FluentIconsFilled}"
                  Glyph="{x:Bind symbols:FluentSystemIconsFilled.Accessibility24, Mode=OneTime}"
                  FontSize="48"/>
    </StackPanel>
</Page>
```

### Uso en C# (Code-Behind)
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FluentSystemIcons;

namespace MyApp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) =>
            {
                if (Application.Current.Resources.TryGetValue("FluentIconsRegular", out var res) && res is FontFamily font)
                {
                    RootContainer.Children.Add(new FontIcon
                    {
                        FontFamily = font,
                        Glyph = FluentSystemIconsRegular.Accessibility24,
                        FontSize = 48
                    });
                }
            };
        }
    }
}
```

---

## 6. Actualización de Iconos con el Generador

Si Microsoft actualiza el repositorio oficial de [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons):
1. Descarga los activos más recientes y localiza los archivos `.css` en la carpeta `fonts/`.
2. Compila y ejecuta el proyecto **`FluentSystemIcons.Generator`** pasando los nuevos estilos CSS.
3. Reemplaza los archivos de clases resultantes en el proyecto `FluentSystemIcons.WinUI` y reempaqueta la biblioteca.

---

## 7. Licencia
Este proyecto se distribuye bajo la [Licencia MIT](LICENSE).
