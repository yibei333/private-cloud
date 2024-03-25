using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PrivateCloud.Maui.Extensions;

#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace PrivateCloud.Maui;

public static class MauiProgram
{
#if WINDOWS
    public static Microsoft.UI.Xaml.Window? MainWindow { get; private set; }
#endif

    public static MauiApp CreateMauiApp()
    {
        //handle mixed content error,[https://niek.github.io/chrome-features/]
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--allow-running-insecure-content");

        var builder = MauiApp.CreateBuilder();

        builder
            .UseLogging()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            .ConfigureMauiHandlers(x =>
            {
#if ANDROID
                x.AddHandler(typeof(Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView), typeof(PrivateCloud.Maui.Platforms.Android.AndroidCustomBlazorWebViewHandler));
#endif
            });
#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windowsLifecycleBuilder =>
            {
                windowsLifecycleBuilder.OnWindowCreated(window => MainWindow = window);
            });
        });
#endif
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
