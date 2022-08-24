using BaoXia.Constants;
using BaoXia.Utils.Extensions;
using CSharp.Sample.ImageUtil.ConfigFiles;
using CSharp.Sample.ImageUtil.Extensions;
using CSharp.Sample.ImageUtil.LogFiles;
using CSharp.Sample.ImageUtil.Models;
using CSharp.Sample.ImageUtil.Utils;
using CSharp.Sample.ImageUtil.ViewModels;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics.CodeAnalysis;
using BaoXia.Utils.ConcurrentTools;
using BaoXia.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;
using BaoXia.Utils.Cache;
using CSharp.Sample.ImageUtil.Constants;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using StringExtension = BaoXia.Utils.Extensions.StringExtension;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using BaoXia.Utils.MathTools;

namespace CSharp.Sample.ImageUtil.Controllers
{
        public class UploadController : Controller
        {

                ////////////////////////////////////////////////
                // @静态常量
                ////////////////////////////////////////////////

                #region 静态常量

                public class FontFamilyInfo
                {
                        public string FontFilePath { get; set; }

                        public SixLabors.Fonts.FontFamily FontFamily { get; set; }

                        public FontFamilyInfo(
                                string fontFilePath,
                                SixLabors.Fonts.FontFamily fontFamily)
                        {
                                this.FontFilePath = fontFilePath;
                                this.FontFamily = fontFamily;
                        }
                }

                #endregion


                ////////////////////////////////////////////////
                // @静态变量
                ////////////////////////////////////////////////

                #region 静态变量

                private static readonly List<Models.FileInfo> _fileInfoStorageQueue = new();

                private static readonly LoopTask _taskToStorageFileInfo = new(
                        async cancellationToken =>
                        {
                                Models.FileInfo[]? fileInfesNeedStorage = null;
                                lock (_fileInfoStorageQueue)
                                {
                                        if (_fileInfoStorageQueue.Count < 1)
                                        {
                                                return false;
                                        }
                                        fileInfesNeedStorage = _fileInfoStorageQueue.ToArray();
                                        _fileInfoStorageQueue.Clear();
                                }
                                if (fileInfesNeedStorage == null
                                || fileInfesNeedStorage.Length < 1)
                                {
                                        return false;
                                }

                                using var scope = BaoXia.Utils.Environment.ApplicationBuilder?.ApplicationServices.CreateScope();
                                var db = scope?.ServiceProvider.GetRequiredService<Data.FileInfoDbContext>();
                                if (db == null)
                                {
                                        Log.Warning.Logs(
                                                null,
                                                "无法在“持久化文件信息”的任务中，获取数据库上下文。",
                                                null,
                                                "UploadController");
                                        //
                                        return false;
                                }

                                foreach (var fileInfo in fileInfesNeedStorage)
                                {
                                        try
                                        {
                                                var fileInfoEntity = db.Entry(fileInfo);
                                                // !!!
                                                fileInfoEntity
                                                .Property(fileInfoModel => fileInfoModel.SecondsToDatabaseOperation)
                                                .IsModified = true;
                                                // !!!
                                                {
                                                        fileInfo.StorageTime = DateTime.Now;
                                                }
                                        }
                                        catch (Exception exception)
                                        {
                                                Log.Exception.Logs(
                                                        null,
                                                        "讲文件信息加入数据库上下文失败，程序异常。",
                                                        exception,
                                                        "UploadController");
                                        }
                                }
                                await db.SaveChangesAsync();

                                return true;
                        },
                        () =>
                        {
                                if (Config.Service.FileInfoStorageIntervalSeconds > 0)
                                {
                                        return Config.Service.FileInfoStorageIntervalSeconds;
                                }
                                return ServiceConfig.FileInfoStorageIntervalSecondsDefault;
                        });

                private static readonly AsyncItemsCache<string, Image, object> _imageCache = new(
                        async (watermarkImageAbsoluteFilePath, _) =>
                        {
                                if (string.IsNullOrEmpty(watermarkImageAbsoluteFilePath))
                                {
                                        return null;
                                }

                                var image
                                = await SixLabors.ImageSharp.Image.LoadAsync(
                                        watermarkImageAbsoluteFilePath);
                                { }
                                return image;
                        },
                        null,
                        null);


                private static readonly SixLabors.Fonts.FontCollection _fontCollection = new();
                private static readonly ItemsCache<string, FontFamilyInfo, object> _fontCache = new(
                         (fontFileName, _) =>
                        {
                                if (string.IsNullOrEmpty(fontFileName))
                                {
                                        return null;
                                }

                                var fontFilePath = fontFileName.ToAbsoluteFilePathInRootPath(
                                        FilePaths.FontsDictionaryPath);
                                var fontFamily = _fontCollection.Add(fontFilePath);
                                if (string.IsNullOrEmpty(fontFamily.Name))
                                {
                                        return null;
                                }

                                var fontFamilyInfo = new FontFamilyInfo(
                                        fontFilePath,
                                        fontFamily);
                                { }
                                return fontFamilyInfo; ;
                        },
                        null,
                        null);

                #endregion


                ////////////////////////////////////////////////
                // @类方法
                ////////////////////////////////////////////////

                #region 类方法

                public static void DidServiceConfigChanged(ServiceConfig serviceConfig)
                {
                        // !!!
                        _imageCache.Clear();
                        // !!!
                }

                private static void AddFileInfoToStorageQueue(Models.FileInfo fileInfo)
                {
                        if (fileInfo.Id == 0)
                        {
                                return;
                        }

                        lock (_fileInfoStorageQueue)
                        {
                                _fileInfoStorageQueue.Add(fileInfo);
                        }
                        _taskToStorageFileInfo.Start();
                }

