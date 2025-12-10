using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using Google.XR.ARCoreExtensions;

public class ARPlaceObject : MonoBehaviour
{
    [Header("配置・UI設定")]
    public GameObject objectToPlace;
    public TMP_InputField noteInputField;
    public ARRaycastManager raycastManager;
    public TMP_Text statusText;

    [Header("Cloud Anchor設定")]
    public ARAnchorManager anchorManager;

    [Header("ボタン設定")]
    public GameObject editButton;
    public GameObject loadButton;

    private const string SAVE_KEY_IDS = "SavedAnchorIDs";
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject selectedNote = null;

    void Start()
    {
        if (editButton != null) editButton.SetActive(false);
        noteInputField.onSubmit.AddListener(OnSubmitNote);
        ShowStatus("準備完了！メモを置いてください");
    }

    void Update()
    {
        if (IsPointerOverUI()) return;
        if (GetTouchBegan()) HandleTap();
    }

    void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log("Status: " + message);
    }

    void HandleTap()
    {
        GameObject clickedNote = GetClickedNote();
        if (clickedNote != null)
        {
            SelectNote(clickedNote);
            return;
        }

        Vector2 touchPosition = GetTouchPosition();
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            if (selectedNote != null) DeselectNote();
            else CreateNewNote(hits[0].pose);
        }
    }

    void CreateNewNote(Pose hitPose)
    {
        Quaternion rotationFix = hitPose.rotation * Quaternion.Euler(90, 0, 0);
        GameObject newObject = Instantiate(objectToPlace, hitPose.position, rotationFix);

        TMP_Text textComponent = newObject.GetComponentInChildren<TMP_Text>();
        string initialText = "MEMO";
        if (textComponent != null) textComponent.text = "Hosting...";

        spawnedObjects.Add(newObject);
        SelectNote(newObject);

        ShowStatus("クラウドに保存中...");
        StartCoroutine(HostCloudAnchor(newObject, initialText));
    }

    IEnumerator HostCloudAnchor(GameObject noteObject, string memoText)
    {
        ARAnchor localAnchor = noteObject.AddComponent<ARAnchor>();
        yield return new WaitForEndOfFrame();

        var promise = anchorManager.HostCloudAnchorAsync(localAnchor, 1);
        yield return promise;

        TMP_Text textComponent = noteObject.GetComponentInChildren<TMP_Text>();

        if (promise.State == PromiseState.Done)
        {
            string cloudId = promise.Result.CloudAnchorId;
            ShowStatus("保存成功！");
            SaveAnchorData(cloudId, memoText);

            if (textComponent != null) textComponent.text = memoText;

            yield return new WaitForSeconds(3f);
            ShowStatus("保存完了");
        }
        else
        {
            ShowStatus("保存失敗: " + promise.State);
            if (textComponent != null) textComponent.text = "Error";
        }
    }

    public void OnLoadButtonClicked()
    {
        string storedIds = PlayerPrefs.GetString(SAVE_KEY_IDS, "");
        if (string.IsNullOrEmpty(storedIds))
        {
            ShowStatus("保存されたデータがありません");
            return;
        }

        string[] ids = storedIds.Split(',');
        ShowStatus($"{ids.Length} 個のメモを探しています...");

        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id))
            {
                StartCoroutine(ResolveCloudAnchor(id));
            }
        }
    }

    IEnumerator ResolveCloudAnchor(string cloudId)
    {
        var promise = anchorManager.ResolveCloudAnchorAsync(cloudId);

        while (promise.State == PromiseState.Pending)
        {
            yield return null;
        }

        if (promise.State == PromiseState.Done)
        {
            var result = promise.Result;

            if (result == null || result.Anchor == null)
            {
                ShowStatus($"場所が見つかりません (理由: {result?.CloudAnchorState})");
                yield break;
            }

            ARCloudAnchor resultAnchor = result.Anchor;
            ShowStatus("場所発見！復元中...");

            GameObject restoredObject = Instantiate(objectToPlace, resultAnchor.transform.position, resultAnchor.transform.rotation);
            restoredObject.transform.SetParent(resultAnchor.transform, false);

            string savedText = PlayerPrefs.GetString("Memo_" + cloudId, "MEMO");
            TMP_Text textComponent = restoredObject.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = savedText;

            spawnedObjects.Add(restoredObject);

            yield return new WaitForSeconds(1f);
            ShowStatus("復元完了！");
        }
        else
        {
            ShowStatus($"エラー: {promise.State}");
        }
    }

    void SaveAnchorData(string cloudId, string text)
    {
        string currentIds = PlayerPrefs.GetString(SAVE_KEY_IDS, "");
        if (!currentIds.Contains(cloudId))
        {
            currentIds += cloudId + ",";
            PlayerPrefs.SetString(SAVE_KEY_IDS, currentIds);
        }
        PlayerPrefs.SetString("Memo_" + cloudId, text);
        PlayerPrefs.Save();
    }

    void OnSubmitNote(string text)
    {
        if (selectedNote != null)
        {
            TMP_Text textComponent = selectedNote.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = noteInputField.text;
            DeselectNote();
        }
    }

    void SelectNote(GameObject note)
    {
        if (selectedNote != null) DeselectNote();
        selectedNote = note;
        var outline = selectedNote.GetComponentInChildren<Outline>();
        if (outline != null) outline.enabled = true;

        TMP_Text textComponent = selectedNote.GetComponentInChildren<TMP_Text>();
        if (textComponent != null) noteInputField.text = textComponent.text;
        if (editButton != null) editButton.SetActive(true);
    }

    public void OnEditButtonClicked()
    {
        if (selectedNote == null) return;
        if (editButton != null) editButton.SetActive(false);
        noteInputField.ActivateInputField();
    }

    void DeselectNote()
    {
        if (selectedNote != null)
        {
            var outline = selectedNote.GetComponentInChildren<Outline>();
            if (outline != null) outline.enabled = false;
        }
        selectedNote = null;
        noteInputField.text = "";
        if (editButton != null) editButton.SetActive(false);
        noteInputField.DeactivateInputField();
    }

    public void ClearAllNotes()
    {
        foreach (var obj in spawnedObjects) Destroy(obj);
        spawnedObjects.Clear();
        DeselectNote();
    }

    GameObject GetClickedNote()
    {
        Ray ray = Camera.main.ScreenPointToRay(GetTouchPosition());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.CompareTag("Note")) return hit.collider.gameObject;
            if (hit.transform.parent != null && hit.transform.parent.CompareTag("Note")) return hit.transform.parent.gameObject;
        }
        return null;
    }

    bool GetTouchBegan()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        return false;
    }

    Vector2 GetTouchPosition()
    {
        if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return Vector2.zero;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}