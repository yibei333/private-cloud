namespace PrivateCloud.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        blazorWebView.HostPage = "wwwroot/index.html";
        blazorWebView.StartPath = "/index.html#/login";
    }
}
