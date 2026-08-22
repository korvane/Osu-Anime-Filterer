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
using Avalonia.Controls.Documents;
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
                if(message.Equals(""))
                {
                    ConsoleGUI.Text = "";
                }
                else
                {
                    ConsoleGUI.Text += message + Environment.NewLine;
                }
                ConsoleGUI.CaretIndex = ConsoleGUI.Text.Length;
                ConsoleGUI.ScrollToLine(ConsoleGUI.GetLineCount() - 1);
            });
        Helper.SetProgress(progress);
        Loaded += async(_,_) =>
        {
                EnableButtons(false);

                try
                {
                    await Helper.DownloadPython(File.Exists(installCheck));
                }
                finally
                {
                    EnableButtons(true);
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
            Helper.LogMessage($"Error: {e}");
        }
    }

    public async void HandleFilter(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        Helper.LogMessage("");
        EnableButtons(false);
        MainWindowViewModel.ImagePaths = new List<string>(0);
        if (!Path.Exists(osuFile))
        {
            Helper.LogMessage("Choose a valid path - e.g. your Osu! root folder (...\\Osu!\\)");
        }
        else
        {
            MainWindowViewModel.ImagePaths = new List<string>();
            Helper.LogMessage($"Gathering images at {osuFile}\n");
            await Task.Run(() => MainWindowViewModel.GatherImages(osuFile));
            if(MainWindowViewModel.ImagePaths.Count == 0)
            {
                Helper.LogMessage("beatmaps are already filtered!! :P");
            }
            else
            {
                Helper.LogMessage($"Done with gathering images at {osuFile}\n");
                Helper.LogMessage($"Start Model: {osuFile}\n\n...\n\n");
                List<ModelOutputItem> unfilteredPaths = await MainWindowViewModel.RunModel();
                Helper.LogMessage($"\nFilter and Replace: {osuFile}\n");
                await Task.Run(()=>MainWindowViewModel.FilterImages(unfilteredPaths));
                Helper.LogMessage($"\nreplacement done :P");
            }
        }
        EnableButtons(true);
    }
    public async void HandleUnfilter(object? obj, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        Helper.LogMessage("");
        EnableButtons(false);
        if (!Path.Exists(osuFile))
        {
            Helper.LogMessage("Choose a valid directory - e.g. your Osu! root folder (...\\Osu!\\)");
        }
        else
        {
            Helper.LogMessage($"Start unfilter at {osuFile}\n");
            await Task.Run(()=>MainWindowViewModel.HandleUnfilter(osuFile));
            Helper.LogMessage($"Done with unfilter at {osuFile}");
        }
        EnableButtons(true);
    }
    private void EnableButtons(bool enable)
    {
        if (enable)
        {
            ChooseFile.IsEnabled = true;
            Filter.IsEnabled = true;
            Unfilter.IsEnabled = true;
        }
        else
        {
            ChooseFile.IsEnabled = false;
            Filter.IsEnabled = false;
            Unfilter.IsEnabled = false;
        }
    }
}
