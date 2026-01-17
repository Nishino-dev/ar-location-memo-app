using UnityEngine;
using TMPro;

public class ARMemo : MonoBehaviour
{
    private TMP_Text textComponent;
    public MeshRenderer backgroundRenderer;

    void Awake()
    {
        textComponent = GetComponentInChildren<TMP_Text>();
    }

    public void Initialize(MemoData data)
    {
        if (textComponent != null)
        {
            textComponent.text = data.txt;
            if (ColorUtility.TryParseHtmlString(data.fc, out Color fontCol))
                textComponent.color = fontCol;
        }

        if (backgroundRenderer != null && !string.IsNullOrEmpty(data.bc))
        {
            if (ColorUtility.TryParseHtmlString(data.bc, out Color backCol))
            {
                Material newMat = new Material(backgroundRenderer.sharedMaterial);
                newMat.color = backCol;
                backgroundRenderer.material = newMat;
            }
        }

        float scale = Mathf.Clamp(data.sz, 0.1f, 2.0f);
        transform.localScale = Vector3.one * scale;
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }
}