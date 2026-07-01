using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using System.IO;
using System;

namespace osu_filterer.Dependencies;
public static class Helper
{
    // Run this if you are building manually. This should point to .\osu-filterer\osu-filterer. if you arent building with avalonia then adjust accordingly.:
    // public static readonly string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    public static readonly string projectRoot = Path.GetFullPath(AppContext.BaseDirectory);
    public static void ShowError(string message)
    {
        Window parent = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
    ?? throw new InvalidOperationException("Main window not available.");
        var dialog = new Window
        {
            Title = "Error",
            Width = 400,
            Height = 200,
            Content = new SelectableTextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(20)
            }
        };

        dialog.Show(parent);
    }
}