using System.Drawing;

namespace CSharp.Sample.ImageUtil.Extensions
{
        public static class SizeExtension
        {
                public static SizeF ToSizeF(this Size size)
                {
                        return new SizeF(
                                size.Width,
                                size.Height);
                }
        }
}
