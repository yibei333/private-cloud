using NLog;

namespace PrivateCloud.Maui;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider => Current?.Handler?.MauiContext?.Services ?? throw new NullReferenceException();

    public static Logger Logger => LogManager.GetCurrentClassLogger();

    public static bool IsWebHandleBack { get;set; }

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            Logger.Error(error);
        };

        InitializeComponent();
        MainPage = new MainPage();
    }
}