                private static long SaveImageToFilePath(
                    string objectFileAbsolutePath,
                    Image image,
                    IImageEncoder imageEncoder,
                    int imageSaveQuality)
                {
                        if (image == null
                            || imageEncoder == null)
                        {
                                return 0;
                        }

                        if (imageEncoder
                            is SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder jpgEncoder)
                        {
                                jpgEncoder.Quality = imageSaveQuality;
                        }

                        image.Save(
                            objectFileAbsolutePath,
                            imageEncoder);

                        var imageFileInfo = new System.IO.FileInfo(objectFileAbsolutePath);
                        { }
                        return imageFileInfo.Length;
                }
                private static Image ProcessImageFileWithSourceImage(
                        Image sourceImage,
                        ImageSize? objectImageSize,
                        bool isWatermarkEnable,
                        ImageWatermarkInfo? watermarkInfo,
                        string? watermarkCaption)
                {
                        var isNeedResizeImage = false;
                        if (objectImageSize != null)
                        {
                                if (objectImageSize.Width != sourceImage.Width
                                        || objectImageSize.Height != sourceImage.Height)
                                {
                                        isNeedResizeImage = true;
                                }
                        }
                        var isNeedStampWatermark = false;
                        if (isWatermarkEnable == true
                                && watermarkInfo?.IsValid == true)
                        {
                                isNeedStampWatermark = true;
                        }

                        if (isNeedResizeImage == false
                                && isNeedStampWatermark == false)
                        {
                                return sourceImage;
                        }

                        var objectImage = sourceImage.Clone(
                                imageProcessingContext =>
                                {
                                        var objectImageWidth = sourceImage.Width;
                                        var objectImageHeight = sourceImage.Height;
                                        if (isNeedResizeImage
                                        && objectImageSize != null)
                                        {
                                                objectImageWidth = objectImageSize.Width;
                                                objectImageHeight = objectImageSize.Height;
                                                imageProcessingContext.Resize(new ResizeOptions()
                                                {
                                                        Mode = ResizeMode.Crop,
                                                        Size = new Size(objectImageWidth, objectImageHeight)
                                                        //,
                                                        //TargetRectangle = new Rectangle(
                                                        //        (sourceImage.Width - objectImageWidth) / 2,
                                                        //        (sourceImage.Height - objectImageHeight) / 2,
                                                        //        objectImageWidth,
                                                        //        objectImageHeight)
                                                });
                                        }
                                        if (isNeedStampWatermark == false
                                        || watermarkInfo == null)
                                        {
                                                return;
                                        }

                                        var watermarkImageAbsoluteFilePath
                                        = watermarkInfo
                                        .WatermarkImageFilePath
                                        ?.ToAbsoluteFilePathInRootPath(
                                                BaoXia.Utils.Environment.ApplicationDirectoryPath);
                                        if (string.IsNullOrEmpty(watermarkImageAbsoluteFilePath))
                                        {
                                                return;
                                        }

                                        var loadWatermarkImageTask
                                        = _imageCache.GetAsync(
                                                watermarkImageAbsoluteFilePath,
                                                null);
                                        // !!!
                                        loadWatermarkImageTask?.Wait();
                                        // !!!
                                        var watermarkImage = loadWatermarkImageTask?.Result;
                                        if (watermarkImage == null)
                                        {
                                                return;
                                        }


                                        ////////////////////////////////////////////////
                                        // 开始绘制水印：
                                        ////////////////////////////////////////////////

                                        var watermarkMarginLeft = watermarkInfo.MarginBottom;
                                        var watermarkMarginTop = watermarkInfo.MarginTop;
                                        var watermarkMarginRight = watermarkInfo.MarginRight;
                                        var watermarkMarginBottom = watermarkInfo.MarginBottom;
                                        var isWatermarkDrawImageFirst
                                        = watermarkInfo.IsWatermarkDrawImageFirst;
                                        var watermarkImageWidth
                                        = watermarkImage.Width;
                                        var watermarkImageHeight
                                        = watermarkImage.Height;
                                        var watermarkImageAndCaptionSeparatorSize
                                        = watermarkInfo.WatermarkImageAndCaptionSeparatorSize;
                                        var watermarkCaptionFontFileName
                                        = watermarkInfo.WatermarkCaptionFontFileName;
                                        SixLabors.Fonts.Font? watermarkCaptionFont = null;
                                        var watermarkCaptionFontSize
                                        = watermarkInfo.WatermarkCaptionFontSize;
                                        int watermarkCaptionWidth = 0;
                                        int watermarkCaptionHeight = 0;
                                        if (watermarkCaptionFontFileName?.Length > 0
                                                && watermarkCaption?.Length > 0)
                                        {
                                                watermarkCaptionFont
                                                = _fontCache.Get(watermarkCaptionFontFileName, null)
                                                ?.FontFamily
                                                .CreateFont(watermarkCaptionFontSize);
                                                if (watermarkCaptionFont != null)
                                                {
                                                        var watermarkCaptionSize
                                                        = SixLabors.Fonts.TextMeasurer.Measure(
                                                                watermarkCaption,
                                                                new SixLabors.Fonts.TextOptions(watermarkCaptionFont));
                                                        // !!!
                                                        watermarkCaptionWidth = (int)Math.Ceiling(watermarkCaptionSize.Width);
                                                        watermarkCaptionHeight = (int)Math.Ceiling(watermarkCaptionSize.Height);
                                                        // !!!
                                                }
                                        }


                                        ////////////////////////////////////////////////

                                        var watermarkWidth = 0;
                                        var watermarkHeight = 0;
                                        var watermarkImageLeft = 0;
                                        var watermarkImageTop = 0;
                                        var watermarkCaptionLeft = 0;
                                        var watermarkCaptionTop = 0;
                                        switch (watermarkInfo.WatermarkLayout)
                                        {
                                                default:
                                                case ImageWatermarkInfo.LayoutType.HorizontalLayout:
                                                        {
                                                                watermarkWidth = watermarkImageWidth;
                                                                if (watermarkCaptionWidth > 0)
                                                                {
                                                                        watermarkWidth
                                                                        += watermarkImageAndCaptionSeparatorSize
                                                                        + watermarkCaptionWidth;
                                                                }
                                                                watermarkHeight
                                                                = watermarkImageHeight > watermarkCaptionHeight
                                                                ? watermarkImageHeight
                                                                : watermarkCaptionHeight;


                                                                if (watermarkInfo.IsWatermarkDrawImageFirst)
                                                                {
                                                                        watermarkImageLeft = 0;
                                                                        watermarkCaptionLeft
                                                                        = watermarkImageLeft
                                                                        + watermarkImageWidth
                                                                        + watermarkImageAndCaptionSeparatorSize;
                                                                }
                                                                else
                                                                {
                                                                        watermarkCaptionLeft = 0;
                                                                        watermarkImageLeft
                                                                        = watermarkCaptionLeft
                                                                        + watermarkCaptionWidth
                                                                        + watermarkImageAndCaptionSeparatorSize;
                                                                }

                                                                switch (watermarkInfo.WatermarkLayoutAlignType)
                                                                {
                                                                        case ImageWatermarkInfo.AlignType.Left:
                                                                        case ImageWatermarkInfo.AlignType.Top:
                                                                                {
                                                                                        watermarkImageTop = 0;
                                                                                        watermarkCaptionTop = 0;
                                                                                }
                                                                                break;
                                                                        case ImageWatermarkInfo.AlignType.Right:
                                                                        case ImageWatermarkInfo.AlignType.Bottom:
                                                                                {
                                                                                        watermarkImageTop
                                                                                        = watermarkHeight
                                                                                        - watermarkImageHeight;
                                                                                        watermarkCaptionTop
                                                                                        = watermarkHeight
                                                                                        - watermarkCaptionHeight;
                                                                                }
                                                                                break;
                                                                        default:
                                                                        case ImageWatermarkInfo.AlignType.Center:
                                                                                {
                                                                                        watermarkImageTop
                                                                                        = (watermarkHeight
                                                                                        - watermarkImageHeight)
                                                                                        / 2;
                                                                                        watermarkCaptionTop
                                                                                        = (watermarkHeight
                                                                                        - watermarkCaptionHeight)
                                                                                        / 2;
                                                                                }
                                                                                break;
                                                                }
                                                        }
                                                        break;
                                                case ImageWatermarkInfo.LayoutType.VerticalLayout:
                                                        {
                                                                watermarkWidth
                                                                = watermarkImageWidth > watermarkCaptionWidth
                                                                ? watermarkImageWidth
                                                                : watermarkCaptionWidth;
                                                                watermarkHeight
                                                                = watermarkImageHeight;
                                                                {
                                                                        watermarkHeight
                                                                        += watermarkImageAndCaptionSeparatorSize
                                                                        + watermarkCaptionHeight;
                                                                }


                                                                if (watermarkInfo.IsWatermarkDrawImageFirst)
                                                                {
                                                                        watermarkImageTop = 0;
                                                                        watermarkCaptionTop
                                                                        = watermarkImageTop
                                                                        + watermarkImageHeight
                                                                        + watermarkImageAndCaptionSeparatorSize;
                                                                }
                                                                else
                                                                {
                                                                        watermarkCaptionTop = 0;
                                                                        watermarkImageTop
                                                                        = watermarkCaptionTop
                                                                        + watermarkCaptionHeight
                                                                        + watermarkImageAndCaptionSeparatorSize;
                                                                }

                                                                switch (watermarkInfo.WatermarkLayoutAlignType)
                                                                {
                                                                        case ImageWatermarkInfo.AlignType.Left:
                                                                        case ImageWatermarkInfo.AlignType.Top:
                                                                                {
                                                                                        watermarkImageLeft = 0;
                                                                                        watermarkCaptionLeft = 0;
                                                                                }
                                                                                break;
                                                                        case ImageWatermarkInfo.AlignType.Right:
                                                                        case ImageWatermarkInfo.AlignType.Bottom:
                                                                                {
                                                                                        watermarkImageLeft
                                                                                        = watermarkWidth
                                                                                        - watermarkImageWidth;
                                                                                        watermarkCaptionLeft
                                                                                        = watermarkWidth
                                                                                        - watermarkCaptionWidth;
                                                                                }
                                                                                break;
                                                                        default:
                                                                        case ImageWatermarkInfo.AlignType.Center:
                                                                                {
                                                                                        watermarkImageLeft
                                                                                        = (watermarkWidth
                                                                                        - watermarkImageWidth)
                                                                                        / 2;
                                                                                        watermarkCaptionLeft
                                                                                        = (watermarkWidth
                                                                                        - watermarkCaptionWidth)
                                                                                        / 2;
                                                                                }
                                                                                break;
                                                                }
                                                        }
                                                        break;
                                        }

                                        var watermarkLeft = 0;
                                        switch (watermarkInfo.HorizontalAlignType)
                                        {
                                                case ImageWatermarkInfo.AlignType.Left:
                                                case ImageWatermarkInfo.AlignType.Top:
                                                        {
                                                                watermarkLeft = watermarkInfo.MarginLeft;
                                                        }
                                                        break;
                                                default:
                                                case ImageWatermarkInfo.AlignType.Right:
                                                case ImageWatermarkInfo.AlignType.Bottom:
                                                        {
                                                                watermarkLeft
                                                                = objectImageWidth
                                                                - watermarkInfo.MarginRight
                                                                - watermarkWidth;
                                                        }
                                                        break;
                                                case ImageWatermarkInfo.AlignType.Center:
                                                        {
                                                                watermarkLeft
                                                                = (objectImageWidth
                                                                - watermarkWidth)
                                                                / 2;
                                                        }
                                                        break;
                                        }
                                        var watermarkTop = 0;
                                        switch (watermarkInfo.VerticalAlignType)
                                        {
                                                case ImageWatermarkInfo.AlignType.Left:
                                                case ImageWatermarkInfo.AlignType.Top:
                                                        {
                                                                watermarkTop = watermarkInfo.MarginTop;
                                                        }
                                                        break;
                                                default:
                                                case ImageWatermarkInfo.AlignType.Right:
                                                case ImageWatermarkInfo.AlignType.Bottom:
                                                        {
                                                                watermarkTop
                                                                = objectImageHeight
                                                                - watermarkInfo.MarginBottom
                                                                - watermarkHeight;
                                                        }
                                                        break;
                                                case ImageWatermarkInfo.AlignType.Center:
                                                        {
                                                                watermarkTop
                                                                = (objectImageHeight
                                                                - watermarkHeight)
                                                                / 2;
                                                        }
                                                        break;
                                        }

                                        watermarkImageLeft += watermarkLeft;
                                        watermarkImageTop += watermarkTop;
                                        {
                                                imageProcessingContext.DrawImage(
                                                        watermarkImage,
                                                        new Point(watermarkImageLeft, watermarkImageTop),
                                                        1.0F);
                                        }

                                        watermarkCaptionLeft += watermarkLeft;
                                        watermarkCaptionTop += watermarkTop;
                                        if (watermarkCaptionFont != null)
                                        {
                                                if (watermarkInfo.WatermarkCaptionColor?.ToRGBA(
                                                        out var watermarkCaptionColorRed,
                                                        out var watermarkCaptionColorGreen,
                                                        out var watermarkCaptionColorBlue,
                                                        out var watermarkCaptionColorAlpha) == true)
                                                {
                                                        var watermarkCaptionColor
                                                        = Color.FromRgba(
                                                                watermarkCaptionColorRed,
                                                                watermarkCaptionColorGreen,
                                                                watermarkCaptionColorBlue,
                                                                (byte)(255.0F * watermarkCaptionColorAlpha));
                                                        var watermarkCaptionColorWithoutAlpha
                                                        = Color.FromRgba(
                                                                watermarkCaptionColorRed,
                                                                watermarkCaptionColorGreen,
                                                                watermarkCaptionColorBlue,
                                                                255);

                                                        var watermarkCaptionBorderSize
                                                        = watermarkInfo.WatermarkCaptionBorderSize;
                                                        if (watermarkInfo.WatermarkCaptionBorderColor?.ToRGBA(
                                                                out var red,
                                                                out var green,
                                                                out var blue,
                                                                out var alpha) == true)
                                                        {
                                                                var watermarkCaptionBorderColor
                                                                = Color.FromRgba(
                                                                        red,
                                                                        green,
                                                                        blue,
                                                                        (byte)(255.0F * alpha));
                                                                imageProcessingContext.DrawText(
                                                                        new DrawingOptions(),
                                                                        watermarkCaption,
                                                                        watermarkCaptionFont,
                                                                        new SolidBrush(watermarkCaptionColor),
                                                                        new Pen(watermarkCaptionBorderColor, watermarkCaptionBorderSize),
                                                                        new Point(
                                                                                watermarkCaptionLeft,
                                                                                watermarkCaptionTop));
                                                        }
                                                        else
                                                        {
                                                                imageProcessingContext.DrawText(
                                                                        watermarkCaption,
                                                                        watermarkCaptionFont,
                                                                        SixLabors.ImageSharp.Color.FromRgba(0, 0, 0, 16),
                                                                        new Point(
                                                                                watermarkCaptionLeft,
                                                                                watermarkCaptionTop));
                                                        }
                                                }
                                        }
                                });
                        return objectImage!;
                }


