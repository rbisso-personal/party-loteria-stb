using UnityEngine;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

namespace PartyLoteria.Utils
{
    /// <summary>
    /// QR Code generator for Unity using ZXing.Net library.
    /// Generates scannable QR codes with proper error correction.
    /// </summary>
    public static class QRCodeGenerator
    {
        private const int DEFAULT_QUIET_ZONE = 4;

        /// <summary>
        /// Generates a QR code texture from the given data string.
        /// </summary>
        /// <param name="data">The data to encode (URL or text)</param>
        /// <param name="pixelsPerModule">Size of each module in pixels</param>
        /// <param name="foregroundColor">Color of the dark modules</param>
        /// <param name="backgroundColor">Color of the light modules</param>
        /// <returns>A Texture2D containing the QR code</returns>
        public static Texture2D Generate(string data, int pixelsPerModule = 10,
            Color? foregroundColor = null, Color? backgroundColor = null)
        {
            Color fg = foregroundColor ?? Color.black;
            Color bg = backgroundColor ?? Color.white;

            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Margin = DEFAULT_QUIET_ZONE,
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M
                }
            };

            BitMatrix matrix = writer.Encode(data);
            int width = matrix.Width * pixelsPerModule;
            int height = matrix.Height * pixelsPerModule;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < matrix.Height; y++)
            {
                for (int x = 0; x < matrix.Width; x++)
                {
                    Color moduleColor = matrix[x, y] ? fg : bg;

                    int baseX = x * pixelsPerModule;
                    int baseY = (matrix.Height - 1 - y) * pixelsPerModule;

                    for (int py = 0; py < pixelsPerModule; py++)
                    {
                        for (int px = 0; px < pixelsPerModule; px++)
                        {
                            int index = (baseY + py) * width + (baseX + px);
                            pixels[index] = moduleColor;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Debug.Log($"[QRCode] Generated {matrix.Width}x{matrix.Height} QR code ({width}x{height}px) for: {data}");

            return texture;
        }
    }
}
