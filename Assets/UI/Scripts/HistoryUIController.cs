using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class HistoryUIController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset itemTemplate;
    [SerializeField] private QRGenerator qrGenerator;

    private ScrollView scrollView;
    private Button backBtn;
    private Button deleteSelectedBtn;

    private VisualElement dialogOverlay;
    private Button confirmDeleteBtn;
    private Button cancelDeleteBtn;

    private List<MemoData> selectedItems = new List<MemoData>();

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

        if (backBtn != null)
            backBtn.clicked += () => SceneManager.LoadScene("GenerateScene");

        if (deleteSelectedBtn != null)
        {
            deleteSelectedBtn.SetEnabled(false);
            deleteSelectedBtn.clicked += () => {
                if (dialogOverlay != null)
                    dialogOverlay.style.display = DisplayStyle.Flex;
            };
        }

        if (cancelDeleteBtn != null)
            cancelDeleteBtn.clicked += () => dialogOverlay.style.display = DisplayStyle.None;

        if (confirmDeleteBtn != null)
        {
            confirmDeleteBtn.clicked += () => {
                ExecuteDelete();
                dialogOverlay.style.display = DisplayStyle.None;
            };
        }

        RefreshList();
    }

    private void ExecuteDelete()
    {
        if (selectedItems.Count == 0) return;

        HistoryWrapper history = GetHistory();

        history.memoList.RemoveAll(m => selectedItems.Any(s =>
            s.txt == m.txt && s.fc == m.fc && s.bc == m.bc));

        PlayerPrefs.SetString("QR_HISTORY", JsonUtility.ToJson(history));
        PlayerPrefs.Save();

        RefreshList();

        if (deleteSelectedBtn != null)
            deleteSelectedBtn.SetEnabled(false);
    }

    private void RefreshList()
    {
        if (scrollView == null) return;
        scrollView.Clear();
        selectedItems.Clear();

        HistoryWrapper history = GetHistory();

        foreach (var data in history.memoList)
        {
            VisualElement item = itemTemplate.CloneTree();
            item.RemoveFromClassList("selected-item");

            item.RegisterCallback<ClickEvent>(evt => ToggleSelection(item, data));

            var label = item.Q<Label>("ItemText");
            if (label != null) label.text = data.txt;

            var qrPreview = item.Q<VisualElement>("QRPreview");
            if (qrPreview != null && qrGenerator != null)
            {
                Texture2D tex = qrGenerator.Generate(data.txt, data.fc, data.bc);
                if (tex != null)
                    qrPreview.style.backgroundImage = new StyleBackground(tex);
            }

            var fontDot = item.Q<VisualElement>("FontColorDot");
            if (fontDot != null && !string.IsNullOrWhiteSpace(data.fc))
            {
                string hex = data.fc.StartsWith("#") ? data.fc : "#" + data.fc;
                if (ColorUtility.TryParseHtmlString(hex, out Color fCol))
                    fontDot.style.backgroundColor = fCol;
            }

            var backDot = item.Q<VisualElement>("BackColorDot");
            if (backDot != null && !string.IsNullOrWhiteSpace(data.bc))
            {
                string hex = data.bc.StartsWith("#") ? data.bc : "#" + data.bc;
                if (ColorUtility.TryParseHtmlString(hex, out Color bCol))
                    backDot.style.backgroundColor = bCol;
            }

            scrollView.Add(item);
        }
    }

    private void ToggleSelection(VisualElement item, MemoData data)
    {
        if (selectedItems.Contains(data))
        {
            selectedItems.Remove(data);
            item.RemoveFromClassList("selected-item");
            item.style.backgroundColor = new StyleColor(Color.clear);
        }
        else
        {
            selectedItems.Add(data);
            item.AddToClassList("selected-item");
            item.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.5f));
        }

        if (deleteSelectedBtn != null)
        {
            deleteSelectedBtn.SetEnabled(selectedItems.Count > 0);
        }
    }
}