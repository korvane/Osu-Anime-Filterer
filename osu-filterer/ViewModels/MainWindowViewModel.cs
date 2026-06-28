using System;
using System.Collections.Generic;
using System.IO;

namespace osu_filterer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    public void filterImages(String path)
    {
        try
        {
            String[] osu = Directory.GetDirectories(path);
            foreach(String tempSongDir in osu)
            {
                String songDir = path + tempSongDir;
                String[] songFiles = Directory.GetDirectories(songDir);
                foreach(String file in tempDir)
            }
        }
        catch (Exception e)
        {
            
        }
    }

    public void judge()
    {
        
    }
}
