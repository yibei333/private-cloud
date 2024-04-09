using CommunityToolkit.Maui.Alerts;
using Microsoft.JSInterop;
using SharpDevLib;
using SharpDevLib.Extensions.Model;
using System.Text;

#if WINDOWS
using Microsoft.UI.Windowing;
using System.Diagnostics;
#endif

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
    public static async Task<ApplicationInfo> GetAppInfoAsync()
    {
        var version = string.Empty;
        using var stream = await FileSystem.Current.OpenAppPackageFileAsync("wwwroot/version.txt");
        if (stream is not null)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            version = Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        return new() { Version = version, Platform = DeviceInfo.Current.Platform.Convert() };
    }

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
    public static async void Upgrade(string path)
    {
        try
        {

#if WINDOWS
            Process.Start(new ProcessStartInfo(path));
#elif ANDROID
            var context = Android.App.Application.Context;
            if (context is null || context.ApplicationContext is null) throw new Exception("unable to find android context");
            var file = new Java.IO.File(path);

            using var install = new Android.Content.Intent(Android.Content.Intent.ActionView);
            var apkURI = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.ApplicationContext.PackageName + ".provider", file);
            install.SetDataAndType(apkURI, "application/vnd.android.package-archive");
            install.AddFlags(Android.Content.ActivityFlags.NewTask);
            install.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
            install.AddFlags(Android.Content.ActivityFlags.ClearTop);
            install.PutExtra(Android.Content.Intent.ExtraNotUnknownSource, true);
            Platform.CurrentActivity?.StartActivity(install);
#endif
            await Toast.Make($"已尝试自动升级").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"启动安装失败:{ex.Message}").Show();
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