using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using osu_filterer.ViewModels;
namespace osu_filterer.Views;

public partial class MainWindow : Window
{
    public String osuFile;
    public MainWindow()
    {
        InitializeComponent();
        FilePathTextBox.Text = "";
        osuFile = "";
    }

    public async void FileExplorer(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        try
        {
            TopLevel topLevel = GetTopLevel(this);
            FolderPickerOpenOptions options = new FolderPickerOpenOptions { Title = "Hi", AllowMultiple = false };
            IReadOnlyList<IStorageFolder> tempFolder = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

            if (tempFolder.Count > 0)
            {
                osuFile = tempFolder[0].Path.LocalPath;
                FilePathTextBox.Text = osuFile;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e}");
        }
    }

    public async void RunFilterer(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        String path = Path.Join(osuFile, "Songs");
        if (!Path.Exists(path))
        {
            Console.Write("Choose a valid path.");
            var dialog = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                Content = new TextBlock { Text = "Select a valid file path.", Margin = new Avalonia.Thickness(20) }
            };
            await dialog.ShowDialog(dialog);
        }
        else
        {
            MainWindowViewModel.FilterImages(osuFile);
        }
    }
    public async void UndoFilterer(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        String path = Path.Join(osuFile, "Songs");
        if (!Path.Exists(path))
        {
            Console.Write("Choose a valid path.");
            var dialog = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                Content = new TextBlock { Text = "Select a valid file path.", Margin = new Avalonia.Thickness(20) }
            };
            await dialog.ShowDialog(dialog);
        }
        else
        {
            MainWindowViewModel.UndoFilter(osuFile);
        }
    }
}