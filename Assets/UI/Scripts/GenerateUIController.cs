using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    private VisualElement dialogOverlay;
    private Button confirmMoveBtn;
    private Button cancelMoveBtn;

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

        dialogOverlay = root.Q<VisualElement>("DialogOverlay");
        confirmMoveBtn = root.Q<Button>("ConfirmMoveButton");
        cancelMoveBtn = root.Q<Button>("CancelMoveButton");

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

        if (dialogOverlay != null) dialogOverlay.style.display = DisplayStyle.None;

        if (confirmMoveBtn != null)
            confirmMoveBtn.clicked += () => SceneManager.LoadScene("HistoryScene");

        if (cancelMoveBtn != null)
            cancelMoveBtn.clicked += () => dialogOverlay.style.display = DisplayStyle.None;

        if (saveBtn != null) saveBtn.SetEnabled(false);

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
        SaveToHistory();
        if (dialogOverlay != null)
        {
            dialogOverlay.style.display = DisplayStyle.Flex;
        }
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
        if (history.memoList.Any(m => m.ShortID == newData.ShortID))
        {
            return;
        }
        history.memoList.Insert(0, newData);

        PlayerPrefs.SetString("QR_HISTORY", JsonUtility.ToJson(history));
        PlayerPrefs.Save();
    }

    private void UpdateMemoPreview()
    {
        if (memoPreview == null || memoText == null) return;

        memoPreview.style.backgroundColor = backPreview.style.backgroundColor.value;

        memoText.style.color = fontPreview.style.backgroundColor.value;
    }
}