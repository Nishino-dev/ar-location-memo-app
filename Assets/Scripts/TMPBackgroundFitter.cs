using UnityEngine;
using TMPro;

public class TMPBackgroundFitter : MonoBehaviour
{
    public TextMeshPro text;
    public Transform background;

    public Vector2 padding = new Vector2(0.02f, 0.01f);

    void LateUpdate()
    {
        if (text == null || background == null) return;

        text.ForceMeshUpdate();
        Bounds b = text.bounds;

        background.localScale = new Vector3(
            b.size.x + padding.x,
            b.size.y + padding.y,
            1f
        );

        background.localPosition = new Vector3(
            b.center.x,
            b.center.y,
            0f
        );
    }
}