using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace FluentSystemIcons.Gallery.Models
{
    public class IconInfo
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }
        public string Glyph { get; set; }

        public string XamlCode { get; set; }

        public FontFamily FontFamily { get; set; }

        public string FontKey { get; set; }

        public string Category { get; set; }

        public string Size { get; set; }

        public List<string> Keywords { get; set; } = new();

        public IconInfo()
        {
            Name = string.Empty;
            DisplayName = string.Empty;
            Glyph = string.Empty;
            XamlCode = string.Empty;
            FontKey = string.Empty;
            Category = string.Empty;
            Size = string.Empty;
            FontFamily = null!;
        }
    }
}