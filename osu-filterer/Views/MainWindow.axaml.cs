using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using osu_filterer.Dependencies;
using osu_filterer.ViewModels;
using System.Linq;
namespace osu_filterer.Views;

public partial class MainWindow : Window
{
    public String osuFile;
    public Progress<string> progress {get; set;}

    public MainWindow()
    {
        InitializeComponent();
        FilePathTextBox.Text = "";
        ConsoleGUI.Text = "";
        osuFile = "";
        string installCheck = Path.Combine(Helper.projectRoot, "Dependencies", "python", "CompletedInstall.txt");
        progress = new Progress<string>(message =>
            {
                ConsoleGUI.Text += message + Environment.NewLine;
                ConsoleGUI.CaretIndex = ConsoleGUI.Text.Length;
                ConsoleGUI.ScrollToLine(ConsoleGUI.GetLineCount() - 1);
            });
        Loaded += async(_,_) =>
        {
                ChooseFile.IsEnabled = false;
                Filter.IsEnabled = false;
                Unfilter.IsEnabled = false;

                try
                {
                    await Helper.DownloadPython(File.Exists(installCheck), progress);
                }
                finally
                {
                    ChooseFile.IsEnabled = true;
                    Filter.IsEnabled = true;
                    Unfilter.IsEnabled = true;
                }
        };
        
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