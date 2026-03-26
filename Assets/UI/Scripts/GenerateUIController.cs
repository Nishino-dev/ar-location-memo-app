using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class GenerateUIController : MonoBehaviour
{
    public QRGenerator qrGenerator;

    private VisualElement qrFrame;
    private TextField contentField;
    private TextField fontColorField;
    private TextField backgroundColorField;
    private VisualElement fontPreview;
    private VisualElement backPreview;
    private Button generateBtn;
    private Button saveBtn;
    private Button backBtn;
    private VisualElement messageUI;

    private Texture2D currentTexture;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        qrFrame = root.Q<VisualElement>("QRFrame");
        contentField = root.Q<TextField>("ContentInput");
        fontColorField = root.Q<TextField>("FontColorInput");
        backgroundColorField = root.Q<TextField>("BackgroundColorInput");
        fontPreview = root.Q<VisualElement>("FontColorPreview");
        backPreview = root.Q<VisualElement>("BackgroundColorPreview");
        generateBtn = root.Q<Button>("GenerateButton");
        saveBtn = root.Q<Button>("SaveButton");
        backBtn = root.Q<Button>("BackButton");
        messageUI = root.Q<VisualElement>("MessageUI");

        if (saveBtn != null) saveBtn.SetEnabled(false);
        if (messageUI != null) messageUI.style.display = DisplayStyle.None;

        generateBtn.clicked += OnGenerateClicked;
        saveBtn.clicked += OnSaveClicked;
        backBtn.clicked += () => SceneManager.LoadScene("MenuScene");

        fontColorField.RegisterValueChangedCallback(evt => UpdateColor(fontPreview, evt.newValue));
        backgroundColorField.RegisterValueChangedCallback(evt => UpdateColor(backPreview, evt.newValue));
    }

    private void UpdateColor(VisualElement preview, string hex)
    {
        if (preview != null && ColorUtility.TryParseHtmlString(hex, out Color col))
            preview.style.backgroundColor = col;
    }

    private void OnGenerateClicked()
    {
        currentTexture = qrGenerator.Generate(contentField.value, fontColorField.value, backgroundColorField.value);
        if (currentTexture != null)
        {
            qrFrame.style.backgroundImage = new StyleBackground(currentTexture);
            saveBtn.SetEnabled(true);
        }
    }

    private void OnSaveClicked()
    {
        if (currentTexture == null) return;
        NativeGallery.SaveImageToGallery(currentTexture, "MyARApp", "QRCode_{0}.png");
        StartCoroutine(ShowMessage());
    }

    IEnumerator ShowMessage()
    {
        messageUI.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(2.0f);
        messageUI.style.display = DisplayStyle.None;
    }
}