                private static ImageFileInfo? SaveImageToFilePathAndCreateImageFileInfo(
                        Image? image,
                        string objectFileAbsolutePath,
                        string? objectFileAbsoluteUri,
                        IImageEncoder imageEncoder,
                        int imageSaveQuality)
                {
                        int imageWidth;
                        int imageHeight;
                        if (image != null)
                        {
                                imageWidth = image.Width;
                                imageHeight = image.Height;
                        }
                        else
                        {
                                return null;
                        }


                        var imageBytesCountSaved
                        = SaveImageToFilePath(
                                objectFileAbsolutePath,
                                image,
                                imageEncoder,
                                imageSaveQuality);

                        var imageFileInfo = new ImageFileInfo()
                        {
                                FileUrl = objectFileAbsoluteUri,
                                FileSizeInKB = imageBytesCountSaved / 1024,

                                Width = imageWidth,
                                Height = imageHeight
                        };
                        return imageFileInfo;
                }

                #endregion



                ////////////////////////////////////////////////
                // @自身属性
                ////////////////////////////////////////////////

                #region 自身属性

                private Data.FileInfoDbContext _fileInfoDbContext;

                #endregion

                ////////////////////////////////////////////////
                // @自身实现
                ////////////////////////////////////////////////

                #region 自身实现

