using System.Drawing;

namespace CSharp.Sample.ImageUtil.Extensions
{
        public static class SizeFExtension
        {
                public static Size ToSize(this SizeF size)
                {
                        return new Size(
                                (int)size.Width,
                                (int)size.Height);
                }
        }
}
