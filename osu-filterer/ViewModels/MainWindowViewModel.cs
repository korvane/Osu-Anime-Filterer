using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using osu_filterer.Dependencies;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using osu_filterer.Views;

namespace osu_filterer.ViewModels;

public class ModelOutputItem
{
    public required string Path { get; set; }
    public bool Prediction { get; set; }
    public double Probability { get; set; }
    public string Name => System.IO.Path.GetFileName(Path);
}
public partial class MainWindowViewModel : ViewModelBase
{
    public static async void HandleFilter(string path)
    {
        path = Path.Join(path, "Songs");
        if (!Path.Exists(path))
        {
            Console.Write("Choose a valid path.");
        }
        List<string> imagePaths = new List<string>();
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                if (IsFilteredDir(dir))
                {
                    Console.WriteLine($"Beatmap is already filtered: {System.IO.Path.GetFileName(dir)}");
                    continue;
                }
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                    {
                        imagePaths.Add(file);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Helper.ShowError(e.ToString());
            Console.WriteLine($"Error: {e}");
        }
        if(imagePaths.Count == 0)
        {
            Console.WriteLine("beatmaps are filtered!! :P");
            return;
        }
        Console.WriteLine($"Start Model: {path}");
        List<ModelOutputItem> unfilteredPaths = RunModel(imagePaths);
        Console.WriteLine($"Filter and Replace: {path}");
        FilterFiles(unfilteredPaths);
        Console.WriteLine($"replacement done :P");
    }

    public static void HandleUnfilter(string path)
    {
        path = Path.Join(path, "Songs");
        if (!Path.Exists(path))
        {
            throw new Exception("Choose a valid path.");
        }
        try
        {
            Console.WriteLine($"Start unfilter at {path}");
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    if (file.EndsWith(".filtered"))
                    {
                        File.Delete(file.Substring(0, file.IndexOf(".filtered")));
                        File.Move(file, file.Substring(0, file.IndexOf(".filtered")));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Helper.ShowError(e.ToString());
            Console.WriteLine($"error: {e}");
        }
        Console.WriteLine($"Done with unfilter at {path}");
    }

    private static List<ModelOutputItem> RunModel(List<string> files)
    {
        var payload = new { images = files };
        string json = JsonSerializer.Serialize(payload);
        string python = $"{Helper.projectRoot}\\dependencies\\python\\python.exe";

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = python,
                Arguments = $"{Helper.projectRoot}\\dependencies\\is_anime_model.py",
                WorkingDirectory=Helper.projectRoot,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(psi) ?? throw new Exception("ProcessStartInfo cannot be null.");
            process.StandardInput.WriteLine(json);
            process.StandardInput.Close();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine("END MODEL");
            List<ModelOutputItem> modelOutput = JsonSerializer.Deserialize<List<ModelOutputItem>>(output) ?? throw new Exception("output returned null.");
            return modelOutput;
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            Helper.ShowError($"This app cannot run as a standalone!\nProgram must be ran from its original folder.\n\nRemember to change the rootDirectory string if necessary.\n\ncurrent directory: {Helper.projectRoot}\n\n python directory:{python}\n\n{e.ToString()}");
            Console.WriteLine(e);
        }
        catch (Exception e)
        {
            Helper.ShowError($"{e.ToString()}");

            Console.WriteLine(e);
        }
        return new List<ModelOutputItem>();
    }

    // Only checks if a filter has been applied at all, not whether a directory has been scanned.
    private static bool IsFilteredDir(string dir)
    {
        foreach (string file in Directory.EnumerateFiles(dir))
        {
            if (file.EndsWith(".filtered"))
            {
                return true;
            }
        }
        return false;
    }

    private static void FilterFiles(List<ModelOutputItem> unfilteredPaths)
    {
        foreach (ModelOutputItem item in unfilteredPaths)
        {
            if (item.Prediction)
                try
                {
                    File.Move(item.Path, $"{item.Path}.filtered");
                    File.Copy($"{Helper.projectRoot}\\Dependencies\\black\\black{Path.GetExtension(item.Path)}", item.Path);
                    Console.WriteLine($"probability: {item.Probability:F2} for {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(item.Path))}: {item.Name}");
                }
                catch (IOException)
                {
                    Console.WriteLine($"File already filtered: {item.Name}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Helper.ShowError(e.ToString());
                    Console.WriteLine($"no access. Error: {e}");
                }
        }
    }
}