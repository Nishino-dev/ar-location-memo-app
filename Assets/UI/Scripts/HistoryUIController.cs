using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class HistoryUIController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset itemTemplate;
    [SerializeField] private QRGenerator qrGenerator;

    private ScrollView scrollView;
    private Button backBtn;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        scrollView = root.Q<ScrollView>("HistoryScrollView");
        backBtn = root.Q<Button>("BackButton");

        if (backBtn != null)
            backBtn.clicked += () => SceneManager.LoadScene("GenerateScene");

        RefreshList();
    }

    private void RefreshList()
    {
        if (scrollView == null) return;
        scrollView.Clear();

        string json = PlayerPrefs.GetString("QR_HISTORY", "{\"memoList\":[]}");
        HistoryWrapper history = JsonUtility.FromJson<HistoryWrapper>(json);

        foreach (var data in history.memoList)
        {
            VisualElement item = itemTemplate.CloneTree();

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

            var deleteBtn = item.Q<Button>("DeleteButton");
            if (deleteBtn != null)
            {
                deleteBtn.clicked += () => {
                    DeleteEntry(data);
                    RefreshList();
                };
            }

            scrollView.Add(item);
        }
    }

    private void DeleteEntry(MemoData targetData)
    {
        string json = PlayerPrefs.GetString("QR_HISTORY", "{\"memoList\":[]}");
        HistoryWrapper history = JsonUtility.FromJson<HistoryWrapper>(json);

        history.memoList.RemoveAll(m => m.txt == targetData.txt && m.fc == targetData.fc && m.bc == targetData.bc);

        PlayerPrefs.SetString("QR_HISTORY", JsonUtility.ToJson(history));
        PlayerPrefs.Save();
    }
}