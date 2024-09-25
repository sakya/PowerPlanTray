using Avalonia;
using System;
using System.IO;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;

namespace PowerPlanTray;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Set current directory to the one containing the executable
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
        if (exeDir != null)
            Directory.SetCurrentDirectory(exeDir);

        BuildAvaloniaApp()
            .Start(AppMain, args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<MaterialDesignIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.Software] // For some reason uses less memory
            })
            .LogToTrace();
    }

    private static void AppMain(Application app, string[] args)
    {
        var mApp = ((App)app);
        mApp.Init();
        mApp.Run(mApp.CancellationTokenSource.Token);
    }
}