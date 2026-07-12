using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using System.IO;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using Avalonia.Threading;
using osu_filterer.Views;
using Avalonia.Controls.Documents;


namespace osu_filterer.Dependencies;
public static class Helper
{
    // Run this if you are building manually. This should point to .\osu-filterer\osu-filterer. if you arent building with avalonia then adjust accordingly.:
    // public static readonly string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    public static string projectRoot =
    new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name == "Debug"
        ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
        : AppContext.BaseDirectory;
    private static IProgress<string>? consoleProgress;

    public static void SetProgress(IProgress<string> progress)
    {
        consoleProgress = progress;
    }

    public static void LogMessage(string message)
    {
        consoleProgress?.Report(message);
    }

    public static async Task DownloadPython(bool completed)
    {
        if (completed)
        {
            LogMessage("Python dependencies already installed\n");
            return;
        }
        string dependencies = Path.Combine(Helper.projectRoot, "Dependencies");
        string pythonDir = Path.Combine(dependencies, "python");
        string pythonexe = Path.Combine(pythonDir, "python.exe");
        string getPip = Path.Combine(pythonDir, "get-pip.py");
        string requirements = Path.Combine(dependencies, "requirements.txt");
        string install = Path.Combine(pythonDir, "CompletedInstall.txt");

        
        try
        {
            //install embeddable python
            LogMessage("\nPulling new python env...\n");
            string zipPath = Path.Combine(Helper.projectRoot, "python.zip");
            using (HttpClient client = new())
            {
                await using var stream = await client.GetStreamAsync("https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip");
                await using var file = File.Create(zipPath);
                await stream.CopyToAsync(file);
            }
            LogMessage("\nPull complete.\n");
            
            //extract 
            LogMessage("\nExtracting python env...\n");
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
            LogMessage("\nExtract complete.\n");

            //download get-pip
            LogMessage("\nPulling get-pip...\n");
            using (HttpClient client = new())
            {
                await using var stream = await client.GetStreamAsync("https://bootstrap.pypa.io/get-pip.py");
                await using var file = File.Create(getPip);
                await stream.CopyToAsync(file);
            }
            LogMessage("\nPull complete.\n");

            //uncomment python313._pth import for installing pip
            LogMessage("\nUncommenting python313._pth import for installing pip...\n");
            string pthFile = Directory.GetFiles(pythonDir, "python*._pth").Single();
            string contents = File.ReadAllText(pthFile);
            contents = contents.Replace("#import site", "import site");
            File.WriteAllText(pthFile, contents);
            LogMessage("\nOperation complete.\n");

        }
        catch(Exception e)
        {
            LogMessage(e.Message);
        }
    
        //install pip
        LogMessage("\nInstalling pip...\n");
        var installPip = new ProcessStartInfo
        {
            FileName = pythonexe,
            Arguments = getPip,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(installPip)?? throw new Exception("Pip Install failed");
        process.OutputDataReceived += (_, e) =>
        {
            if(e.Data != null)
            {
                LogMessage(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if(e.Data != null)
            {
                LogMessage(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            LogMessage("\nRequirements install failed\n");
            return;
        }
            

        //install requirements
        LogMessage("\nBeginning Python Dependencies install...\n");
        var installRequirements = new ProcessStartInfo
        {
            FileName = pythonexe,
            Arguments = $"-m pip install -r {requirements}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process = Process.Start(installRequirements)?? throw new Exception("Dependencies Install failed.");
        process.OutputDataReceived += (_, e) =>
        {
            if(e.Data != null)
            {
                LogMessage(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if(e.Data != null)
            {
                LogMessage(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            LogMessage($"\nDependencies Install failed. Make sure you set the correct project root.\nCurrent: {projectRoot}\nIs this the location of your unzipped folder?\nyou should review and change the code if necessary(Dependencies\\Helper.cs line 23)");
            return;
        }
        LogMessage("Finished Python Dependencies install.");
        LogMessage("Setup complete!");
        File.WriteAllText(install, "");
    }
}