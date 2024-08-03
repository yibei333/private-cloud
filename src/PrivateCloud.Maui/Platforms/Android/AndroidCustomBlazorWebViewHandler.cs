using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Microsoft.AspNetCore.Components.WebView.Maui;
using BuildVersionCodes = Android.OS.BuildVersionCodes;
using Color = Android.Graphics.Color;
using PM = Android.Content.PM;
using View = Android.Views.View;

namespace PrivateCloud.Maui.Platforms.Android;

partial class AndroidCustomBlazorWebViewHandler : BlazorWebViewHandler
{
    protected override global::Android.Webkit.WebView CreatePlatformView()
    {
        var webView = base.CreatePlatformView();
        webView.SetWebChromeClient(new AndroidCustomWebChromeClient());
        webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
        return webView;
    }
}

partial class AndroidCustomWebChromeClient : WebChromeClient
{
    private readonly Activity? context;
    private int originalUiOptions;
    private View? customView;
    private ICustomViewCallback? videoViewCallback;

    public AndroidCustomWebChromeClient()
    {
        this.context = Platform.CurrentActivity;
    }

    public override Bitmap? DefaultVideoPoster => Bitmap.CreateBitmap(1, 1, Bitmap.Config.Alpha8!);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422:验证平台兼容性", Justification = "<挂起>")]
    public override void OnHideCustomView()
    {
        if (context is null) return;

        if (context.Window?.DecorView is FrameLayout layout) layout.RemoveView(customView);
        if (!IsTablet(context)) context.RequestedOrientation = PM.ScreenOrientation.Portrait;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            context.Window?.SetDecorFitsSystemWindows(true);
            context.Window?.InsetsController?.Show(WindowInsets.Type.SystemBars());
        }
        else
        {
            if (context.Window?.DecorView is not null) context.Window.DecorView.SystemUiFlags = (SystemUiFlags)originalUiOptions;
        }

        videoViewCallback?.OnCustomViewHidden();
        customView = null;
        videoViewCallback = null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422:验证平台兼容性", Justification = "<挂起>")]
    public override void OnShowCustomView(View? view, ICustomViewCallback? callback)
    {
        if (customView != null)
        {
            OnHideCustomView();
            return;
        }

        if (context == null) return;

        videoViewCallback = callback;
        customView = view;
        customView?.SetBackgroundColor(Color.White);
        context.RequestedOrientation = PM.ScreenOrientation.Landscape;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            context.Window?.SetDecorFitsSystemWindows(false);
            context.Window?.InsetsController?.Hide(WindowInsets.Type.SystemBars());
        }
        else
        {
            if (context.Window is not null)
            {
                originalUiOptions = (int)context.Window.DecorView.SystemUiFlags;
                var newUiOptions = originalUiOptions | (int)SystemUiFlags.LayoutStable | (int)SystemUiFlags.LayoutHideNavigation | (int)SystemUiFlags.LayoutHideNavigation |
                                (int)SystemUiFlags.LayoutFullscreen | (int)SystemUiFlags.HideNavigation | (int)SystemUiFlags.Fullscreen | (int)SystemUiFlags.Immersive;
                context.Window.DecorView.SystemUiFlags = (SystemUiFlags)newUiOptions;
            }
        }

        if (context.Window?.DecorView is FrameLayout layout) layout.AddView(customView, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
    }

    private static bool IsTablet(Activity context)
    {
        return (context.Resources?.Configuration?.ScreenLayout & ScreenLayout.SizeMask) >= ScreenLayout.SizeLarge;
    }
}