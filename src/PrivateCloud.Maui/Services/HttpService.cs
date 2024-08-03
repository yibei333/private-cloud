using CommunityToolkit.Maui.Storage;
using Microsoft.JSInterop;
using SharpDevLib;
using SharpDevLib.Transport;
using System.Diagnostics;

namespace PrivateCloud.Maui.Services;

public static class HttpService
{
    [JSInvokable]
    public static async Task<HttpResponse<string>> UploadFolder(RequestOptions request)
    {
        var pickResult = await FolderPicker.Default.PickAsync();
        if (!pickResult.IsSuccessful) return HttpResponse<string>.Succeed(request.Url, null, "cancel");

        var directory = new DirectoryInfo(pickResult.Folder.Path) ?? throw new NullReferenceException();
        var rootPath = directory.Parent?.FullName ?? string.Empty;
        var files = GetFolderFiles(rootPath, directory);
        if (files.IsNullOrEmpty()) return HttpResponse<string>.Failed(request.Url, System.Net.HttpStatusCode.InternalServerError, "不支持上传空文件夹");

        request.Method = "post";
        var postRequest = new HttpMultiPartFormDataRequest(request.Url, files.ToArray()).SetRequest(request);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().PostAsync<string>(postRequest);
    }

    private static List<HttpFormFile> GetFolderFiles(string rootPath, DirectoryInfo directory)
    {
        var childDirectories = directory.GetDirectories();
        var childDirecotryFiles = childDirectories.SelectMany(x => GetFolderFiles(rootPath, x));
        var childFiles = directory.GetFiles().Select(x => new HttpFormFile("Files", rootPath.IsNullOrWhiteSpace() ? x.FullName : x.FullName.Replace(rootPath, ""), File.OpenRead(x.FullName)));
        return childDirecotryFiles.Union(childFiles).ToList();
    }

    [JSInvokable]
    public static async Task<HttpResponse<string>> UploadFiles(RequestOptions request)
    {
        var pickResult = await FilePicker.Default.PickMultipleAsync();
        if (!pickResult.Any()) return HttpResponse<string>.Succeed("cancel", null);

        var files = pickResult.Select(x => new HttpFormFile("Files", x.FileName, File.OpenRead(x.FullPath))).ToList();
        if (files.IsNullOrEmpty()) return HttpResponse<string>.Failed(request.Url, System.Net.HttpStatusCode.InternalServerError, "没有需要上传的文件");

        request.Method = "post";
        var postRequest = new HttpMultiPartFormDataRequest(request.Url, files.ToArray()).SetRequest(request);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().PostAsync<string>(postRequest);
    }

    [JSInvokable]
    public static async void OpenBrowser(string url)
    {
        try
        {
            await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"open browser failed:{ex.Message}");
        }
    }

    [JSInvokable]
    public static async Task<HttpResponse<string>> Download(RequestOptions options)
    {
        try
        {
            var requestOptions = new HttpKeyValueRequest(options.Url).SetRequest(options);
            var stream = await App.ServiceProvider.GetRequiredService<IHttpService>().GetStreamAsync(requestOptions);
            if (DeviceInfo.Current.Platform == DevicePlatform.Android) return await AndroidDownload(options, stream);

            var result = await FileSaver.Default.SaveAsync(options.Name, stream, CancellationToken.None);
            result.EnsureSuccess();
            stream.Close();
            if (result.IsSuccessful)
            {
                return HttpResponse<string>.Succeed(options.Url, null, $"已保存在位置:{result.FilePath}");
            }
            else
            {
                return HttpResponse<string>.Failed(options.Url, System.Net.HttpStatusCode.InternalServerError, result.Exception?.Message ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            return HttpResponse<string>.Failed(options.Url, System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    static async Task<HttpResponse<string>> AndroidDownload(RequestOptions options, Stream stream)
    {
        try
        {
            var path = string.Empty;
#if ANDROID
            path = Android.OS.Environment.ExternalStorageDirectory?.Path.CombinePath($"Download/{options.Name}") ?? throw new Exception("找不到外部存储目录");
#endif
            var fileInfo = new FileInfo(path);
            if (fileInfo.Directory is null || !fileInfo.Directory.Exists) Directory.CreateDirectory(fileInfo.Directory!.FullName);
            if (File.Exists(path)) File.Delete(path);
            using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            return HttpResponse<string>.Succeed(options.Url, null, $"已保存在位置:{path}");
        }
        catch (Exception ex)
        {
            return HttpResponse<string>.Failed(options.Url, System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [JSInvokable]
    public static async Task<HttpResponse<string>> HttpRequest(RequestOptions options)
    {
        if (options.Method == "get") return await Get(options);
        else if (options.Method == "post") return await Post(options);
        else if (options.Method == "put") return await Put(options);
        else if (options.Method == "delete") return await Delete(options);
        else return HttpResponse<string>.Failed(options.Url, System.Net.HttpStatusCode.InternalServerError, $"request '{options.Method}' not supported");
    }

    [JSInvokable]
    public static async Task<HttpResponse<byte[]>> GetBlob(RequestOptions options)
    {
        var requestOptions = new HttpKeyValueRequest(options.Url).SetRequest(options);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().GetAsync<byte[]>(requestOptions);
    }

    static async Task<HttpResponse<string>> Get(RequestOptions options)
    {
        var requestOptions = new HttpKeyValueRequest(options.Url).SetRequest(options);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().GetAsync<string>(requestOptions);
    }

    static async Task<HttpResponse<string>> Post(RequestOptions options)
    {
        var requestOptions = new HttpJsonRequest(options.Url, options.Data).SetRequest(options);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().PostAsync<string>(requestOptions);
    }

    static async Task<HttpResponse<string>> Put(RequestOptions options)
    {
        var requestOptions = new HttpJsonRequest(options.Url, options.Data).SetRequest(options);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().PutAsync<string>(requestOptions);
    }

    static async Task<HttpResponse<string>> Delete(RequestOptions options)
    {
        var requestOptions = new HttpKeyValueRequest(options.Url).SetRequest(options);
        return await App.ServiceProvider.GetRequiredService<IHttpService>().DeleteAsync<string>(requestOptions);
    }

    static TRequest SetRequest<TRequest>(this TRequest request, RequestOptions options) where TRequest : HttpRequest
    {
        request.Headers = options.Headers.ToDictionary(x => x.Key, x => new string[] { x.Value });
        if (options.OptionInstance is not null)
        {
            if (options.Method == "post")
            {
                request.OnSendProgress = async (p) =>
                {
                    await options.OptionInstance.InvokeVoidAsync("progress", p.Progress);
                };
            }
            else
            {
                request.OnReceiveProgress = async (p) =>
                {
                    await options.OptionInstance.InvokeVoidAsync("progress", p.Progress);
                };
            }
        }
        if (options.Timeout.HasValue && options.Timeout > 0) request.TimeOut = TimeSpan.FromSeconds(options.Timeout.Value);
        return request;
    }
}

public class RequestOptions
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public List<KeyValueDto> Headers { get; set; } = [];
    public IJSObjectReference? OptionInstance { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Timeout { get; set; }
}

public class KeyValueDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}