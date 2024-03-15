using Microsoft.JSInterop;
using Microsoft.UI.Windowing;
using SharpDevLib;
using SharpDevLib.Extensions.Model;

namespace PrivateCloud.Maui.Services;

public static class AppService
{
    [JSInvokable]
    public static void SetWebHandleBack(bool isHandle)
    {
        App.IsWebHandleBack = isHandle;
    }

    [JSInvokable]
    public static async void SetLoginPassword(KeyValueDto namePassword)
    {
        if (namePassword.Key.IsEmpty() || namePassword.Value.IsEmpty()) return;
        await SecureStorage.Default.SetAsync($"password_{namePassword.Key}", namePassword.Value);
    }

    [JSInvokable]
    public static async Task<string> GetLoginPassword(string name)
    {
        if (name.IsEmpty()) return string.Empty;
        return (await SecureStorage.Default.GetAsync($"password_{name}")) ?? string.Empty;
    }

    [JSInvokable]
    public static ApplicationInfo GetAppInfo() => new() { Version = AppInfo.Current.VersionString, Platform = DeviceInfo.Current.Platform.Convert() };

    [JSInvokable]
    public static async Task<Result> SetClipboard(string text)
    {
        try
        {
            await Clipboard.Default.SetTextAsync(text);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Failed(ex.Message);
        }
    }

    [JSInvokable]
    public static void FullScreen()
    {
#if WINDOWS
        if (MauiProgram.MainWindow is null) return;
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MauiProgram.MainWindow);
        Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var _appWindow = AppWindow.GetFromWindowId(myWndId);
        _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
#endif
    }

    [JSInvokable]
    public static void ExitFullScreen()
    {
#if WINDOWS
        if (MauiProgram.MainWindow is null) return;
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MauiProgram.MainWindow);
        Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var _appWindow = AppWindow.GetFromWindowId(myWndId);
        _appWindow.SetPresenter(AppWindowPresenterKind.Default);
#endif
    }
}

public class ApplicationInfo
{
    public string? Version { get; set; }
    public string? Platform { get; set; }
}

public static class PlatformsConvert
{
    public static string Convert(this DevicePlatform platform)
    {
        if (platform == DevicePlatform.Android) return Platforms.android.ToString();
        else if (platform == DevicePlatform.iOS) return Platforms.ios.ToString();
        else if (platform == DevicePlatform.macOS || platform == DevicePlatform.MacCatalyst) return Platforms.mac.ToString();
        else if (platform == DevicePlatform.Tizen) return Platforms.tizen.ToString();
        else if (platform == DevicePlatform.WinUI) return Platforms.windows.ToString();
        else return platform.ToString();
    }
}

public enum Platforms
{
    android = 1,
    ios,
    mac,
    tizen,
    windows,
}