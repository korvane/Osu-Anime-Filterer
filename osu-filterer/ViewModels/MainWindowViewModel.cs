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
using System.Dynamic;

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
    public static List<string> ImagePaths {get; set;} = new List<string>();
    public static void GatherImages(string? path)
    {
        if (!Path.Exists(path))
        {
            Helper.LogMessage("Choose a valid path.");
            return;
        }
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                if (IsFilteredDir(dir))
                {
                    continue;
                }
                GatherImages(dir);
            }
            foreach (string file in Directory.EnumerateFiles(path))
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                {
                    ImagePaths.Add(file);
                }
            }
            File.WriteAllText(Path.Join(path, ".filtered"), "");
        }
        catch (Exception e)
        {
            Helper.LogMessage($"Error:\n{e}");
        }
    }

    public static void HandleUnfilter(string path)
    {
        if (!Path.Exists(path))
        {
            Helper.LogMessage($"Invalid path: {path}");
            return;
        }
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                HandleUnfilter(dir);
            }
            foreach (string file in Directory.EnumerateFiles(path))
            {
                if (!file.EndsWith("\\.filtered") && file.EndsWith(".filtered"))
                {
                    File.Delete(file.Substring(0, file.IndexOf(".filtered")));
                    File.Move(file, file.Substring(0, file.IndexOf(".filtered")));
                }
            }
            File.Delete(Path.Join(path, ".filtered"));
        }
        catch (Exception e)
        {
            Helper.LogMessage(e.Message);
            Helper.LogMessage($"error: {e}");
        }
    }

    public static async Task<List<ModelOutputItem>> RunModel()
    {
        var payload = new { images = ImagePaths };
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
            Helper.LogMessage($"This app cannot run as a standalone!\nProgram must be ran from its original folder.\n\nRemember to change the rootDirectory string if necessary.\n\ncurrent directory: {Helper.projectRoot}\n\n python directory:{python}\n\n{e.Message}");
            Helper.LogMessage(e.Message);
        }
        catch (Exception e)
        {
            Helper.LogMessage($"{e.Message}");
        }
        return new List<ModelOutputItem>();
    }

    // Checks whether a directory has been scanned.
    private static bool IsFilteredDir(string dir)
    {
        return File.Exists(Path.Combine(dir, ".filtered"));
    }

    public static void FilterImages(List<ModelOutputItem> unfilteredPaths)
    {
        foreach (ModelOutputItem item in unfilteredPaths)
        {
            if (item.Prediction)
                try
                {
                    File.Move(item.Path, $"{item.Path}.filtered");
                    File.Copy($"{Helper.projectRoot}\\Dependencies\\black\\black{Path.GetExtension(item.Path)}", item.Path);
                    Helper.LogMessage($"probability: {item.Probability:F2} for {Path.GetFileName(Path.GetDirectoryName(item.Path))}: {item.Name}");
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
