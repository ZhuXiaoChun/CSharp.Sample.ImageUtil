﻿using CSharp.Sample.ImageUtil.Extensions;
using SkiaSharp;

namespace CSharp.Sample.ImageUtil.Utils
{
    public class ImageCard
    {
        ////////////////////////////////////////////////
        // @静态常量
        ////////////////////////////////////////////////

        #region 自身属性

        protected string _resourceImagesDirectoryPath;

        protected int _canvasWidth;
        // 2779 = iPhone 13 Pro Max 的屏幕高度。
        protected int _canvasHeightMax;
        protected Padding _canvasPadding;

        protected int _canvasDPIZoomRatio;
        protected float _canvasResolutionX;
        protected float _canvasResolutionY;

        protected int _canvasWidthInPixel;
        protected int _canvasHeightInPixel;

        protected float _textRenderOffsetY;


        ////////////////////////////////////////////////


        protected readonly HttpClient _httpClient;


        protected BaoXia.Utils.Cache.ObjectPool<SKSurface> _canvasSurfaces;

        protected BaoXia.Utils.Cache.AsyncItemsCache<string, SKBitmap, object?>? _imageWithUrlKey;

        protected BaoXia.Utils.Cache.ItemsCache<string, SKTypeface?, object?>? _fontFamiliesWithFontFileName;
        protected SKTypeface _fontFamily_SourceHanSansSC_Regular;
        protected SKTypeface _fontFamily_SourceHanSansSC_Midium;
        protected SKTypeface _fontFamily_SourceHanSansSC_Bold;

        #endregion


        ////////////////////////////////////////////////
        // @自身实现
        ////////////////////////////////////////////////

        #region 自身实现

        public ImageCard(
                string resourceImagesDirectoryPath,
                //
                int canvasWidth = 375,
                // 2779 = iPhone 13 Pro Max 的屏幕高度。
                int canvasHeightMax = 2778 * 3,
                //
                float canvasDPIDefault = 96.0F,
                int canvasDPIZoomRatio = 2,
                //
                int canvasPaddingLeft = 16,
                int canvasPaddingTop = 0,
                int canvasPaddingRight = 16,
                int canvasPaddingBottom = 0,
                //
                float textRenderOffsetY = -1.0F)
        {
            _resourceImagesDirectoryPath = resourceImagesDirectoryPath;

            _canvasWidth = canvasWidth;
            // 2779 = iPhone 13 Pro Max 的屏幕高度。
            _canvasHeightMax = canvasHeightMax;
            _canvasWidthInPixel = (int)(_canvasWidth * canvasDPIZoomRatio);
            _canvasHeightInPixel = (int)(_canvasHeightMax * canvasDPIZoomRatio);

            _canvasDPIZoomRatio = canvasDPIZoomRatio;

            _canvasResolutionX = canvasDPIDefault * canvasDPIZoomRatio;
            _canvasResolutionY = _canvasResolutionX;

            _canvasPadding = new(
                    canvasPaddingLeft,
                    canvasPaddingTop,
                    canvasPaddingRight,
                    canvasPaddingBottom);

            _textRenderOffsetY = textRenderOffsetY;

            ////////////////////////////////////////////////

            _httpClient = new();

            _canvasSurfaces
                    = new(() =>
                    {
                        var canvasInfo = new SKImageInfo(
                                width: _canvasWidthInPixel,
                                height: _canvasHeightInPixel,
                                colorType: SKColorType.Rgba8888,
                                alphaType: SKAlphaType.Premul);
                        var canvasSurface
                        = SKSurface.Create(canvasInfo);
                        var canvas = canvasSurface.Canvas;
                        {
                            canvas.Scale(_canvasDPIZoomRatio);
                        }
                        return canvasSurface;
                    });

            _imageWithUrlKey = new(
            async (imageUrl, _) =>
            {
                var image = await this.ImageDownloadFromAsync(imageUrl);
                { }
                return image;
            },
            null,
            () => 0.0,
            () => 0.0);


            _fontFamiliesWithFontFileName = new(
                    (fontFileName, _) =>
                    {
                        var fontFamily = this.FontFamilyFileNamed(fontFileName);
                        {
                        }
                        return fontFamily;
                    },
                    null,
                    null,
                    null);
            _fontFamily_SourceHanSansSC_Regular
            = _fontFamiliesWithFontFileName.Get("PingFang Regular.ttf", new object())!;
            _fontFamily_SourceHanSansSC_Midium
                    = _fontFamiliesWithFontFileName.Get("PingFang Medium.ttf", new object())!;
            _fontFamily_SourceHanSansSC_Bold
                    = _fontFamiliesWithFontFileName.Get("PingFang Bold.ttf", new object())!;


            ////////////////////////////////////////////////

        }

