using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ChatUp.Application.Common.Helpers
{
    public static class ImageCompressionHelper
    {
        public static string CreateThumbnail(
            string base64,
            int maxWidth = 300,
            int quality = 70)
        {
            var bytes = Convert.FromBase64String(base64);

            using var image = ImageSharpImage.Load(bytes);

            var ratio = (double)maxWidth / image.Width;
            var height = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(maxWidth, height));

            using var ms = new MemoryStream();
            image.Save(ms, new JpegEncoder { Quality = quality });

            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
