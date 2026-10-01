# User Manual & Technical Guide: FluentSystemIcons.WinUI

This document describes in detail the system requirements, installation, three-project solution architecture, and practical integration of the **FluentSystemIcons.WinUI** library in desktop applications built with **WinUI 3** (`Windows App SDK`).

---

## 1. Solution Architecture (3 Projects)

The solution (`FluentSystemIcons.sln`) is composed of three interconnected projects:

1. **`FluentSystemIcons.WinUI`** (Library & NuGet Package):
   - Contains TrueType font asset files (`.ttf`) in `Assets/Fonts/` (`Regular`, `Filled`, `Light`, `Resizable`).
   - Provides the XAML Resource Dictionary (`Resources/FluentIcons.xaml`) and strongly-typed C# static classes containing character constants (e.g., `FluentSystemIconsRegular.Accessibility24`).
2. **`FluentSystemIcons.Gallery`** (Explorer & Showcase App):
   - A modern WinUI 3 desktop application designed to visually explore the complete icon catalog.
   - Features real-time search, theme toggling (Light/Dark), Windows backdrop testing (Mica, MicaAlt, Acrylic), and instant code snippet copying for XAML and C#.
3. **`FluentSystemIcons.Generator`** (Automation Console Utility):
   - A command-line utility used to automate asset synchronization. It parses official Microsoft `.css` stylesheets and automatically generates C# static classes (`.cs`), eliminating manual maintenance.

---

## 2. System Requirements

| Component | Requirement |
|---|---|
| **Development IDE** | Visual Studio 2022 with the "Windows application development" workload (Windows App SDK) |
| **Runtime Framework** | .NET 8.0 SDK or later |
| **Target OS / SDK** | Windows 10, version 2004 (Build 19041) or higher |
| **Windows App SDK** | Version 1.0.0 or later |

---

## 3. Installation

You can install the NuGet package in your target project using either the NuGet Package Manager or the .NET CLI:

### NuGet Package Manager
```shell
Install-Package FluentSystemIcons.WinUI
```

### .NET CLI
```shell
dotnet add package FluentSystemIcons.WinUI
```

---

## 4. Initial Setup (Resource Registration)

For the WinUI 3 rendering engine to display the embedded font assets, you must merge the library's resource dictionary into your application's global `App.xaml` file:

**File: `App.xaml`**
```xml
<Application
    x:Class="MyApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                
                <!-- Fluent System Icons Resource Dictionary -->
                <ResourceDictionary Source="ms-appx:///FluentSystemIcons.WinUI/Resources/FluentIcons.xaml"/>
                
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## 5. Usage Examples

### Usage in XAML
By adding the symbols namespace to your XAML page header, you can use `FontIcon` along with compiled bindings (`{x:Bind}`):

```xml
<Page
    x:Class="MyApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:symbols="using:FluentSystemIcons">

    <StackPanel Spacing="16" Orientation="Horizontal" HorizontalAlignment="Center" VerticalAlignment="Center">
        <!-- Regular Style -->
        <FontIcon FontFamily="{StaticResource FluentIconsRegular}"
                  Glyph="{x:Bind symbols:FluentSystemIconsRegular.Accessibility24, Mode=OneTime}"
                  FontSize="48"/>

        <!-- Filled Style -->
        <FontIcon FontFamily="{StaticResource FluentIconsFilled}"
                  Glyph="{x:Bind symbols:FluentSystemIconsFilled.Accessibility24, Mode=OneTime}"
                  FontSize="48"/>
    </StackPanel>
</Page>
```

### Usage in C# Code-Behind
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

## 6. Updating Icons with the Generator

If Microsoft updates the official [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) repository:
1. Download the latest assets and locate the `.css` files in the `fonts/` folder.
2. Build and run the **`FluentSystemIcons.Generator`** console project passing the new CSS styles.
3. Replace the generated class files in the `FluentSystemIcons.WinUI` project and repackage the library.

---

## 7. License
This project is distributed under the [MIT License](LICENSE).
