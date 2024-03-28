using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using PrivateCloud.Server.Auth;
using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data;
using PrivateCloud.Server.Data.Migrations;
using PrivateCloud.Server.Filters;
using PrivateCloud.Server.Services;
using Serilog;
using Serilog.Enrichers.Span;
using SharpDevLib;
using SharpDevLib.Extensions.Data;
using SharpDevLib.Extensions.Encryption;
using SharpDevLib.Extensions.Http;
using SharpDevLib.Extensions.Jwt;
using System.Reflection;

Statics.Init();
var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
    WebRootPath = AppContext.BaseDirectory.CombinePath("wwwroot")
};

var builder = WebApplication.CreateBuilder(options);
builder.Configuration.AddJsonFile(AppDomain.CurrentDomain.BaseDirectory.CombinePath("appsettings.Other.json"), true, true);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<FailFilter>();
}).AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
});
builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = long.MaxValue;
    x.MemoryBufferThreshold = int.MaxValue;
});
builder.Host.UseSerilog((context, configration) =>
{
    configration.ReadFrom.Configuration(context.Configuration).Enrich.WithSpan();
    configration.WriteTo.File(Statics.LogPath);
});
builder.Host.UseWindowsService();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddData<DataContext, DbMigration>(option => option.UseSqlite(Statics.DbConnectionString));
builder.Services.AddTransient<FfmpegService>();
builder.Services.AddScoped<ThumbTaskService>();
builder.Services.AddScoped<CryptoTaskService>();
builder.Services.AddScoped<CleanTempService>();
builder.Services.AddEncryption();
builder.Services.AddHangfire(configuration =>
{
    configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();
    configuration.UseSQLiteStorage(Statics.DBFilePath);
    configuration.UseDashboardJavaScript(Assembly.GetExecutingAssembly(), $"{Assembly.GetExecutingAssembly().GetName().Name}.wwwroot.hangfire.js");
});

builder.Services.AddHangfireServer();
builder.Services.AddHttp();
builder.Services.AddJwt();
builder.Services.AddAuthentication(StaticNames.TokenSchemeName).AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(StaticNames.TokenSchemeName, null);
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationHandler, RoleAuthorizeHandler>();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
{
    Statics.AppCancellationTokenSource.Cancel();
});

app.UseCors(corsConfig =>
{
    var allowedOrigins = app.Configuration.GetValue<string>(StaticNames.CorsOriginsName)?.Split(";", StringSplitOptions.RemoveEmptyEntries).ToArray() ?? [];
    corsConfig.WithOrigins([.. allowedOrigins]);
    corsConfig.AllowAnyHeader();
    corsConfig.AllowAnyMethod();
    corsConfig.AllowCredentials();
});
app.MigrateData<DataContext>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard(Statics.HangfireRoute, new DashboardOptions
{
    Authorization = new[] { new HangfireAuthFilter() },
});
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.Context.Request.Path.ToString().IsStaticFiles())
        {
            context.Context.Response.Headers.ContentType += "; charset=utf-8";
            context.Context.Response.Headers.CacheControl = "max-age=31536000";
            context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        }
    }
});
app.MapControllers();
Statics.ServiceProvider = app.Services.CreateScope().ServiceProvider;
BackgroundJob.Schedule(() => Statics.ServiceProvider.GetRequiredService<ThumbTaskService>().ScanToProcessThumbTaskAsync(), TimeSpan.FromSeconds(5));
BackgroundJob.Schedule(() => Statics.ServiceProvider.GetRequiredService<CryptoTaskService>().ScanToProcessCryptoTask(), TimeSpan.FromSeconds(5));
await app.RunAsync();