                public UploadController(Data.FileInfoDbContext fileInfoDbContext)
                {
                        _fileInfoDbContext = fileInfoDbContext;
                }

                private async Task<UploadResponseType> ProcessUploadRequestAsync<UploadResponseType>(
                        UploadRequest? request,
                        Data.FileInfoDbContext fileInfoDbContext,
                        Func<FilePathRule,
                            string,
                            string?,
                            IFormFile,
                            //
                            UploadRequest,
                            int,
                            int,
                            string?,
                            string[]?,
                            //
                            FileUploadStatisticsInfo,
                            //
                            Task<UploadResponseType>>?
                    toGetUploadResponseBySaveFileAtAbsolutePathAsync = null)
                        where UploadResponseType : UploadResponse, new()
                {
                        Models.FileInfo? fileInfoInDb = null;
                        var fileUploadStatisticsInfo = new FileUploadStatisticsInfo();
                        var responseBeginTime = DateTime.Now;
                        await using var _ = new CodesAtFunctionEnd(
                                async () =>
                                {
                                        if (fileInfoInDb == null
                                        || fileInfoInDb.Id == 0)
                                        {
                                                return;
                                        }

                                        var responseEndTime = DateTime.Now;
                                        fileUploadStatisticsInfo.SecondsToResponse
                                                = (responseEndTime - responseBeginTime)
                                                .TotalSeconds;

                                        ////////////////////////////////////////////////
                                        // !!! 存储最终的文件信息 !!!
                                        ////////////////////////////////////////////////
                                        var updateFileInfoToDatabaseBeginTime = DateTime.Now;
                                        {
                                                var updateFileInfoToDatabaseBeinTime = DateTime.Now;
                                                {
                                                        fileInfoInDb.SecondsToResponse = fileUploadStatisticsInfo.SecondsToResponse;
                                                        fileInfoInDb.SecondsToDatabaseOperation
                                                        = fileUploadStatisticsInfo.SecondsToDatabaseOperation_CreateFileInfo
                                                        + fileUploadStatisticsInfo.SecondsToDatabaseOperation_UpadateFileInfo;
                                                        fileInfoInDb.SecondsToFileProcessing = fileUploadStatisticsInfo.SecondsToFileProcessing;
                                                        fileInfoInDb.SecondsToFileStorage = fileUploadStatisticsInfo.SecondsToFileStorage;
                                                        {
                                                                // !!!
                                                                await fileInfoDbContext.SaveChangesAsync();
                                                                // !!!
                                                        }
                                                        fileInfoDbContext.Entry(fileInfoInDb).State = EntityState.Detached;
                                                }
                                                var updateFileInfoToDatabaseEndTime = DateTime.Now;
                                                fileUploadStatisticsInfo.SecondsToDatabaseOperation_UpadateFileInfo
                                                = (updateFileInfoToDatabaseEndTime - updateFileInfoToDatabaseBeginTime)
                                                .TotalSeconds;
                                                // !!!
                                                fileInfoInDb.SecondsToDatabaseOperation
                                                = fileUploadStatisticsInfo.SecondsToDatabaseOperation_CreateFileInfo
                                                + fileUploadStatisticsInfo.SecondsToDatabaseOperation_UpadateFileInfo;
                                                // !!!
                                                UploadController.AddFileInfoToStorageQueue(fileInfoInDb);
                                                // !!!
                                        }
                                        ////////////////////////////////////////////////
                                });

                        if (request == null
                            || request.IsValid == false)
                        {
                                return new UploadResponseType()
                                {
                                        Error = Error.InvalidRequest
                                };
                        }

                        var imageFile = request.File;
                        var imageFileName = imageFile?.FileName;
                        string? imageFileExtenionName = null;
                        if (imageFileName?.Length > 0)
                        {
                                imageFileExtenionName
                                    = imageFileName.ToFileExtensionName();
                        }
                        var fileTags = request.FileTags;
                        var filePathRuleMatched
                            = Config.Service.GetFilePathRuleWithFileExtensionName(
                                imageFileExtenionName,
                                fileTags);
                        if (filePathRuleMatched == null)
                        {
                                return new UploadResponseType()
                                {
                                        Error = Error.ObjectNotExisted
                                };
                        }

                        var createFileInfoToDatabaseBeginTime = DateTime.Now;
                        ////////////////////////////////////////////////
                        var currentAuthorizationInfo
                            = this.GetCurrentAuthorizationInfo();
                        var now = DateTime.Now;
                        fileInfoInDb = new Models.FileInfo()
                        {
                                CreateTime = now,
                                UpdateTime = now,
                        };
                        {
                                fileInfoDbContext.FileInfo?.Add(fileInfoInDb);
                        }
                        await fileInfoDbContext.SaveChangesAsync();
                        ////////////////////////////////////////////////
                        var createFileInfoToDatabaseEndTime = DateTime.Now;
                        fileUploadStatisticsInfo.SecondsToDatabaseOperation_CreateFileInfo
                                = (createFileInfoToDatabaseEndTime
                                - createFileInfoToDatabaseBeginTime).TotalSeconds;

                        var currentUserId = currentAuthorizationInfo.UserId;
                        string? objectFileAbsolutePath
                                = filePathRuleMatched.GetFileAbsolutePathWithFileId(
                                        currentUserId,
                                        fileInfoInDb.Id,
                                        imageFileExtenionName,
                                        fileTags,
                                        0,
                                        0);
                        if (string.IsNullOrEmpty(objectFileAbsolutePath))
                        {
                                throw new ApplicationException(
                                        "无法生成文件的绝对路径：\r\n"
                                        + "用户Id：" + currentAuthorizationInfo.UserId + "，"
                                        + "文件Id：" + fileInfoInDb.Id + "，"
                                        + "文件扩展名：" + imageFileExtenionName + "，"
                                        + "文件标签：" + StringExtension.StringWithStrings(request.FileTags) + "。");
                        }

                        var objectFileAbsoluteDictionaryPath
                                = objectFileAbsolutePath.ToFileSystemDirectoryPath(true);
                        {
                                System.IO.Directory.CreateDirectory(objectFileAbsoluteDictionaryPath);
                        }
                        string? objectFileAbsoluteUri
                                = filePathRuleMatched.GetFileAbsoluteUriWithFileId(
                                        currentUserId,
                                        fileInfoInDb.Id,
                                        imageFileExtenionName,
                                        fileTags,
                                        0,
                                        0);

                        UploadResponseType? response = null;
                        var sourceFile = request.File!;
                        if (toGetUploadResponseBySaveFileAtAbsolutePathAsync != null)
                        {
                                response
                                    = await toGetUploadResponseBySaveFileAtAbsolutePathAsync(
                                    filePathRuleMatched,
                                    objectFileAbsolutePath,
                                    objectFileAbsoluteUri,
                                    sourceFile,
                                    //
                                    request,
                                    currentUserId,
                                    fileInfoInDb.Id,
                                    imageFileExtenionName,
                                    fileTags,
                                    //
                                    fileUploadStatisticsInfo);
                                {
                                        response.FileUploadStatisticsInfo = fileUploadStatisticsInfo;
                                }
                        }
                        else
                        {
                                var fileStorageBeginTime = DateTime.Now;
                                ////////////////////////////////////////////////
                                var objectFileWriteStream = new FileStream(objectFileAbsolutePath, FileMode.CreateNew);
                                {
                                        await sourceFile.CopyToAsync(objectFileWriteStream);
                                }
                                var fileSaveBytesCount = objectFileWriteStream.Length;
                                ////////////////////////////////////////////////
                                var fileStorageEndTime = DateTime.Now;
                                fileUploadStatisticsInfo.SecondsToDatabaseOperation_CreateFileInfo
                                        = (fileStorageEndTime - fileStorageBeginTime).TotalSeconds;

                                // !!!
                                response = new UploadResponseType
                                {
                                        FileInfo = new FileInfo(
                                            objectFileAbsoluteUri,
                                            fileSaveBytesCount / 1024 / 1024),

                                        FileUploadStatisticsInfo = fileUploadStatisticsInfo
                                };
                                // !!!
                        }
                        return response;
                }

