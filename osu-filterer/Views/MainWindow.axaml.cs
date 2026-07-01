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
            TopLevel topLevel = GetTopLevel(this) ?? throw new Exception("Top level is null.");
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

    public async void HandleFilter(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        String path = Path.Join(osuFile, "Songs");
        if (!Path.Exists(path))
        {
            Console.WriteLine("Choose a valid path.");
        }
        else
        {
            MainWindowViewModel.HandleFilter(osuFile);
        }
    }
    public async void HandleUnfilter(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        String path = Path.Join(osuFile, "Songs");
        if (!Path.Exists(path))
        {
            Console.WriteLine("Choose a valid path.");
        }
        else
        {
            MainWindowViewModel.HandleUnfilter(osuFile);
        }
    }
}