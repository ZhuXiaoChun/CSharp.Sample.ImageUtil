using SixLabors.ImageSharp;
using SkiaSharp;

namespace CSharp.Sample.ImageUtil.ViewModels
{
        public class ImageSize
        {
                public int Width { get; set; }
                public int Height { get; set; }

                public bool IsValid
                {
                        get
                        {
                                if (this.Width > 0
                                        && this.Height > 0)
                                {
                                        return true;
                                }
                                return false;
                        }
                }

                public SKSizeI ToSKSizeI()
                {
                        return new SKSizeI(
                            this.Width,
                            this.Height);
                }
                public Size ToISSize()
                {
                        return new Size(
                            this.Width,
                            this.Height);
                }
        }
}
