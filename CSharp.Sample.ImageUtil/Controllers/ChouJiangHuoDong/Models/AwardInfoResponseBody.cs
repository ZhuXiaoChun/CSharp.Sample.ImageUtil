using SkiaSharp;

namespace CSharp.Sample.ImageUtil.Controllers.ChouJiangHuoDong.Models
{
        public class AwardInfoResponseBody
        {
                public string? AwardHeadImageUrl { get; set; }

                public SKBitmap? AwardHeadImage { get; set; }

                public string? AwardTitle { get; set; }

                public string? AwardDescription { get; set; }

                public string? AwardEndTimeCaption { get; set; }

                public float PrizePrice { get; set; }

                public int TotalUsersCount { get; set; }

                public List<UserInfo>? UserInfes { get; set; }

                public int NumberOfWinners { get; set; }

                public WinningUser[]? WinningUsers { get; set; }
        }
}
