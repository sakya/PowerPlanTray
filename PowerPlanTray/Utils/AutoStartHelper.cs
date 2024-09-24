using System;
using System.IO;
using System.Net;
using Securify.ShellLink;

namespace PowerPlanTray.Utils;

public static class AutoStartHelper
{
    public static bool IsEnabled()
    {
        var autoStart = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "PowerPlanTray.lnk");
        return File.Exists(autoStart);
    }

    public static bool SetEnabled(bool enabled)
    {
        var autoStart = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "PowerPlanTray.lnk");
        if (!enabled && File.Exists(autoStart)) {
            File.Delete(autoStart);
        } else if (enabled) {
            var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(exePath))
                return false;
            if (exePath.EndsWith(".dll")) {
                exePath = $"{exePath.Remove(exePath.Length - 4, 4)}.exe";
            }
            var exeDir = Path.GetDirectoryName(exePath);
            Shortcut.CreateShortcut(exePath, null, exeDir, exePath, 0)
                .WriteToFile(autoStart);
        }

        return true;
    }

}