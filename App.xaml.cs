using Microsoft.UI.Xaml;
using System;

namespace HandwrittenNotes;

public partial class App : Application
{
    private Window? _window;
    public static MainWindow? MainWindowInstance;

    public App()
    {
        this.UnhandledException += App_UnhandledException;
        this.InitializeComponent();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.IO.File.WriteAllText("crash.log", $"Exception: {e.Exception}\nMessage: {e.Message}");
        e.Handled = true;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindowInstance = (MainWindow)_window;
        _window.Activate();
    }
}
