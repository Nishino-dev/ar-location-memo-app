using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GenerateUIController : MonoBehaviour
{
    public QRGenerator qrGenerator;
    [SerializeField] private ColorPickerController colorPicker;

    private VisualElement qrFrame;
    private TextField contentField;
    private VisualElement fontPreview;
    private VisualElement backPreview;
    private VisualElement memoPreview;
    private Label memoText;
    private Button generateBtn;
    private Button saveBtn;
    private Button backBtn;
    private VisualElement messageUI;

    private Texture2D currentTexture;
    private VisualElement _activeDot;

    private void OnEnable()
    {
        
        var root = GetComponent<UIDocument>().rootVisualElement;

        qrFrame = root.Q<VisualElement>("QRFrame");
        contentField = root.Q<TextField>("ContentInput");
        fontPreview = root.Q<VisualElement>("FontColorDot");
        backPreview = root.Q<VisualElement>("BackColorDot");
        memoPreview = root.Q<VisualElement>("MemoPreview");
        memoText = root.Q<Label>("MemoText");
        generateBtn = root.Q<Button>("GenerateButton");
        saveBtn = root.Q<Button>("SaveButton");
        backBtn = root.Q<Button>("BackButton");
        messageUI = root.Q<VisualElement>("MessageUI");

        if (fontPreview != null)
        {
            fontPreview.style.backgroundColor = Color.black;
        }

        if (backPreview != null)
        {
            Color defaultBackColor;
            if (ColorUtility.TryParseHtmlString("#FFF3A9", out defaultBackColor))
            {
                backPreview.style.backgroundColor = defaultBackColor;
            }
        }

        UpdateMemoPreview();

        var historyBtn = root.Q<Button>("HistoryButton");

        if (colorPicker != null)
        {
            colorPicker.Setup(root);
            colorPicker.OnColorConfirmed = (confirmedColor) => {
                if (_activeDot != null)
                {
                    _activeDot.style.backgroundColor = confirmedColor;
                    UpdateMemoPreview();
                }
            };
        }

        if (fontPreview != null)
        {
            fontPreview.RegisterCallback<ClickEvent>(evt => {
                _activeDot = fontPreview;
                colorPicker.Open(fontPreview.style.backgroundColor.value);
            });
        }

        if (backPreview != null)
        {
            backPreview.RegisterCallback<ClickEvent>(evt => {
                _activeDot = backPreview;
                colorPicker.Open(backPreview.style.backgroundColor.value);
            });
        }

        if (saveBtn != null) saveBtn.SetEnabled(false);
        if (messageUI != null) messageUI.style.display = DisplayStyle.None;

        generateBtn.clicked += OnGenerateClicked;
        saveBtn.clicked += OnSaveClicked;
        backBtn.clicked += () => SceneManager.LoadScene("MenuScene");

        if (historyBtn != null)
            historyBtn.clicked += () => SceneManager.LoadScene("HistoryScene");
    }

    private void OnGenerateClicked()
    {
        string fontHex = "#" + ColorUtility.ToHtmlStringRGB(fontPreview.style.backgroundColor.value);
        string backHex = "#" + ColorUtility.ToHtmlStringRGB(backPreview.style.backgroundColor.value);

        currentTexture = qrGenerator.Generate(contentField.value, fontHex, backHex);

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
            fc = "#" + ColorUtility.ToHtmlStringRGB(fontPreview.style.backgroundColor.value),
            bc = "#" + ColorUtility.ToHtmlStringRGB(backPreview.style.backgroundColor.value)
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

    private void UpdateMemoPreview()
    {
        if (memoPreview == null || memoText == null) return;

        memoPreview.style.backgroundColor = backPreview.style.backgroundColor.value;

        memoText.style.color = fontPreview.style.backgroundColor.value;
    }
}