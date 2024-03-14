using Android.Graphics;
using Android.Webkit;
using Microsoft.AspNetCore.Components.WebView.Maui;

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
    public override Bitmap? DefaultVideoPoster => Bitmap.CreateBitmap(1, 1, Bitmap.Config.Alpha8!);
}