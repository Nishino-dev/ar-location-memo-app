using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class HistoryUIController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset itemTemplate;
    [SerializeField] private QRGenerator qrGenerator;
    [SerializeField] private QRPrintManager qrPrintManager;

    private ScrollView scrollView;
    private Button backBtn, deleteSelectedBtn, confirmDeleteBtn, cancelDeleteBtn, printBtn;
    private VisualElement dialogOverlay;
    private DropdownField colsDropdown;
    private Label selectedCountLabel;

    private List<MemoData> selectedItems = new List<MemoData>();
    private readonly List<string> printLayoutChoices = new List<string> { "6ñá (2x3)", "12ñá (3x4)", "24ñá (4x6)" };

    private HistoryWrapper GetHistory()
    {
        string json = PlayerPrefs.GetString("QR_HISTORY", "{\"memoList\":[]}");
        return JsonUtility.FromJson<HistoryWrapper>(json);
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        scrollView = root.Q<ScrollView>("HistoryScrollView");
        backBtn = root.Q<Button>("BackButton");
        deleteSelectedBtn = root.Q<Button>("DeleteSelectedButton");
        dialogOverlay = root.Q<VisualElement>("DialogOverlay");
        confirmDeleteBtn = root.Q<Button>("ConfirmDeleteButton");
        cancelDeleteBtn = root.Q<Button>("CancelDeleteButton");
        printBtn = root.Q<Button>("PrintButton");
        colsDropdown = root.Q<DropdownField>("PrintColsDropdown");
        selectedCountLabel = root.Q<Label>("SelectedCountLabel");

        if (backBtn != null) backBtn.clicked += () => SceneManager.LoadScene("GenerateScene");
        if (cancelDeleteBtn != null) cancelDeleteBtn.clicked += () => dialogOverlay.style.display = DisplayStyle.None;

        if (deleteSelectedBtn != null)
        {
            deleteSelectedBtn.SetEnabled(false);
            deleteSelectedBtn.clicked += () => dialogOverlay.style.display = DisplayStyle.Flex;
        }

        if (confirmDeleteBtn != null)
        {
            confirmDeleteBtn.clicked += () => { ExecuteDelete(); dialogOverlay.style.display = DisplayStyle.None; };
        }

        if (colsDropdown != null)
        {
            colsDropdown.choices = printLayoutChoices;
            colsDropdown.value = printLayoutChoices[0];
            colsDropdown.RegisterValueChangedCallback(evt => UpdateSelectionCount());
        }

        if (printBtn != null) printBtn.clicked += OnClickPrintSelected;

        RefreshList();
    }

    private void ExecuteDelete()
    {
        if (selectedItems.Count == 0) return;

        HistoryWrapper history = GetHistory();
        history.memoList.RemoveAll(m => selectedItems.Any(s => s.txt == m.txt && s.fc == m.fc && s.bc == m.bc));

        PlayerPrefs.SetString("QR_HISTORY", JsonUtility.ToJson(history));
        PlayerPrefs.Save();
        RefreshList();
    }

    private void RefreshList()
    {
        if (scrollView == null) return;
        scrollView.Clear();
        selectedItems.Clear();
        UpdateSelectionCount();

        foreach (var data in GetHistory().memoList)
        {
            VisualElement item = itemTemplate.CloneTree();
            item.RegisterCallback<ClickEvent>(evt => ToggleSelection(item, data));

            var idLabel = item.Q<Label>("ItemIDLabel");
            if (idLabel != null) idLabel.text = data.ShortID;

            var textLabel = item.Q<Label>("ItemText");
            if (textLabel != null) textLabel.text = data.txt;

            var qrPreview = item.Q<VisualElement>("QRPreview");
            if (qrPreview != null)
            {
                qrPreview.style.backgroundImage = new StyleBackground(qrGenerator.Generate(data.txt, data.fc, data.bc));
            }

            SetDotColor(item.Q<VisualElement>("FontColorDot"), data.fc);
            SetDotColor(item.Q<VisualElement>("BackColorDot"), data.bc);

            scrollView.Add(item);
        }
    }

    private void SetDotColor(VisualElement dot, string hex)
    {
        if (dot == null || string.IsNullOrWhiteSpace(hex)) return;
        string validHex = hex.StartsWith("#") ? hex : "#" + hex;
        if (ColorUtility.TryParseHtmlString(validHex, out Color col)) dot.style.backgroundColor = col;
    }

    private void ToggleSelection(VisualElement item, MemoData data)
    {
        var existing = selectedItems.FirstOrDefault(s => s.txt == data.txt && s.fc == data.fc && s.bc == data.bc);

        if (existing != null)
        {
            selectedItems.Remove(existing);
            item.style.backgroundColor = new StyleColor(Color.clear);
        }
        else
        {
            selectedItems.Add(data);
            item.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.5f));
        }

        UpdateSelectionCount();
        deleteSelectedBtn?.SetEnabled(selectedItems.Count > 0);
    }

    public void OnClickPrintSelected()
    {
        if (selectedItems.Count == 0) return;

        int cols = 2, rows = 3;
        string val = colsDropdown?.value ?? "";
        if (val.Contains("12")) { cols = 3; rows = 4; }
        else if (val.Contains("24")) { cols = 4; rows = 6; }

        List<Texture2D> texs = new List<Texture2D>();
        List<string> ids = new List<string>();

        foreach (var data in selectedItems)
        {
            texs.Add(qrGenerator.Generate(data.txt, data.fc, data.bc));
            ids.Add(data.ShortID);
        }

        qrPrintManager?.GenerateAndShareHistory(texs, cols, rows, ids);
    }

    private void UpdateSelectionCount()
    {
        if (selectedCountLabel == null) return;

        int count = selectedItems.Count;
        if (count > 0)
        {
            int max = 6;
            string val = colsDropdown?.value ?? "";
            if (val.Contains("24")) max = 24;
            else if (val.Contains("12")) max = 12;

            int pages = (count + max - 1) / max;
            selectedCountLabel.text = $"{count} / {max}ñá ({pages}ÉyÅ[ÉW)";
            selectedCountLabel.style.display = DisplayStyle.Flex;
            printBtn?.SetEnabled(true);
        }
        else
        {
            selectedCountLabel.style.display = DisplayStyle.None;
            printBtn?.SetEnabled(false);
        }
    }
}