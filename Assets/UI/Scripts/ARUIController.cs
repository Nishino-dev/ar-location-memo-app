using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class ARUIController : MonoBehaviour
{
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        var backBtn = root.Q<Button>("BackButton");

        backBtn.clicked += () => SceneManager.LoadScene("MenuScene");
    }
}