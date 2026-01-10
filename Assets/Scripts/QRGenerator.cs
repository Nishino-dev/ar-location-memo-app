using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

public class QRGenerator : MonoBehaviour
{
    [Header("UIê›íË")]
    public TMP_InputField inputField;
    public RawImage qrDisplayImage;

    public void OnGenerateButtonClicked()
    {
        string text = inputField.text;

        if (string.IsNullOrEmpty(text)) return;

        int width = 256;
        int height = 256;

        var writer = new QRCodeWriter();
        BitMatrix bitMatrix = writer.encode(text, BarcodeFormat.QR_CODE, width, height);

        Texture2D texture = new Texture2D(width, height);
        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (bitMatrix[x, y])
                {
                    pixels[y * width + x] = Color.black;
                }
                else
                {
                    pixels[y * width + x] = Color.white;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        qrDisplayImage.texture = texture;
    }
}