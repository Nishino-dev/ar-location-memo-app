using System.Collections.Generic;
using ZXing;
using ZXing.Common;

public static class QRDecoder
{
    public static string Decode(byte[] pixels, int width, int height)
    {
        try
        {
            MultiFormatReader reader = new MultiFormatReader();
            var hints = new Dictionary<DecodeHintType, object>
            {
                { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } },
                { DecodeHintType.TRY_HARDER, true }
            };
            reader.Hints = hints;

            var source = new PlanarYUVLuminanceSource(pixels, width, height, 0, 0, width, height, false);
            var binarizer = new HybridBinarizer(source);
            var binaryBitmap = new BinaryBitmap(binarizer);
            var result = reader.decode(binaryBitmap);

            return result?.Text;
        }
        catch
        {
            return null;
        }
    }
}