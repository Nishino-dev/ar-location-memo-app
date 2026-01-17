using System.Collections.Generic;
using ZXing;
using ZXing.Common;

public static class QRDecoder
{
    public static string Decode(byte[] pixels, int width, int height)
    {
        try
        {
            var reader = new BarcodeReaderGeneric();

            reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
            reader.Options.TryHarder = true;
            reader.Options.CharacterSet = "UTF-8";

            var source = new PlanarYUVLuminanceSource(pixels, width, height, 0, 0, width, height, false);

            var result = reader.Decode(source);

            return result?.Text?.Trim();
        }
        catch
        {
            return null;
        }
    }
}