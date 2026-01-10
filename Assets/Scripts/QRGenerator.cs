using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing.QrCode;
using System.Collections;

public class QRGenerator : MonoBehaviour
{
    [Header("UIê›íË")]
    public TMP_InputField inputField;
    public RawImage qrDisplayImage;
    public Button saveButton;
    public GameObject messageUI;

    private Texture2D generatedTexture;

    void Start()
    {
        if (saveButton != null)
        {
            saveButton.interactable = false;
        }

        if (messageUI != null) messageUI.SetActive(false);
    }

    public void OnGenerateButtonClicked()
    {
        string text = inputField.text;

        if (string.IsNullOrEmpty(text)) return;

        int width = 512;
        int height = 512;

        var writer = new QRCodeWriter();
        var bitMatrix = writer.encode(text, ZXing.BarcodeFormat.QR_CODE, width, height);

        generatedTexture = new Texture2D(width, height);
        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = bitMatrix[x, y] ? Color.black : Color.white;
            }
        }

        generatedTexture.SetPixels32(pixels);
        generatedTexture.Apply();

        qrDisplayImage.texture = generatedTexture;

        if (saveButton != null)
        {
            saveButton.interactable = true;
        }
    }

    public void OnSaveButtonClicked()
    {
        if (generatedTexture == null) return;

        NativeGallery.SaveImageToGallery(generatedTexture, "MyARApp", "QRCode_{0}.png");

        Debug.Log("ï€ë∂èàóùÇé¿çsÇµÇ‹ÇµÇΩ");

        if (messageUI != null)
        {
            StartCoroutine(ShowMessageRoutine());
        }
    }

    IEnumerator ShowMessageRoutine()
    {
        messageUI.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        messageUI.SetActive(false);
    }
}