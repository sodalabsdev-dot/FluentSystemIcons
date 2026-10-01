# FluentSystemIcons for WinUI 3
[![NuGet Version](https://img.shields.io/nuget/v/FluentSystemIcons.WinUI.svg)](https://www.nuget.org/packages/FluentSystemIcons.WinUI)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A complete, high-performance toolkit to easily integrate Microsoft's official **Fluent System Icons** into **WinUI 3** (`Windows App SDK`) applications. This repository provides strongly-typed C# constants, font assets, XAML resources, an interactive icon gallery app, and an automated generator utility.

---

## Solution Overview (3 Projects)

The solution (`FluentSystemIcons.sln`) is structured into **3 core projects**, each serving a specific role in the ecosystem:

### 1. `FluentSystemIcons.WinUI` (Library & NuGet Package)
- **Purpose:** The core redistributable class library packaged as a NuGet package.
- **Key Contents:** 
  - TrueType font assets (`.ttf`) for all styles (`Regular`, `Filled`, `Light`, and `Resizable`) stored in `Assets/Fonts/`.
  - XAML Resource Dictionary (`Resources/FluentIcons.xaml`) defining global font families (`FluentIconsRegular`, `FluentIconsFilled`, `FluentIconsLight`, `FluentIconsResizable`).
  - Strongly-typed auto-generated C# static classes containing Unicode character constants (e.g., `FluentSystemIconsRegular.Accessibility24`).
- **Usage:** Consumed by desktop applications to render system icons efficiently with `{x:Bind}` and compiled bindings.

### 2. `FluentSystemIcons.Gallery` (Explorer & Showcase App)
- **Purpose:** A modern WinUI 3 sample and explorer desktop application designed following Windows 11 design guidelines.
- **Key Features:**
  - **Real-Time Search & Filtering:** Instantly search thousands of icons by name, style, or Unicode code.
  - **Visual Customization:** Test icon legibility across multiple grid sizes (small, medium, large).
  - **Theme & Backdrop Toggle:** Switch between Light/Dark themes and test modern Windows backdrops (Mica, MicaAlt, Acrylic) in real time.
  - **Code Snippet Copier:** One-click copy for ready-to-use XAML (`FontIcon`) and C# code snippets.

### 3. `FluentSystemIcons.Generator` (Automation Console Utility)
- **Purpose:** A maintenance and contribution command-line utility used to automate the synchronization of upstream icon assets.
- **Key Functions:**
  - Reads raw `.css` stylesheets from Microsoft's official icon repository.
  - Automatically parses icon names, converting them from `snake_case` (e.g., `ic_fluent_access_time_24_regular`) to `PascalCase` C# constants.
  - Generates updated C# class files (`.cs`) for each font variant, eliminating manual maintenance overhead.

---

## Key Features

- **Complete Icon Catalog:** Access thousands of icons from the official Microsoft Fluent System Icons set.
- **Multiple Styles:** Full native support for **Regular**, **Filled**, **Light**, and **Resizable** font variants.
- **Strongly Typed & Safe:** Avoid magic strings or incorrect hex codes. Enjoy full IntelliSense autocompletion and compile-time safety.
- **Optimized for WinUI 3:** Built specifically for high-performance rendering using `{x:Bind}`.
- **Ready-to-Use Gallery:** Includes a standalone desktop app to explore and test icons visually.

---

## System Requirements

| Requirement | Specification |
|---|---|
| **Development IDE** | Visual Studio 2022 with "Windows application development" workload |
| **Runtime SDK** | .NET 8.0 SDK or later |
| **Target OS / SDK** | Windows 10, version 2004 (Build 19041) or higher |
| **Windows App SDK** | Version 1.0.0 or later |

---

## Building and Packaging

1. Clone or download the repository.
2. Open `FluentSystemIcons.sln` in Visual Studio 2022.
3. Restore NuGet packages for the solution.
4. Set the build configuration to **Release** (`x64` or `AnyCPU`).
5. To build the NuGet package, right-click `FluentSystemIcons.WinUI` and select **Pack**.

---

## Getting Started (Quick Integration)

### Step 1: Register Resources in `App.xaml`
Add the library's resource dictionary to your application's merged dictionaries in `App.xaml`:

```xml
<Application
    x:Class="MyApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                
                <!-- Fluent System Icons Resources -->
                <ResourceDictionary Source="ms-appx:///FluentSystemIcons.WinUI/Resources/FluentIcons.xaml"/>
                
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Step 2: Use Icons in XAML
Import the symbols namespace and use `FontIcon` with `{x:Bind}`:

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

### Step 3: Use Icons in C# Code-Behind
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

## Updating Icons via Generator

If Microsoft releases new icons in the upstream [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) repository:
1. Download updated CSS stylesheets and TTF fonts.
2. Run the **`FluentSystemIcons.Generator`** console project with the new CSS files to regenerate C# class constants.
3. Replace the files in `FluentSystemIcons.WinUI` and repackage.

---

## Credits & License

- Icon assets courtesy of [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons).
- Licensed under the [MIT License](LICENSE).
