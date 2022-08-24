using BaoXia.Constants;
using CSharp.Sample.ImageUtil.Attributes;
using CSharp.Sample.ImageUtil.Controllers.ChouJiangHuoDong.Models;
using CSharp.Sample.ImageUtil.Extensions;
using CSharp.Sample.ImageUtil.LogFiles;
using CSharp.Sample.ImageUtil.Utils;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace CSharp.Sample.ImageUtil.Controllers.ChouJiangHuoDong
{
        [Route("/chouJiangHuoDong/shareCard_{__openAwardId__}.{__imageType__}")]
        [AuthorizationNotRequired]
        public class ShareCardController : Controller
        {

                ////////////////////////////////////////////////
                // @自身属性
                ////////////////////////////////////////////////

                #region 自身属性

                static ImageCard _shareImageCard = new("Resources/ShareCardFor_ChouJiangHuoDong/");

                #endregion


                ////////////////////////////////////////////////
                // @自身实现
                ////////////////////////////////////////////////

                #region 自身实现

                public async Task<IActionResult> ShareCard(
                        [FromQuery] int openAwardId,
                        [FromQuery] string? imageType = null,
                        [FromQuery] int imageQuality = 100,
                        [FromQuery] string? fileDownloadName = null,
                        [FromQuery] bool isTestDataEnable = false)
                {
                        var response = new ViewModels.Response();
                        try
                        {
                                if (openAwardId == 0)
                                {
                                        var imageTypeInPath = this.RouteData.Values["__openAwardId__"] as string;
                                        _ = int.TryParse(imageTypeInPath, out openAwardId);
                                }
                                if (imageType == null
                                        || imageType.Length < 1)
                                {
                                        imageType = this.RouteData.Values["__imageType__"] as string;
                                }

                                var imageCardInfo = await _shareImageCard.Create<Models.AwardInfoResponseBody>(
                                        imageType,
                                        imageQuality,
                                        fileDownloadName,
                                        isTestDataEnable,
                                        async (httpClient,
                                        imageDownloadFromAsync,
                                        isTestDataEnable) =>
                                        {
                                                // 通过网络或其他信息，获取奖项信息（当前样例中使用了测试数据）：
                                                // 开启测试数据：
                                                isTestDataEnable = true;
                                                AwardInfoResponseBody? openAwardInfo =  new AwardInfoResponseBody();
                                                if (openAwardInfo != null
                                                && isTestDataEnable)
                                                {
                                                        openAwardInfo.AwardHeadImageUrl = "https://ts1.cn.mm.bing.net/th/id/R-C.45daaa3efd006630a5451daa78c69e58?rik=11%2bTyULCvov02Q&riu=http%3a%2f%2f5b0988e595225.cdn.sohucs.com%2fimages%2f20180102%2f828473c1a1b248c9af93c4cf047f9899.jpeg&ehk=eCB%2bEDwyrF5DmlwemhUa07mZWu4om1kZ0eTEa7ODao8%3d&risl=&pid=ImgRaw&r=0";
                                                        openAwardInfo.AwardTitle = "欢迎参与C#开源项目";
                                                        openAwardInfo.PrizePrice = 68;
                                                        openAwardInfo.AwardDescription = "这是一个基于 SkiaSharp，ImageSharp 开源项目的C# 创建分享卡片，管理图片上传的样例程序。";
                                                        openAwardInfo.AwardEndTimeCaption = "2小时3分钟后截止";
                                                        openAwardInfo.TotalUsersCount = 1024;
                                                }

                                                if (openAwardInfo != null)
                                                {
                                                        var tasksToDownloadImage = new List<Task>();

                                                        var AwardHeadImageUrl = openAwardInfo.AwardHeadImageUrl;
                                                        if (AwardHeadImageUrl?.Length > 0)
                                                        {
                                                                tasksToDownloadImage.Add(Task.Run(async () =>
                                                                {
                                                                        openAwardInfo.AwardHeadImage = await imageDownloadFromAsync(AwardHeadImageUrl);
                                                                }));
                                                        }

                                                        var userInfes = new List<UserInfo>();
                                                        // 测试数据：
                                                        for (var userIndex = 0;
                                                        userIndex < 3;
                                                        userIndex++)
                                                        {
                                                                userInfes.Add(new()
                                                                {
                                                                        AwardHeadImageUrl = "https://tse3-mm.cn.bing.net/th/id/OIP-C.q1zQe5qWtN7w5n9ngVvIIgAAAA?pid=ImgDet&rs=1"
                                                                });
                                                        }
                                                        openAwardInfo.UserInfes = userInfes;

                                                        foreach (var userInfo in userInfes)
                                                        {
                                                                var userAwardHeadImageUrl = userInfo.AwardHeadImageUrl;
                                                                if (userAwardHeadImageUrl?.Length > 0)
                                                                {
                                                                        tasksToDownloadImage.Add(Task.Run(async () =>
                                                                        {
                                                                                userInfo.AwardHeadImage = await imageDownloadFromAsync(userAwardHeadImageUrl);
                                                                        }));
                                                                }
                                                        }

                                                        // !!!
                                                        await Task.WhenAll(tasksToDownloadImage.ToArray());
                                                        // !!!
                                                }
                                                return openAwardInfo;
                                        },
                                        (Models.AwardInfoResponseBody? openAwardInfo,
                                                SKCanvas canvas,
                                                //
                                                int canvasWidth,
                                                int canvasHeightMax,
                                                //
                                                Padding canvasPadding,
                                                //
                                                SKTypeface fontFamily_SourceHanSansSC_Regular,
                                                SKTypeface fontFamily_SourceHanSansSC_Midium,
                                                SKTypeface fontFamily_SourceHanSansSC_Bold,
                                                //
                                                float textRenderOffsetY,
                                                //
                                                HttpClient httpClient,
                                                Func<string, SKBitmap> imageFileNamed,
                                                Func<uint, SKColor> colorFromArgbHex,
                                                Func<string, SKTypeface?> fontFamilyFileNamed) =>
                                        {
                                                ////////////////////////////////////////////////
                                                // 获取活动信息
                                                ////////////////////////////////////////////////
                                                if (openAwardInfo == null)
                                                {
                                                        return 0;
                                                }

                                                var SKPaintDefault = new SKPaint()
                                                {
                                                        IsAntialias = true,
                                                        FilterQuality = SKFilterQuality.High,
                                                        HintingLevel = SKPaintHinting.Normal,
                                                        Typeface = fontFamily_SourceHanSansSC_Regular
                                                };

                                                ////////////////////////////////////////////////
                                                // 初始化画布。
                                                ////////////////////////////////////////////////
                                                canvas.Clear(SKColors.White);

                                                ////////////////////////////////////////////////
                                                // 绘制：页头。
                                                ////////////////////////////////////////////////
                                                {
                                                        if (openAwardInfo.AwardHeadImage != null)
                                                        {
                                                                var pageAwardHeadImage = openAwardInfo.AwardHeadImage;
                                                                if (pageAwardHeadImage != null)
                                                                {
                                                                        var pageAwardHeadImageFrame
                                                                        = SKRect.Create(0, 0, canvasWidth, 210);
                                                                        canvas.DrawBitmap(
                                                                                pageAwardHeadImage,
                                                                                pageAwardHeadImageFrame,
                                                                                SKPaintDefault);
                                                                }
                                                        }

                                                        var button_JinBiChouJiang_120x28
                                                        = imageFileNamed("button_JinBiChouJiang_120x28@3x.png");
                                                        canvas.DrawBitmap(
                                                                button_JinBiChouJiang_120x28,
                                                                SKRect.Create(0, 0, button_JinBiChouJiang_120x28.Width, button_JinBiChouJiang_120x28.Height),
                                                                SKRect.Create(canvasPadding.Left, 174, 120, 28),
                                                                SKPaintDefault);
                                                }

                                                ////////////////////////////////////////////////
                                                // 绘制：活动金额信息。
                                                ////////////////////////////////////////////////
                                                SKRect openAwardInfoSumInfoFrame
                                                = SKRect.Create(
                                                        canvasWidth - canvasPadding.Right,
                                                        230,
                                                        0,
                                                        18);
                                                {
                                                        var text = "￥" + openAwardInfo.PrizePrice.ToString("F0");
                                                        if (text?.Length > 0)
                                                        {
                                                                var skPaint = SKPaintDefault.Clone();
                                                                {
                                                                        skPaint.Style = SKPaintStyle.Fill;
                                                                        skPaint.TextSize = 8;
                                                                        skPaint.Color = colorFromArgbHex(0xFFFFFFFD);
                                                                }
                                                                SKRect textBounds = new();
                                                                skPaint.MeasureText(text, ref textBounds);
                                                                {
                                                                        var openAwardInfoSumInfoFrameWidth
                                                                        = textBounds.Width + 12;

                                                                        openAwardInfoSumInfoFrame.Right
                                                                        = canvasWidth
                                                                        - canvasPadding.Right;
                                                                        openAwardInfoSumInfoFrame.Left
                                                                        = openAwardInfoSumInfoFrame.Right
                                                                        - openAwardInfoSumInfoFrameWidth;
                                                                }
                                                                var textBoxRoundFrame = new SKRoundRect(
                                                                        openAwardInfoSumInfoFrame,
                                                                        4);
                                                                var textBoxPaint = new SKPaint()
                                                                {
                                                                        Style = SKPaintStyle.Fill,
                                                                        Color = colorFromArgbHex(0x4D000000),
                                                                        IsAntialias = true,
                                                                };
                                                                canvas.DrawRoundRect(textBoxRoundFrame, textBoxPaint);

                                                                var textPosition = new SKPoint(
                                                                        (float)Math.Floor(openAwardInfoSumInfoFrame.MidX - textBounds.Width / 2),
                                                                        (float)Math.Floor(openAwardInfoSumInfoFrame.MidY - textBounds.Height / 2)
                                                                                + textRenderOffsetY);
                                                                canvas.DrawTextToLeftTop(
                                                                        text,
                                                                        textPosition,
                                                                        skPaint);

                                                                var throughLineFrame = SKRect.Create(
                                                                        textPosition.X,
                                                                        textPosition.Y + textBounds.Height / 2 + 1.0F,
                                                                        textBounds.Width,
                                                                        0.5F);
                                                                {
                                                                        canvas.DrawRect(
                                                                                throughLineFrame,
                                                                                skPaint);
                                                                }
                                                        }
                                                }

                                                ////////////////////////////////////////////////
                                                // 绘制：活动标题。
                                                ////////////////////////////////////////////////
                                                float drawOffsetYByTitleLines = 0.0F;
                                                {
                                                        var text = openAwardInfo.AwardTitle;
                                                        if (text?.Length > 0)
                                                        {
                                                                //if (text.Length >= 16)
                                                                //{
                                                                //        text = text[..16] + "...";
                                                                //}
                                                                // text = text + text;

                                                                var skPaint = SKPaintDefault.Clone();
                                                                {
                                                                        skPaint.Typeface = fontFamily_SourceHanSansSC_Bold;
                                                                        skPaint.TextSize = 18;
                                                                        skPaint.Color = colorFromArgbHex(0xFF222222);
                                                                }
                                                                var titleFrame = new SKRect(
                                                                        canvasPadding.Left,
                                                                        230 + textRenderOffsetY,
                                                                        openAwardInfoSumInfoFrame.Left - 32,
                                                                        canvasHeightMax);
                                                                var titleLineHeight = 24.0F;
                                                                titleFrame = canvas.DrawTextInRect(
                                                                        text,
                                                                        titleFrame,
                                                                        titleLineHeight,
                                                                        SKTextAlign.Left,
                                                                        SKTextAlign.Left,
                                                                        skPaint);
                                                                // !!!
                                                                drawOffsetYByTitleLines
                                                                = titleFrame.Size.Height
                                                                - titleLineHeight;
                                                                // !!!
                                                        }
                                                }
                                                // !!!
                                                canvas.Translate(0.0F, drawOffsetYByTitleLines);
                                                // !!!



                                                //////////////////////////////////////////////////
                                                //// 绘制：参与用户头像。
                                                //////////////////////////////////////////////////
                                                SKRect lastUserAwardHeadImageFrame;
                                                int userAwardHeadImagesCount = 0;
                                                {
                                                        var userAwardHeadImagePaint_Fill = new SKPaint()
                                                        {
                                                                IsAntialias = true,
                                                                Style = SKPaintStyle.Fill,
                                                                Color = SKColors.LightGray,
                                                                StrokeWidth = 1.0F
                                                        };
                                                        var userAwardHeadImagePaint_Stroke = new SKPaint()
                                                        {
                                                                IsAntialias = true,
                                                                Style = SKPaintStyle.Stroke,
                                                                Color = SKColors.White,
                                                                StrokeWidth = 1.0F,
                                                        };

                                                        var userAwardHeadImageOffset = 16;
                                                        var userAwardHeadImageFrameRadius = 12.0F;
                                                        lastUserAwardHeadImageFrame = SKRect.Create(
                                                                canvasPadding.Left - userAwardHeadImageOffset,
                                                                263,
                                                                userAwardHeadImageFrameRadius * 2,
                                                                userAwardHeadImageFrameRadius * 2);
                                                        SKRect userAwardHeadImageBorderFrame = new(
                                                                lastUserAwardHeadImageFrame.Left,
                                                                lastUserAwardHeadImageFrame.Top,
                                                                lastUserAwardHeadImageFrame.Right,
                                                                lastUserAwardHeadImageFrame.Bottom);
                                                        {
                                                                userAwardHeadImageBorderFrame.Inflate(0.0F, 0.0F);
                                                        }
                                                        var userInfes = openAwardInfo.UserInfes;
                                                        if (userInfes?.Any() == true)
                                                        {
                                                                var userAwardHeadImagesCountMax = 3;

                                                                // !!!
                                                                userAwardHeadImagesCount = userInfes.Count;
                                                                if (userAwardHeadImagesCount > userAwardHeadImagesCountMax)
                                                                {
                                                                        userAwardHeadImagesCount = userAwardHeadImagesCountMax;
                                                                }
                                                                // !!!

                                                                for (var userAwardHeadImageIndex = 0;
                                                                        userAwardHeadImageIndex < userAwardHeadImagesCount;
                                                                        userAwardHeadImageIndex++)
                                                                {
                                                                        // !!!
                                                                        lastUserAwardHeadImageFrame.Offset(userAwardHeadImageOffset, 0);
                                                                        userAwardHeadImageBorderFrame.Offset(userAwardHeadImageOffset, 0);
                                                                        // !!!

                                                                        canvas.DrawCircle(
                                                                                lastUserAwardHeadImageFrame.MidX,
                                                                                lastUserAwardHeadImageFrame.MidY,
                                                                                userAwardHeadImageFrameRadius,
                                                                                userAwardHeadImagePaint_Fill);

                                                                        var userAwardHeadImage = userInfes[userAwardHeadImageIndex].AwardHeadImage;
                                                                        if (userAwardHeadImage != null)
                                                                        {
                                                                                canvas.Save();
                                                                                {
                                                                                        var clipPath = new SKPath();
                                                                                        {
                                                                                                clipPath.AddCircle(
                                                                                                        lastUserAwardHeadImageFrame.MidX,
                                                                                                        lastUserAwardHeadImageFrame.MidY,
                                                                                                        userAwardHeadImageFrameRadius);
                                                                                        }
                                                                                        canvas.ClipPath(
                                                                                                clipPath,
                                                                                                SKClipOperation.Intersect,
                                                                                                true);
                                                                                        // !!!
                                                                                        canvas.DrawBitmap(
                                                                                                userAwardHeadImage,
                                                                                                lastUserAwardHeadImageFrame);
                                                                                        // !!!
                                                                                }
                                                                                canvas.Restore();
                                                                        }
                                                                        canvas.DrawCircle(
                                                                                userAwardHeadImageBorderFrame.MidX,
                                                                                userAwardHeadImageBorderFrame.MidY,
                                                                                userAwardHeadImageBorderFrame.Width / 2,
                                                                                userAwardHeadImagePaint_Stroke);
                                                                }
                                                        }
                                                }

                                                //////////////////////////////////////////////////
                                                //// 绘制：参与人数信息。
                                                //////////////////////////////////////////////////
                                                {
                                                        var text = "共" + openAwardInfo.TotalUsersCount + "人参与";
                                                        var skPaint = SKPaintDefault.Clone();
                                                        {
                                                                skPaint.Typeface = fontFamily_SourceHanSansSC_Midium;
                                                                skPaint.TextSize = 14;
                                                                skPaint.Color = colorFromArgbHex(0xFF666666);
                                                        }
                                                        SKRect textBounds = new();
                                                        skPaint.MeasureText(text, ref textBounds);
                                                        var textPosition =
                                                        new SKPoint(
                                                                userAwardHeadImagesCount > 0 ? lastUserAwardHeadImageFrame.Right + 8 : canvasPadding.Left,
                                                                lastUserAwardHeadImageFrame.MidY - textBounds.Height / 2 + textRenderOffsetY);

                                                        canvas.DrawTextToLeftTop(
                                                                text,
                                                                textPosition,
                                                                skPaint);
                                                }

                                                //////////////////////////////////////////////////
                                                //// 绘制：剩余时间。
                                                //////////////////////////////////////////////////
                                                {
                                                        var text = openAwardInfo.AwardEndTimeCaption;
                                                        if (text?.Length > 0)
                                                        {
                                                                var skPaint = SKPaintDefault.Clone();
                                                                {
                                                                        skPaint.Typeface = fontFamily_SourceHanSansSC_Midium;
                                                                        skPaint.TextSize = 12;
                                                                        skPaint.Color = colorFromArgbHex(0xFFEB413D);
                                                                }
                                                                var textBounds = new SKRect();
                                                                skPaint.MeasureText(text, ref textBounds);
                                                                var textPosition = new SKPoint(
                                                                        canvasWidth - canvasPadding.Right - textBounds.Width,
                                                                        lastUserAwardHeadImageFrame.MidY - textBounds.Height / 2 + textRenderOffsetY);

                                                                canvas.DrawTextToLeftTop(
                                                                        text,
                                                                        textPosition,
                                                                        skPaint);
                                                        }
                                                }


                                                //////////////////////////////////////////////////
                                                //// 绘制：水平分隔线。
                                                //////////////////////////////////////////////////
                                                {
                                                        var skPaint = SKPaintDefault.Clone();
                                                        {
                                                                skPaint.IsStroke = false;
                                                                skPaint.Color = colorFromArgbHex(0xFFEEEEEE);
                                                        }

                                                        var lineFrame = SKRect.Create(
                                                                canvasPadding.Left,
                                                                303,
                                                                canvasWidth - canvasPadding.Width,
                                                                1);
                                                        canvas.DrawRect(
                                                                lineFrame,
                                                                skPaint);
                                                }


                                                //////////////////////////////////////////////////
                                                //// 绘制：描述信息。
                                                //////////////////////////////////////////////////
                                                SKRect descriptTextFrame = SKRect.Create(
                                                                        canvasPadding.Left,
                                                                        317,
                                                                        canvasWidth - canvasPadding.Width,
                                                                        canvasHeightMax);
                                                {
                                                        descriptTextFrame.Bottom
                                                        = descriptTextFrame.Top;
                                                }
                                                {
                                                        var text = openAwardInfo.AwardDescription;
                                                        if (text?.Length > 0)
                                                        {
                                                                var skPaint = SKPaintDefault.Clone();
                                                                {
                                                                        skPaint.Typeface = fontFamily_SourceHanSansSC_Midium;
                                                                        skPaint.HintingLevel = SKPaintHinting.NoHinting;
                                                                        skPaint.TextSize = 13;
                                                                        skPaint.Color = colorFromArgbHex(0xFF000000);
                                                                }

                                                                descriptTextFrame = canvas.DrawTextInRect(
                                                                        text,
                                                                        descriptTextFrame,
                                                                        22.0F,
                                                                        SKTextAlign.Left,
                                                                        SKTextAlign.Left,
                                                                        skPaint);
                                                        }
                                                }


                                                //////////////////////////////////////////////////
                                                //// 绘制：绘制页脚。
                                                //////////////////////////////////////////////////
                                                SKRect pageFooterFrame = SKRect.Create(
                                                        0,
                                                        descriptTextFrame.Bottom + 20,
                                                        canvasWidth,
                                                        78);
                                                {
                                                        var image_PageFooter_375x78
                                                        = imageFileNamed("image_PageFooter_375x78@3x.png");
                                                        if (pageFooterFrame.Bottom > canvasHeightMax)
                                                        {
                                                                pageFooterFrame.Top = canvasHeightMax - pageFooterFrame.Height;
                                                        }


                                                        canvas.Save();
                                                        {
                                                                canvas.ClipRect(pageFooterFrame);
                                                                canvas.Clear();

                                                                canvas.DrawBitmap(
                                                                        image_PageFooter_375x78,
                                                                        pageFooterFrame,
                                                                        SKPaintDefault);
                                                        }
                                                        canvas.Restore();
                                                }

                                                canvas.Translate(0, -drawOffsetYByTitleLines);
                                                return pageFooterFrame.Bottom + drawOffsetYByTitleLines;
                                        });
                                if (imageCardInfo?.TryEndResponse(
                                        this.Response,
                                        out var actionResult) == true)
                                {
                                        return actionResult;
                                }
                                response.Error = Error.InvalidRequest;
                        }
                        catch (Exception exception)
                        {
                                Log.Exception.Logs(this, "创建分享卡片失败，程序异常。", exception);
                                //
                                response.Error = Error.ProgramError;
                        }
                        return Json(response);
                }

                #endregion
        }
}
