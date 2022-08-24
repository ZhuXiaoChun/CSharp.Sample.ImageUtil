using BaoXia.Constants;
using BaoXia.Utils.Extensions;
using CSharp.Sample.ImageUtil.ConfigFiles;
using CSharp.Sample.ImageUtil.Constants;
using CSharp.Sample.ImageUtil.Data;
using CSharp.Sample.ImageUtil.Filters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var builderConfiguration = builder.Configuration;
var builderServices = builder.Services;

////////////////////////////////////////////////
// ����EF����
////////////////////////////////////////////////
{
        var sqlConnectionString
                = builderConfiguration.GetConnectionString("FileInfoDbContext");
        if (sqlConnectionString?.Length > 0)
        {
                builderServices.AddDbContext<FileInfoDbContext>(
                                        options =>
                                        {
                                                options.UseSqlServer(sqlConnectionString);
                                        });
        }
}

// Add services to the container.
builder.Services
        .Configure<FormOptions>(options =>
        {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = long.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
        })
        .AddControllersWithViews(
        (config) =>
        {
                config.Filters.Add(new AsyncAuthorizationFilter(
                        AsyncAuthorizationFilter.AuthorizationRequiredMode.Required,
                        new AsyncAuthorizationFilter.UnauthorizedResponse(
                                System.Net.HttpStatusCode.Unauthorized,
                                "text/plain",
                                StringExtension.StringByJsonSerialize(
                                        new CSharp.Sample.ImageUtil.ViewModels.Response()
                                        {
                                                Error = Error.NeedAccess
                                        }))));
        });

builder.WebHost.ConfigureKestrel(serverOptions =>
{
        serverOptions.Limits.MaxRequestBodySize = int.MaxValue;
});

var app = builder.Build();

////////////////////////////////////////////////
BaoXia.Utils.Environment.InitializeWithServerNameAtStartup(
        "C#.SampleCode.ImageUtil",
        app,
        app.Environment.WebRootPath,
        Passwords.AESKey,
        EnvironmentParams.ConfigFilesDirectoryName,
        EnvironmentParams.LogFilesDirectoryName);

////////////////////////////////////////////////

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Use(async (context, next) =>
{
        var request = context.Request;
        var requestContentLength = request.Headers.ContentLength;
        if (requestContentLength > Config.Service.FileUploadSizeInBytesMax)
        {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.RequestEntityTooLarge;

                await context.Response.CompleteAsync();
        }
        else
        {
                await next.Invoke();
        }
});

app.Run();