        protected SKBitmap ImageFileNamed(string imageFileName)
        {
            var imageFilePath
                    = BaoXia.Utils.Environment.ApplicationDirectoryPath
                    + _resourceImagesDirectoryPath
                    + imageFileName;
            var skImage = SKImage.FromEncodedData(imageFilePath);
            var skBitmap = SKBitmap.FromImage(skImage);
            { }
            return skBitmap;
        }

        protected async Task<SKBitmap?> ImageDownloadFromAsync(string imageUrl)
        {
            if (imageUrl == null
                    || imageUrl.Length < 1)
            {
                return null;
            }

            var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            if (imageBytes == null
                    || imageBytes.Length < 1)
            {
                return null;
            }

            var skImage = SKImage.FromEncodedData(imageBytes);
            var skBitmap = SKBitmap.FromImage(skImage);
            { }
            return skBitmap;
        }

        protected SKColor ColorFromArgbHex(uint hex)
        {
            return new SKColor(hex);
        }

        protected SKTypeface FontFamilyFileNamed(string fontFileName)
        {
            var fontFilePath
                    = BaoXia.Utils.Environment.ApplicationDirectoryPath
                    + "Resources/Fonts/"
                    + fontFileName;
            var skTypeface = SKTypeface.FromFile(fontFilePath);
            {
            }
            return skTypeface;
        }

        public async Task<ImageCardInfo?> Create<DataType>(
                String? imageType,
                int imageQuality,
                String? fileDownloadName,
                bool isTestDataEnable,
                Func<HttpClient, Func<string, Task<SKBitmap?>>,
                        bool,
                        Task<DataType?>> toGetDataAsync,
                Func<DataType?,
                        SKCanvas,
                        //
                        int,
                        int,
                        //
                        Padding,
                        //
                        SKTypeface,
                        SKTypeface,
                        SKTypeface,
                        //
                        float,
                        //
                        HttpClient,
                        Func<string, SKBitmap>,
                        Func<uint, SKColor>,
                        Func<string, SKTypeface?>,
                        float> toRenderCard)
        {

            var imageCardInfo = new ImageCardInfo()
            {
                ImageType = imageType,
                ImageQuality = imageQuality,
                FileDownloadName = fileDownloadName
            };


            DataType? data = default;
            var beginTimeOfGetData = DateTime.Now;
            {
                data = await toGetDataAsync(
                        _httpClient,
                        this.ImageDownloadFromAsync,
                        isTestDataEnable);
            }
            var endTimeOfGetData = DateTime.Now;
            imageCardInfo.SecondsToGetData = (endTimeOfGetData - beginTimeOfGetData).TotalSeconds;


            var beginTimeOfRenderCard = DateTime.Now;
            {
                using var canvasSurfaceContainer = _canvasSurfaces.GetObjectContainer()!;
                var canvasSurface = canvasSurfaceContainer.Item;
                var canvas = canvasSurface.Canvas;
                var cardFinalImageHeight
                        = toRenderCard != null
                        ? toRenderCard(
                                data,
                                //
                                canvasSurface.Canvas,
                                //
                                _canvasWidth,
                                _canvasHeightMax,
                                //
                                _canvasPadding,
                                //
                                _fontFamily_SourceHanSansSC_Regular,
                                _fontFamily_SourceHanSansSC_Midium,
                                _fontFamily_SourceHanSansSC_Bold,
                                //
                                _textRenderOffsetY,
                                 //
                                 _httpClient,
                                 this.ImageFileNamed,
                                 this.ColorFromArgbHex,
                                 this.FontFamilyFileNamed)
                        : 0;
                if (cardFinalImageHeight <= 0)
                {
                    return null;
                }


                var canvasBounds = canvas.BoundsI();
                var cardImageFrame = new SKRectI(
                        0,
                        0,
                        canvasBounds.Width,
                        (int)(cardFinalImageHeight * _canvasDPIZoomRatio));

                var imageCard = canvasSurface.Snapshot(cardImageFrame);
                { }
                imageCardInfo.ImageCard = imageCard;
            }
            var endTimeOfRenderCard = DateTime.Now;
            imageCardInfo.SecondsToRenderCard = (endTimeOfRenderCard - beginTimeOfRenderCard).TotalSeconds;

            return imageCardInfo;
        }

        #endregion


        ////////////////////////////////////////////////
        // @事件节点
        ////////////////////////////////////////////////

        #region 事件节点

        //protected virtual int DidRenderCardWithGraphics(
        //        Graphics graphics,
        //        //
        //        int canvasWidth,
        //        int canvasHeightMax,
        //        //
        //        Padding canvasPadding,
        //        //
        //        FontFamily fontFamily_SourceHanSansSC_Regular,
        //        FontFamily fontFamily_SourceHanSansSC_Midium,
        //        FontFamily fontFamily_SourceHanSansSC_Bold,
        //        //
        //        float textRenderOffsetY)


        #endregion
    }
}
