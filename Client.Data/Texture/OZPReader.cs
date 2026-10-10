using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Client.Data.Texture
{
    public class OZPReader : BaseReader<TextureData>
    {
        public const int MAX_WIDTH = 1024;
        public const int MAX_HEIGHT = 1024;

        protected override TextureData Read(byte[] buffer)
        {
            if (buffer.Length >= 8 && buffer[0] == 137 && buffer[1] == 'P' && buffer[2] == 'N' && buffer[3] == 'G')
            {
                // Plain PNGs have the complete signature here; OZP adds a four-byte
                // prefix before that signature. Both share this reader.
                return this.ReadPNG(buffer[4] == 13 && buffer[5] == 10 && buffer[6] == 26 && buffer[7] == 10
                    ? buffer : buffer[4..]);
            }

            throw new ApplicationException($"Invalid file format");
        }

        private TextureData ReadPNG(byte[] buffer)
        {
            using var image = Image.Load<Rgba32>(buffer);

            int width = image.Width;
            int height = image.Height;

            var data = new byte[width * height * 4];
            image.CopyPixelDataTo(data);

            return new TextureData
            {
                Width = width,
                Height = height,
                Components = 4,
                Data = data,
                IsCompressed = false,
                Format = TextureSurfaceFormat.Color
            };
        }
    }
}
