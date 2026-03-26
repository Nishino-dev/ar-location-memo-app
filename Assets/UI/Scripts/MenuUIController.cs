using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuUIController : MonoBehaviour
{
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        var arBtn = root.Q<Button>("ARButton");
        var editorBtn = root.Q<Button>("QRButton");

        arBtn.clicked += () => SceneManager.LoadScene("ARScene");
        editorBtn.clicked += () => SceneManager.LoadScene("GenerateScene");
    }
}