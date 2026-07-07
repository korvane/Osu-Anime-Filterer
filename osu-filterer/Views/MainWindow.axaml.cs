using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using osu_filterer.Dependencies;
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
        string python = Path.Combine(Helper.projectRoot, "Dependencies", "python", "python.exe");
        string get_pip = Path.Combine(Helper.projectRoot, "Dependencies", "python", "get-pip.py");
        string requirements = Path.Combine(Helper.projectRoot, "Dependencies", "requirements.txt");
        string install = Path.Combine(Helper.projectRoot, "Dependencies", "CompletedInstall.txt");
        if(!Path.Exists(install))
        {
            var installPip = new ProcessStartInfo
            {
                FileName = python,
                Arguments = get_pip,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(installPip)?? throw new Exception();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine(error);
            Console.WriteLine(output);

            var installRequirements = new ProcessStartInfo
            {
                FileName = python,
                Arguments = $"-m pip install -r {requirements}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(installRequirements)?? throw new Exception();
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine(error);
            Console.WriteLine(output);
            File.WriteAllText(install, "");
        }
        else
        {
            Console.WriteLine("na");
        }
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