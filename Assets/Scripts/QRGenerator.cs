using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing.QrCode;
using System.Collections;
using System.Collections.Generic;

public class QRGenerator : MonoBehaviour
{
    [Header("UI設定")]
    public TMP_InputField inputField;
    public RawImage qrDisplayImage;
    public Button saveButton;
    public GameObject messageUI;

    [Header("カラーコード入力用UI")]
    public TMP_InputField fontColorInput;
    public TMP_InputField backgroundColorInput;
    public Image fontColorPreview;
    public Image backgroundColorPreview;

    [Header("サイズ設定UI")]
    public Slider sizeSlider;
    public TMP_Text sizeValueText;

    private string currentFontColor = "#FFFFFF";
    private string currentBackgroundColor = "#000000";
    private float currentMemoScale = 1.0f;
    private Texture2D generatedTexture;

    void Start()
    {
        if (saveButton != null) saveButton.interactable = false;
        if (messageUI != null) messageUI.SetActive(false);

        if (fontColorInput != null) fontColorInput.onValueChanged.AddListener(OnFontColorChanged);
        if (backgroundColorInput != null) backgroundColorInput.onValueChanged.AddListener(OnBackgroundColorChanged);

        if (sizeSlider != null)
        {
            sizeSlider.minValue = 0.1f;
            sizeSlider.maxValue = 2.0f;
            sizeSlider.value = currentMemoScale;
            sizeSlider.onValueChanged.AddListener((val) => {
                currentMemoScale = val;
                if (sizeValueText != null) sizeValueText.text = val.ToString("F1");
            });
        }

        OnFontColorChanged(currentFontColor);
        OnBackgroundColorChanged(currentBackgroundColor);
    }

    private void OnFontColorChanged(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            currentFontColor = hex;
            if (fontColorPreview != null) fontColorPreview.color = color;
        }
    }

    private void OnBackgroundColorChanged(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            currentBackgroundColor = hex;
            if (backgroundColorPreview != null) backgroundColorPreview.color = color;
        }
    }

    public void OnGenerateButtonClicked()
    {
        string text = inputField.text;
        if (string.IsNullOrEmpty(text)) return;

        MemoData data = new MemoData
        {
            v = 1,
            txt = text,
            fc = currentFontColor,
            bc = currentBackgroundColor,
            sz = currentMemoScale
        };

        string jsonPayload = JsonUtility.ToJson(data);
        GenerateQR(jsonPayload);
    }

    private void GenerateQR(string payload)
    {
        int width = 512;
        int height = 512;
        var writer = new QRCodeWriter();
        var hints = new Dictionary<ZXing.EncodeHintType, object>
        {
            { ZXing.EncodeHintType.CHARACTER_SET, "UTF-8" }
        };
        var bitMatrix = writer.encode(payload, ZXing.BarcodeFormat.QR_CODE, width, height, hints);

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
        if (saveButton != null) saveButton.interactable = true;
    }

    public void OnSaveButtonClicked()
    {
        if (generatedTexture == null) return;
        NativeGallery.SaveImageToGallery(generatedTexture, "MyARApp", "QRCode_{0}.png");
        if (messageUI != null) StartCoroutine(ShowMessageRoutine());
    }

    IEnumerator ShowMessageRoutine()
    {
        messageUI.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        messageUI.SetActive(false);
    }
}