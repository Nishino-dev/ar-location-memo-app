using UnityEngine;
using TMPro;

public class ARMemo : MonoBehaviour
{
    private TMP_Text textComponent;

    void Awake()
    {
        textComponent = GetComponentInChildren<TMP_Text>();
    }

    public void Initialize(string content)
    {
        if (textComponent != null)
        {
            textComponent.text = content;
        }

        transform.Rotate(90f, 0f, 0f);
    }
}