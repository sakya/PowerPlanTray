using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace PowerPlanTray.Utils;

public static class MaterialIconsHelper
{
    private static readonly Dictionary<string, Bitmap> Cache = new();
    private static readonly Size DefaultSize = new(128, 128);

    public static Bitmap GetBitmap(string icon)
    {
        return GetBitmap(icon, DefaultSize);
    }

    public static Bitmap GetBitmap(string icon, Size size)
    {
        var isDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        var key = $"{(isDarkTheme ? "dark" : "light")}:{icon}:{size.Width}x{size.Height}";
        if (Cache.TryGetValue(key, out var fromCache)) {
            return fromCache;
        }

        var brush = isDarkTheme
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);

        var tCanvas = new Canvas();
        RenderOptions.SetBitmapInterpolationMode(tCanvas, BitmapInterpolationMode.HighQuality);
        var image = new Image()
        {
            Source = new Projektanker.Icons.Avalonia.IconImage() { Value = icon, Brush = brush},
            Width = size.Width,
            Height = size.Height
        };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        tCanvas.Children.Add(image);
        tCanvas.Arrange(new Rect(size));

        var pixelSize = new PixelSize((int)tCanvas.Bounds.Width, (int)tCanvas.Bounds.Height);
        using var renderBitmap = new RenderTargetBitmap(pixelSize);
        renderBitmap.Render(tCanvas);

        using var ms = new MemoryStream();
        renderBitmap.Save(ms);
        ms.Position = 0;
        var res = new Bitmap(ms);
        Cache.Add(key, res);

        return res;
    }
}