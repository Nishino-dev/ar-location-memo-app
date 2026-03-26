using UnityEngine;
using System.Collections.Generic;
using ZXing;
using ZXing.QrCode;

public class QRGenerator : MonoBehaviour
{
    public Texture2D Generate(string text, string fontColor, string backgroundColor)
    {
        if (string.IsNullOrEmpty(text)) return null;

        MemoData data = new MemoData
        {
            v = 1,
            txt = text,
            fc = string.IsNullOrEmpty(fontColor) ? "#FFFFFF" : fontColor,
            bc = string.IsNullOrEmpty(backgroundColor) ? "#000000" : backgroundColor
        };

        string payload = JsonUtility.ToJson(data);
        return CreateQRTexture(payload);
    }

    private Texture2D CreateQRTexture(string payload)
    {
        int width = 256;
        int height = 256;

        var writer = new QRCodeWriter();
        var hints = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.CHARACTER_SET, "UTF-8" },
            { EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.M },
            { EncodeHintType.MARGIN, 1 }
        };

        try
        {
            var bitMatrix = writer.encode(payload, BarcodeFormat.QR_CODE, width, height, hints);
            Texture2D texture = new Texture2D(width, height);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = bitMatrix[x, height - 1 - y] ? Color.black : Color.white;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return null;
        }
    }
}