using FFmpeg.NET;
using PrivateCloud.Server.Common;
using SharpDevLib;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace PrivateCloud.Server.Services;

public class FfmpegService(ILogger<FfmpegService> logger, IConfiguration configuration)
{
    private Engine _engine;
    private const int _thumbHeight = 300;
    private const int _thumbWidth = 600;

    public async Task ConvertVideoToHlsAsync(Guid id, string inputFile, string hlsFilePath, string partFilePath, string partFileBaseUrl, string keyInfoPath)
    {
        Check(id);
        new FileInfo(partFilePath).Directory.Create();

        var args = $" -i \"{inputFile}\" -start_number 0 -hls_list_size 0 -f hls -hls_time 20 -hls_base_url \"{partFileBaseUrl}\" -hls_segment_filename \"{partFilePath}\" \"{hlsFilePath}\"";
        if (keyInfoPath.NotEmpty()) args = $" -i \"{inputFile}\" -start_number 0 -hls_list_size 0 -f hls -hls_time 20 -hls_key_info_file \"{keyInfoPath}\" -hls_base_url \"{partFileBaseUrl}\" -hls_segment_filename \"{partFilePath}\" \"{hlsFilePath}\"";
        await _engine.ExecuteAsync(args, Statics.AppCancellationTokenSource.Token);
    }

    public async Task GetGifThumbAsync(Guid id, string inputFile)
    {
        Check(id);
        var input = new InputFile(inputFile);
        if (!input.FileInfo.Exists) throw new Exception($"file not exist:'{inputFile}'");
        var outputTempPath = Statics.TempPath.CombinePath($"{id}/{id}.tmp.png");
        var outputPath = Statics.TempPath.CombinePath($"{id}/{id}.png");
        if (File.Exists(outputTempPath)) File.Delete(outputTempPath);
        if (File.Exists(outputPath)) File.Delete(outputPath);

        await _engine.GetThumbnailAsync(input, new OutputFile(outputTempPath), new ConversionOptions(), Statics.AppCancellationTokenSource.Token);
        await ResizeImage(outputTempPath, false, outputPath);
        if (File.Exists(outputTempPath)) File.Delete(outputTempPath);
    }

    public async Task GetImageThumbAsync(Guid id, string inputFile)
    {
        Check(id);
        var outputPath = Statics.TempPath.CombinePath($"{id}/{id}.png");
        await ResizeImage(inputFile, false, outputPath);
    }

    public async Task GetVideoThumbAsync(Guid id, string inputFile)
    {
        Check(id);
        var thumbPath = Statics.TempPath.CombinePath($"{id}/{id}.png");
        var gridPath = Statics.TempPath.CombinePath($"{id}/{id}.grid.png");
        var gifPath = Statics.TempPath.CombinePath($"{id}/{id}.gif");
        var folder = Statics.TempPath.CombinePath($"{id}/temp");
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        if (File.Exists(thumbPath)) File.Delete(thumbPath);
        if (File.Exists(gifPath)) File.Delete(gifPath);
        folder.EnsureDirectoryExist();
        var gridImageColumn = configuration.GetValue<int?>("GridImageColumn") ?? 2;

        //get thumb list
        var input = new InputFile(inputFile);
        var mediaInfo = await _engine.GetMetaDataAsync(input, Statics.AppCancellationTokenSource.Token);
        var totalSeconds = mediaInfo.Duration.TotalSeconds;
        var count = 21;
        var unit = totalSeconds / count;
        var gridImages = new List<GridImageInfo>();
        for (int i = 1; i < count; i++)
        {
            var seek = unit * i;
            var tempPath = folder.CombinePath($"{i}.temp.png");
            var path = folder.CombinePath($"{i}.png");
            var padPath = folder.CombinePath($"{i}.pad.png");
            var output = new OutputFile(tempPath);
            var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(seek) };
            await _engine.GetThumbnailAsync(input, output, options, Statics.AppCancellationTokenSource.Token);
            await ResizeImage(tempPath, true, padPath);
            gridImages.Add(new GridImageInfo { FileInfo = new FileInfo(padPath), Text = TimeSpan.FromSeconds(seek).ToString(@"hh\:mm\:ss") });
            await ResizeImage(tempPath, false, path);
        }
        new FileInfo(folder.CombinePath("10.png")).CopyTo(thumbPath);

        //grid image
        CombineToGridImage(gridImages, gridPath, _thumbWidth, _thumbHeight, gridImageColumn);

        //gif
        var gifTempPath = folder.CombinePath("%d.png");
        var args = $"-f image2 -framerate 2 -y -i \"{gifTempPath}\" \"{gifPath}\"";
        await _engine.ExecuteAsync(args, Statics.AppCancellationTokenSource.Token);

        //clean
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    private async Task ResizeImage(string path, bool pad, string targetPath)
    {
        try
        {
            using var image = Image.Load(path);
            if (pad) image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(_thumbWidth, _thumbHeight), Mode = ResizeMode.BoxPad }));
            else image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(0, _thumbHeight) }));
            await image.SaveAsPngAsync(targetPath);
        }
        catch (Exception ex)
        {
            logger.LogError("resize image failed,path='{path}',target='{targetPath}',error:{message}", path, targetPath, ex.Message);
        }
    }

    private static void CombineToGridImage(List<GridImageInfo> files, string savePath, int width, int height, int columns)
    {
        var collection = new FontCollection();
        var family = collection.Add(AppDomain.CurrentDomain.BaseDirectory.CombinePath("Fonts/OpenSans-Regular.ttf"));
        if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory.CombinePath("Fonts/OpenSans-Regular.ttf"))) throw new Exception("aaaaa");
        var font = family.CreateFont(12, FontStyle.Italic);
        var rows = (int)Math.Ceiling(files.Count * 1.0 / columns);
        using var container = new Image<Rgba32>(width * columns, height * rows, Color.White);
        var count = 0;
        files.ForEach(file =>
        {
            var xIndex = count % columns;
            var yIndex = count / columns;
            using var current = Image.Load(file.FileInfo.FullName);
            container.Mutate(x =>
            {
                x.DrawImage(current, new Point(xIndex * width, yIndex * height), 1);
                x.DrawText(file.Text, font, Color.White, new PointF(xIndex * width + 10, yIndex * height + 10));
            });
            count++;
        });
        container.Save(savePath);
    }

    private void Check(Guid id)
    {
        var path = configuration.GetValue<string>("FfmpegBinaryPath");
        if (string.IsNullOrWhiteSpace(path)) throw new Exception("FfmpegBinaryPath config required");
        var fullPath = Statics.FfmpegPath.CombinePath(path);
        if (!File.Exists(fullPath)) throw new Exception($"ffmpeg binary not exist '{fullPath}'");
        if (_engine is null)
        {
            _engine = new Engine(fullPath);
            _engine.Error += OnProcessError;
        }

        var tempPath = Statics.TempPath.CombinePath(id.ToString());
        if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
    }

    private void OnProcessError(object sender, FFmpeg.NET.Events.ConversionErrorEventArgs e)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(e.Input?.Name)) builder.Append($"input:{e.Input.Name};");
        if (!string.IsNullOrWhiteSpace(e.Output?.Name)) builder.Append($"output:{e.Output.Name};");
        builder.AppendLine($"exitCode:{e.Exception?.ExitCode};");
        builder.Append($"message:{e.Exception?.InnerException?.Message ?? e.Exception?.Message};");
        throw new Exception(builder.ToString());
    }

    class GridImageInfo
    {
        public FileInfo FileInfo { get; set; }
        public string Text { get; set; }
    }
}