                public class UploadRequest
                {
                        [MemberNotNullWhen(true, "IsValid")]
                        public IFormFile? File { get; set; }

                        protected string? _fileTagsString;

                        protected string[]? _fileTags;

                        public string? FileTagsString
                        {
                                get
                                {
                                        return _fileTagsString;
                                }

                                set
                                {
                                        _fileTagsString = value;
                                        _fileTags = _fileTagsString?.Split(",");
                                }
                        }

                        public string[]? FileTags => _fileTags;

                        public bool IsValid
                        {
                                get
                                {
                                        if (this.File != null)
                                        {
                                                return true;
                                        }
                                        return false;
                                }
                        }
                }

                public class FileInfo
                {
                        public string? FileUrl { get; set; }

                        public long FileSizeInKB { get; set; }
                        public float FileSizeInMB
                        {
                                get
                                {
                                        return this.FileSizeInKB / 1024;
                                }
                        }

                        public FileInfo()
                        { }

                        public FileInfo(
                                string? fileUrl,
                                long fileSizeInKB)
                        {
                                this.FileUrl = fileUrl;
                                this.FileSizeInKB = fileSizeInKB;
                        }
                }

                public class UploadResponse : ViewModels.Response
                {
                        public FileInfo? FileInfo { get; set; }

                        public FileUploadStatisticsInfo? FileUploadStatisticsInfo { get; set; }
                }

