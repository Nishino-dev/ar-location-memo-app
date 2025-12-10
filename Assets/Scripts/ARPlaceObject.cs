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

    [Header("Cloud Anchor設定")]
    public ARAnchorManager anchorManager;

    [Header("ボタン設定")]
    public GameObject editButton;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject selectedNote = null;

    void Start()
    {
        if (editButton != null) editButton.SetActive(false);
        noteInputField.onSubmit.AddListener(OnSubmitNote);
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

    void Update()
    {
        if (IsPointerOverUI()) return;
        if (GetTouchBegan()) HandleTap();
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
        if (textComponent != null) textComponent.text = "Hosting...";

        spawnedObjects.Add(newObject);
        SelectNote(newObject);

        StartCoroutine(HostCloudAnchor(newObject));
    }

    IEnumerator HostCloudAnchor(GameObject noteObject)
    {
        ARAnchor localAnchor = noteObject.AddComponent<ARAnchor>();

        yield return new WaitForEndOfFrame();

        Debug.Log("☁️ Hosting開始...");

        var promise = anchorManager.HostCloudAnchorAsync(localAnchor, 1);

        yield return promise;

        TMP_Text textComponent = noteObject.GetComponentInChildren<TMP_Text>();

        if (promise.State == PromiseState.Done)
        {
            string cloudId = promise.Result.CloudAnchorId;
            Debug.Log("✅ Host成功！ Cloud ID: " + cloudId);

            if (textComponent != null)
            {
                textComponent.text = "ID: " + cloudId.Substring(0, 5);
            }
        }
        else
        {
            // 失敗...
            Debug.LogError("❌ Host失敗... エラー: " + promise.State);
            if (textComponent != null) textComponent.text = "Error";
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