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
    public MainWindow()
    {
        InitializeComponent();
        FilePathTextBox.Text = "";
        osuFile = "";
        string install = Path.Combine(Helper.projectRoot, "Dependencies", "python", "CompletedInstall.txt");
        Loaded += async (_, _) =>
        {
            await DownloadPython(Path.Exists(install));
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
    private async Task DownloadPython(bool completed)
    {
        if (completed)
        {
            Console.WriteLine("Python dependencies already installed");
            return;
        } 
        string dependencies = Path.Combine(Helper.projectRoot, "Dependencies");
        string pythonDir = Path.Combine(dependencies, "python");
        string pythonexe = Path.Combine(pythonDir, "python.exe");
        string getPip = Path.Combine(pythonDir, "get-pip.py");
        string requirements = Path.Combine(dependencies, "requirements.txt");
        string install = Path.Combine(pythonDir, "CompletedInstall.txt");

        //install embeddable python
        string zipPath = Path.Combine(Helper.projectRoot, "python.zip");
        using (HttpClient client = new())
        {
            await using var stream = await client.GetStreamAsync("https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip");
            await using var file = File.Create(zipPath);
            await stream.CopyToAsync(file);
        }
        

        //extract 
        if (Directory.Exists(pythonDir))
            Directory.Delete(pythonDir, true);
        try
        {
            ZipFile.ExtractToDirectory(zipPath, pythonDir);
        }
        finally
        {
            File.Delete(zipPath);
        }

        //download get-pip
        using (HttpClient client = new())
        {
            await using var stream = await client.GetStreamAsync("https://bootstrap.pypa.io/get-pip.py");
            await using var file = File.Create(getPip);
            await stream.CopyToAsync(file);
        }

        //uncomment python313._pth import for installing pip
        string pthFile = Directory.GetFiles(pythonDir, "python*._pth").Single();
        string contents = File.ReadAllText(pthFile);
        contents = contents.Replace("#import site", "import site");
        File.WriteAllText(pthFile, contents);

        //install pip
        var installPip = new ProcessStartInfo
        {
            FileName = pythonexe,
            Arguments = getPip,
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
        if (process.ExitCode != 0) throw new Exception(error);

        //install requirements
        var installRequirements = new ProcessStartInfo
        {
            FileName = pythonexe,
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
        if (process.ExitCode != 0) throw new Exception(error);
        Console.WriteLine("Finished Python Dependencies install!");
        File.WriteAllText(install, "");
    }
}