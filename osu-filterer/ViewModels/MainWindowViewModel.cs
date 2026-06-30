using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using System.Diagnostics;
using System.Text.Json;

namespace osu_filterer.ViewModels;

public class ModelOutputItem
{
    public string Path { get; set; }
    public bool Prediction { get; set; }
    public double Probability { get; set; }
}
public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly String projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    public string Greeting { get; } = "Welcome to Avalonia!";

    public static async void FilterImages(String path)
    {
        path = Path.Join(path, "Songs");
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
        List<String> imagePaths = new List<string>();
        try
        {
            Console.WriteLine(projectRoot);
            foreach (String dir in Directory.EnumerateDirectories(path))
            {
                if (IsFilteredDir(dir))
                {
                    Console.WriteLine($"Beatmap is already filtered: {dir}");
                    continue;
                }
                foreach (String file in Directory.EnumerateFiles(dir))
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
            var dialog = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                Content = new TextBlock { Text = "Idk.", Margin = new Avalonia.Thickness(20) }
            };

            await dialog.ShowDialog(dialog);
        }

        List<ModelOutputItem> filteredImagePaths = Judge(imagePaths);
        foreach (ModelOutputItem item in filteredImagePaths)
        {
            if (item.Prediction)
                try
                {
                    File.Move(item.Path, $"{item.Path}.filtered");
                    File.Copy($"{projectRoot}\\black\\black{Path.GetExtension(item.Path)}", item.Path);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"File already filtered: {item.Path}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"no access.");
                }
        }
    }

    public static void UndoFilter(String path)
    {
        path = Path.Join(path, "Songs");
        if (!Path.Exists(path))
        {
            throw new Exception("Choose a valid path.");
        }
        try
        {
            Console.WriteLine(projectRoot);
            foreach (String dir in Directory.EnumerateDirectories(path))
            {
                foreach (String file in Directory.EnumerateFiles(dir))
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
            Console.WriteLine($"error EEEEEEE: {e}");
        }
    }

    private static List<ModelOutputItem> Judge(List<String> files)
    {
        var payload = new { images = files };
        String json = JsonSerializer.Serialize(payload);
        String python = $"{projectRoot}\\.venv\\Scripts\\python.exe";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = python,
            Arguments = $"{projectRoot}\\new_runs.py",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(psi);
        process.StandardInput.WriteLine(json);
        process.StandardInput.Close();
        String output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Console.WriteLine("done");
        Console.WriteLine(output);
        List<ModelOutputItem> modelOutput = JsonSerializer.Deserialize<List<ModelOutputItem>>(output);
        return modelOutput;
    }

    private static bool IsFilteredDir(String dir)
    {
        foreach (String file in Directory.EnumerateFiles(dir))
        {
            if (file.EndsWith(".filtered"))
            {
                return true;
            }
        }
        return false;
    }
}
