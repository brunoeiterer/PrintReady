using System;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PrintReady.ViewModels;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Diagnostics;
using PrintReady.Services;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintReady;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static readonly IServiceProvider Services;
    public Window? Window { get; private set; }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    static App()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PrintReadyViewModel>();

        Services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        Window.Activate();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (Window is MainWindow mainWindow)
        {
            mainWindow.DisplayErrorDialog();
        }
    }
}