                public async Task<IActionResult> Index([FromForm] UploadRequest? request)
                {
                        var response = new UploadResponse();
                        try
                        {
                                response
                                    = await this.ProcessUploadRequestAsync<UploadResponse>(
                                        request,
                                        _fileInfoDbContext);
                        }
                        catch (Exception exception)
                        {
                                Log.Exception.Logs(this, "上传文件失败，程序异常。", exception);
                                //
                                response.Error = Error.ProgramError;
                        }
                        return Json(response);
                }

                public class ImageFileInfo : FileInfo
                {
                        public int Width { get; set; }

                        public int Height { get; set; }
                }

                public class ImageUploadRequest : UploadRequest
                {
                        public ImageArea? ImageSaveArea { get; set; }

                        public ImageSize? ImageSaveSize { get; set; }

                        public string? ImageSaveFormat { get; set; }

                        public int ImageSaveQuality { get; set; }

                        public bool IsAutoCreateListAndContentImage { get; set; }

                        public bool IsWatermarkEnable { get; set; }

                        public string? WatermarkCaption { get; set; }
                }

                public class ImageUploadResponse : UploadResponse
                {
                        public ImageFileInfo? ListImageFileInfo { get; set; }

                        public ImageFileInfo? ContentImageFileInfo { get; set; }

                        public ImageFileInfo? SourceImageFileInfo { get; set; }
                }

