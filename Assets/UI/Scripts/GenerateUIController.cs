using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

        var historyBtn = root.Q<Button>("HistoryButton");

        if (saveBtn != null) saveBtn.SetEnabled(false);
        if (messageUI != null) messageUI.style.display = DisplayStyle.None;

        generateBtn.clicked += OnGenerateClicked;
        saveBtn.clicked += OnSaveClicked;
        backBtn.clicked += () => SceneManager.LoadScene("MenuScene");

        if (historyBtn != null)
            historyBtn.clicked += () => SceneManager.LoadScene("HistoryScene");

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
        SaveToHistory();
        StartCoroutine(ShowMessage());
    }

    private void SaveToHistory()
    {
        MemoData newData = new MemoData
        {
            v = 1,
            txt = contentField.value,
            fc = fontColorField.value,
            bc = backgroundColorField.value
        };

        string json = PlayerPrefs.GetString("QR_HISTORY", "{\"memoList\":[]}");
        HistoryWrapper history = JsonUtility.FromJson<HistoryWrapper>(json);

        history.memoList.Insert(0, newData);

        if (history.memoList.Count > 50)
            history.memoList.RemoveAt(history.memoList.Count - 1);

        PlayerPrefs.SetString("QR_HISTORY", JsonUtility.ToJson(history));
        PlayerPrefs.Save();
    }

    IEnumerator ShowMessage()
    {
        messageUI.style.display = DisplayStyle.Flex;
        yield return new WaitForSeconds(2.0f);
        messageUI.style.display = DisplayStyle.None;
    }
}