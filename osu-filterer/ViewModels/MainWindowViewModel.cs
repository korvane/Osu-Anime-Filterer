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
using System.Threading.Tasks;

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
    public static async Task HandleFilter(string? path)
    {
        path = Path.Join(path, "Songs");
        if (!Path.Exists(path))
        {
            Helper.LogMessage("Choose a valid path.");
        }
        List<string> imagePaths = new List<string>();
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                if (IsFilteredDir(dir))
                {
                    continue;
                }
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                    {
                        imagePaths.Add(file);
                    }
                }
                File.WriteAllText(Path.Join(dir, ".filtered"), "");
            }
        }
        catch (Exception e)
        {
            Helper.LogMessage(e.ToString());
            Helper.LogMessage($"Error: {e}");
        }
        if(imagePaths.Count == 0)
        {
            Helper.LogMessage("beatmaps are filtered!! :P");
            return;
        }
        Helper.LogMessage($"Start Model: {path}\n");
        List<ModelOutputItem> unfilteredPaths = await RunModel(imagePaths);
        Helper.LogMessage($"\nFilter and Replace: {path}\n");
        FilterFiles(unfilteredPaths);
        Helper.LogMessage($"\nreplacement done :P");
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
            Helper.LogMessage($"Start unfilter at {path}\n");
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    if (!file.EndsWith("\\.filtered") && file.EndsWith(".filtered"))
                    {
                        File.Delete(file.Substring(0, file.IndexOf(".filtered")));
                        File.Move(file, file.Substring(0, file.IndexOf(".filtered")));
                    }
                }
                File.Delete(Path.Join(dir, ".filtered"));
            }
        }
        catch (Exception e)
        {
            Helper.LogMessage(e.ToString());
            Helper.LogMessage($"error: {e}");
        }
        Helper.LogMessage($"Done with unfilter at {path}");
    }

    private static async Task<List<ModelOutputItem>> RunModel(List<string> files)
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
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(psi) ?? throw new Exception("ProcessStartInfo cannot be null.");
            process.StandardInput.WriteLine(json);
            process.StandardInput.Close();
            process.ErrorDataReceived += (_, e) =>
            {
                if(e.Data != null)
                {
                    Helper.LogMessage(e.Data);
                }
            };
            process.BeginErrorReadLine();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            Helper.LogMessage("END MODEL");
            List<ModelOutputItem> modelOutput = JsonSerializer.Deserialize<List<ModelOutputItem>>(output) ?? throw new Exception("output returned null.");
            return modelOutput;
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            Helper.LogMessage($"This app cannot run as a standalone!\nProgram must be ran from its original folder.\n\nRemember to change the rootDirectory string if necessary.\n\ncurrent directory: {Helper.projectRoot}\n\n python directory:{python}\n\n{e.ToString()}");
            Helper.LogMessage(e.Message);
        }
        catch (Exception e)
        {
            Helper.LogMessage($"{e.Message}");
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
                    Helper.LogMessage($"probability: {item.Probability:F2} for {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(item.Path))}: {item.Name}");
                }
                catch (IOException)
                {
                    Helper.LogMessage($"File already filtered: {item.Name}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Helper.LogMessage(e.Message);
                    Helper.LogMessage($"no access. Error: {e}");
                }
        }
    }
}