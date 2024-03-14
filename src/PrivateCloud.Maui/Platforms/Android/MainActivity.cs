using Android.App;
using Android.Content.PM;
using Android.OS;

namespace PrivateCloud.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            //如需管理所有文件，需要在AndroidManifest.xml中加上<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE"/>，await Permissions.RequestAsync<Permissions.StorageWrite>()应该不是不需的
            //            try
            //            {
            //#pragma warning disable CA1416 // 验证平台兼容性
            //                if (!Android.OS.Environment.IsExternalStorageManager)
            //                {
            //                    Intent intent = new();
            //                    var action = Android.Provider.Settings.ActionManageAllFilesAccessPermission;
            //                    intent.SetAction(action);
            //                    var uri = Android.Net.Uri.FromParts("package", PackageName, null);
            //                    intent.SetData(uri);
            //                    StartActivity(intent);
            //                }
            //#pragma warning restore CA1416 // 验证平台兼容性
            //            }
            //            catch
            //            {
            //            }
            base.OnCreate(savedInstanceState);
        }
    }
}