                public async Task<IActionResult> Image([FromForm] ImageUploadRequest? request)
                {
                        var response = new ImageUploadResponse();
                        try
                        {
                                response = await this.ProcessUploadRequestAsync<ImageUploadResponse>(
                                        request,
                                        _fileInfoDbContext,
                                        async (
                                            filePathRuleMatched,
                                            objectFileAbsolutePath,
                                            objectFileAbsoluteUri,
                                            sourceFile,
                                            uploadRequest,
                                            fileId,
                                            userId,
                                            fileExtensionName,
                                            fileTags,
                                            fileUploadStatisticsInfo) =>
                                        {
                                                var sourceFileStream = sourceFile.OpenReadStream();
                                                var image = await SixLabors.ImageSharp.Image.LoadAsync(sourceFileStream);
                                                if (image == null)
                                                {
                                                        throw new ApplicationException("无法解析图象数据。");
                                                }

                                                ImageArea? imageSaveArea = null;
                                                ImageSize? imageSaveSize = null;
                                                string? imageSaveFileExtensionName = null;
                                                var imageSaveQuality = 100;
                                                bool isAutoCreateListAndContentImage = false;
                                                var watermarkCaption = request?.WatermarkCaption;

                                                if (uploadRequest is ImageUploadRequest imageUploadRequest)
                                                {
                                                        imageSaveArea = imageUploadRequest.ImageSaveArea;
                                                        imageSaveSize = imageUploadRequest.ImageSaveSize;

                                                        imageSaveFileExtensionName = imageUploadRequest.ImageSaveFormat;
                                                        if (string.IsNullOrEmpty(imageSaveFileExtensionName))
                                                        {
                                                                imageSaveFileExtensionName
                                                                = objectFileAbsolutePath.ToFileExtensionName();
                                                        }

                                                        imageSaveQuality = imageUploadRequest.ImageSaveQuality;

                                                        isAutoCreateListAndContentImage = imageUploadRequest.IsAutoCreateListAndContentImage;
                                                }

                                                var imageEncoder
                                                = ISIImageFormatExtension.GetImageEncoderWithFileExtensionName(
                                                        imageSaveFileExtensionName);
                                                if (imageEncoder == null)
                                                {
                                                        throw new ApplicationException("未知的图片格式：" + objectFileAbsolutePath + "。");
                                                }

                                                if (imageSaveQuality <= 0)
                                                {
                                                        if (imageEncoder
                                                        is SixLabors.ImageSharp.Formats.Webp.WebpEncoder)
                                                        {
                                                                imageSaveQuality
                                                                = filePathRuleMatched.ImageSaveQualityForWebpDefault;
                                                        }
                                                        else if (imageEncoder
                                                        is SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder)
                                                        {
                                                                imageSaveQuality
                                                                = filePathRuleMatched.ImageSaveQualityForJpegDefault;
                                                        }
                                                }

                                                var fileProcessingBeginTime_CropSourceImage = DateTime.Now;
                                                if (imageSaveArea?.IsValid == true
                                                || imageSaveSize?.IsValid == true)
                                                {
                                                        image.Mutate(imageProcessingContext =>
                                                        {
                                                                if (imageSaveArea?.IsValid == true)
                                                                {
                                                                        imageProcessingContext
                                                                        = imageProcessingContext
                                                                        .Crop(imageSaveArea.ToISRectangle());
                                                                }

                                                                if (imageSaveSize?.IsValid == true)
                                                                {
                                                                        imageProcessingContext.Crop(
                                                                                imageSaveSize.Width,
                                                                                imageSaveSize.Height);
                                                                }
                                                        });
                                                }
                                                var fileProcessingEndTime_CropSourceImage = DateTime.Now;
                                                var fileProcessingSeconds
                                                = (fileProcessingEndTime_CropSourceImage
                                                - fileProcessingBeginTime_CropSourceImage)
                                                .TotalSeconds;

                                                var response = new ImageUploadResponse();
                                                var tasksToSaveImageFile = new List<Task>();

                                                // 保存图片文件，原始图片：
                                                var sourceImageFileProcessingSeconds = 0.0;
                                                var sourceImageFileStorageSeconds = 0.0;
                                                var taskToSaveSourceImageFile
                                                = Task.Run(() =>
                                                {
                                                        var imageFileAbsolutePath
                                                        = filePathRuleMatched.GetFileAbsolutePathWithFileId(
                                                            userId,
                                                            fileId,
                                                            fileExtensionName,
                                                            fileTags,
                                                            image.Width,
                                                            image.Height);
                                                        if (string.IsNullOrEmpty(imageFileAbsolutePath))
                                                        {
                                                                throw new ApplicationException("无法保存图片文件，指定的文件路径无效。");
                                                        }

                                                        var imageFileAbsoluteUri
                                                        = filePathRuleMatched.GetFileAbsoluteUriWithFileId(
                                                            userId,
                                                            fileId,
                                                            fileExtensionName,
                                                            fileTags,
                                                            image.Width,
                                                            image.Height);
                                                        ////////////////////////////////////////////////


                                                        var sourceImageFileProcessingBeginTime = DateTime.Now;
                                                        //////////////////////////////////////////////
                                                        var sourceImage = UploadController.ProcessImageFileWithSourceImage(
                                                                image,
                                                                null,
                                                                request?.IsWatermarkEnable == true,
                                                                filePathRuleMatched.GetWatermarkInfoForImage(
                                                                        image.Width,
                                                                        image.Height),
                                                                watermarkCaption);
                                                        //////////////////////////////////////////////
                                                        var sourceImageFileProcessingEndTime = DateTime.Now;
                                                        sourceImageFileProcessingSeconds
                                                        = (sourceImageFileProcessingEndTime - sourceImageFileProcessingBeginTime)
                                                        .TotalSeconds;



                                                        var sourceImageFileStorageBeginTime = DateTime.Now;
                                                        ////////////////////////////////////////////////
                                                        var imageFileInfo
                                                        = UploadController.SaveImageToFilePathAndCreateImageFileInfo(
                                                                sourceImage,
                                                                imageFileAbsolutePath,
                                                                imageFileAbsoluteUri,
                                                                imageEncoder,
                                                                imageSaveQuality);
                                                        ////////////////////////////////////////////////
                                                        var sourceImageFileStorageEndTime = DateTime.Now;
                                                        sourceImageFileStorageSeconds
                                                        = (sourceImageFileStorageEndTime - sourceImageFileStorageBeginTime)
                                                        .TotalSeconds;


                                                        // !!!
                                                        response.FileInfo = imageFileInfo;
                                                        response.SourceImageFileInfo = imageFileInfo;
                                                        // !!!
                                                });
                                                tasksToSaveImageFile.Add(taskToSaveSourceImageFile);

                                                var listImageFileProcessingSeconds = 0.0;
                                                var listImageFileStorageSeconds = 0.0;
                                                var contentImageFileProcessingSeconds = 0.0;
                                                var contentImageFileStorageSeconds = 0.0;
                                                if (isAutoCreateListAndContentImage)
                                                {
                                                        // 保存图片文件，列表缩略图：
                                                        tasksToSaveImageFile.Add(Task.Run(
                                                                () =>
                                                                {
                                                                        if (ISIImageFormatExtension.TryGetImageFormatWithFileExtensionName(
                                                                            filePathRuleMatched.ListImageFormat,
                                                                            out var listImageFormat,
                                                                            out var listImageEncoder,
                                                                            out _) != true)
                                                                        {
                                                                                throw new ApplicationException("无法创建列表图片，指定的列表图片的格式无效。");
                                                                        }
                                                                        var listImageFileExtensionName
                                                                        = listImageFormat.FileExtensions.First();

                                                                        var listImageSize
                                                                        = filePathRuleMatched.ListImageSize;
                                                                        if (listImageSize?.IsValid != true)
                                                                        {
                                                                                throw new ApplicationException("无法创建列表图片，指定的列表图片尺寸无效。");
                                                                        }

                                                                        var listImageFileAbsolutePath
                                                                        = filePathRuleMatched.GetListImageFileAbsolutePathWithFileId(
                                                                                userId,
                                                                                fileId,
                                                                                listImageFileExtensionName,
                                                                                fileTags,
                                                                                listImageSize.Width,
                                                                                listImageSize.Height);
                                                                        if (string.IsNullOrEmpty(listImageFileAbsolutePath))
                                                                        {
                                                                                throw new ApplicationException("无法创建列表图片，指定的文件路径无效。");
                                                                        }

                                                                        var listImageSaveQuality
                                                                        = filePathRuleMatched.ListImageSaveQuality;

                                                                        var listImageFileAbsoluteUri
                                                                        = filePathRuleMatched.GetListImageFileAbsoluteUriWithFileId(
                                                                                userId,
                                                                                fileId,
                                                                                listImageFileExtensionName,
                                                                                fileTags,
                                                                                listImageSize.Width,
                                                                                listImageSize.Height);
                                                                        ////////////////////////////////////////////////


                                                                        var listImageFileProcessingBeginTime = DateTime.Now;
                                                                        ////////////////////////////////////////////////
                                                                        var listImage = UploadController.ProcessImageFileWithSourceImage(
                                                                                image,
                                                                                listImageSize,
                                                                                request?.IsWatermarkEnable == true,
                                                                                filePathRuleMatched.GetWatermarkInfoForImage(
                                                                                listImageSize.Width,
                                                                                listImageSize.Height),
                                                                                watermarkCaption);
                                                                        ////////////////////////////////////////////////
                                                                        var listImageFileProcessingEndTime = DateTime.Now;
                                                                        listImageFileProcessingSeconds
                                                                        = (listImageFileProcessingEndTime - listImageFileProcessingBeginTime)
                                                                        .TotalSeconds;


                                                                        var listImageFileStorageBeginTime = DateTime.Now;
                                                                        ////////////////////////////////////////////////
                                                                        var imageFileInfo
                                                                        = UploadController.SaveImageToFilePathAndCreateImageFileInfo(
                                                                            listImage,
                                                                            listImageFileAbsolutePath,
                                                                            listImageFileAbsoluteUri,
                                                                            listImageEncoder,
                                                                            listImageSaveQuality);
                                                                        ////////////////////////////////////////////////
                                                                        var listImageFileStorageEndTime = DateTime.Now;
                                                                        listImageFileStorageSeconds
                                                                        = (listImageFileStorageEndTime - listImageFileStorageBeginTime)
                                                                        .TotalSeconds;

                                                                        // !!!
                                                                        response.ListImageFileInfo = imageFileInfo;
                                                                        // !!!
                                                                }));

                                                        // 保存图片文件，【内容适中】图片：
                                                        tasksToSaveImageFile.Add(Task.Run(
                                                                () =>
                                                                {
                                                                        if (ISIImageFormatExtension.TryGetImageFormatWithFileExtensionName(
                                                                            filePathRuleMatched.ContentImageFormat,
                                                                            out var contentImageFormat,
                                                                            out var contentImageEncoder,
                                                                            out _) != true)
                                                                        {
                                                                                throw new ApplicationException("无法创建内容图片，指定的内容图片的格式无效。");
                                                                        }
                                                                        var contentImageFileExtensionName
                                                                        = contentImageFormat.FileExtensions.First();

                                                                        var contentImageSize
                                                                        = filePathRuleMatched.ContentImageSize;
                                                                        if (contentImageSize?.IsValid != true)
                                                                        {
                                                                                throw new ApplicationException("无法创建内容图片，指定的内容图片尺寸无效。");
                                                                        }

                                                                        var contentImageFileAbsolutePath
                                                                        = filePathRuleMatched.GetContentImageFileAbsolutePathWithFileId(
                                                                                userId,
                                                                                fileId,
                                                                                contentImageFileExtensionName,
                                                                                fileTags,
                                                                                contentImageSize.Width,
                                                                                contentImageSize.Height);
                                                                        if (string.IsNullOrEmpty(contentImageFileAbsolutePath))
                                                                        {
                                                                                throw new ApplicationException("无法创建内容图片，指定的文件路径无效。");
                                                                        }

                                                                        var contentImageSaveQuality
                                                                        = filePathRuleMatched.ContentImageSaveQuality;

                                                                        var contentImageFileAbsoluteUri
                                                                        = filePathRuleMatched.GetContentImageFileAbsoluteUriWithFileId(
                                                                                userId,
                                                                                fileId,
                                                                                contentImageFileExtensionName,
                                                                                fileTags,
                                                                                contentImageSize.Width,
                                                                                contentImageSize.Height);
                                                                        ////////////////////////////////////////////////


                                                                        var contentImageFileProcessingBeginTime = DateTime.Now;
                                                                        ////////////////////////////////////////////////
                                                                        var contentImage = UploadController.ProcessImageFileWithSourceImage(
                                                                                image,
                                                                                contentImageSize,
                                                                                request?.IsWatermarkEnable == true,
                                                                                filePathRuleMatched.GetWatermarkInfoForImage(
                                                                                contentImageSize.Width,
                                                                                contentImageSize.Height),
                                                                                watermarkCaption);
                                                                        ////////////////////////////////////////////////
                                                                        var contentImageFileProcessingEndTime = DateTime.Now;
                                                                        contentImageFileProcessingSeconds
                                                                        = (contentImageFileProcessingEndTime - contentImageFileProcessingBeginTime)
                                                                        .TotalSeconds;


                                                                        var contentImageFileStorageBeginTime = DateTime.Now;
                                                                        ////////////////////////////////////////////////
                                                                        var imageFileInfo
                                                                        = UploadController.SaveImageToFilePathAndCreateImageFileInfo(
                                                                            contentImage,
                                                                            contentImageFileAbsolutePath,
                                                                            contentImageFileAbsoluteUri,
                                                                            contentImageEncoder,
                                                                            contentImageSaveQuality);
                                                                        ////////////////////////////////////////////////
                                                                        var contentImageFileStorageEndTime = DateTime.Now;
                                                                        contentImageFileStorageSeconds
                                                                        = (contentImageFileStorageEndTime - contentImageFileStorageBeginTime)
                                                                        .TotalSeconds;

                                                                        // !!!
                                                                        response.ContentImageFileInfo = imageFileInfo;
                                                                        // !!!
                                                                }));
                                                }

                                                // !!!
                                                await Task.WhenAll(tasksToSaveImageFile.ToArray());
                                                // !!!


                                                ////////////////////////////////////////////////
                                                ////////////////////////////////////////////////
                                                // !!!
                                                fileUploadStatisticsInfo.SecondsToFileProcessing
                                                = Max.Of(
                                                        sourceImageFileProcessingSeconds
                                                        + listImageFileProcessingSeconds
                                                        + contentImageFileProcessingSeconds);
                                                fileUploadStatisticsInfo.SecondsToFileStorage
                                                = Max.Of(
                                                        sourceImageFileStorageSeconds,
                                                         listImageFileStorageSeconds,
                                                         contentImageFileStorageSeconds);
                                                // !!!
                                                ////////////////////////////////////////////////
                                                ////////////////////////////////////////////////


                                                return response;
                                        });
                        }
                        catch (Exception exception)
                        {
                                Log.Exception.Logs(this, "上传图片文件失败，程序异常。", exception);
                                //
                                response.Error = Error.ProgramError;
                        }
                        return Json(response);
                }

                #endregion
        }
}
