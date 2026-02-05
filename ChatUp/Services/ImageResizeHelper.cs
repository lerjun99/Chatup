using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ChatUp.Services
{
    public static class ImageResizeHelper
    {
        public static async Task<string> ResizeBase64Async(
            string base64,
            int maxWidth = 700,
            int quality = 70)
        {
            var bytes = Convert.FromBase64String(base64);

            using var image = SixLabors.ImageSharp.Image.Load(bytes);

            var ratio = (double)maxWidth / image.Width;
            var height = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(maxWidth, height));

            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
            {
                Quality = quality
            });

            return Convert.ToBase64String(ms.ToArray());
        }
    }

}
