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
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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
    public GameObject deleteButton;

    private const string SAVE_KEY_IDS = "SavedAnchorIDs";
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject selectedNote = null;
    private GameObject targetNote = null;
    private Vector2 touchStartPos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD_SQR = 400f;

    private float initialPinchDistance;
    private Vector3 initialScale;
    private float initialPinchAngle;
    private Quaternion initialRotation;
    private bool isMultiTouchMode = false;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        if (editButton != null) editButton.SetActive(false);
        if (deleteButton != null) deleteButton.SetActive(false);
        noteInputField.onSubmit.AddListener(OnSubmitNote);
        ShowStatus("準備完了！メモを置いてください");
    }

    void Update()
    {
        var activeTouches = Touch.activeTouches;
        int touchCount = activeTouches.Count;

        bool isMouseInput = false;
        if (touchCount == 0 && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            touchCount = 1;
            isMouseInput = true;
        }

        if (IsPointerOverUI(activeTouches, isMouseInput)) return;

        if (touchCount >= 2)
        {
            isMultiTouchMode = true;
            isDragging = false;
            HandleTwoFingerManipulation(activeTouches);
        }
        else if (touchCount == 1)
        {
            if (isMultiTouchMode)
            {
                if (isMouseInput)
                {
                    if (!Mouse.current.leftButton.isPressed) ResetMultiTouch();
                }
                else
                {
                    if (activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Ended) ResetMultiTouch();
                }
                return;
            }
            HandleOneFingerInput(activeTouches, isMouseInput);
        }
        else
        {
            ResetMultiTouch();
            isDragging = false;
            targetNote = null;
        }
    }

    void ResetMultiTouch()
    {
        isMultiTouchMode = false;
        initialPinchDistance = 0f;
    }

    void HandleOneFingerInput(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches, bool isMouse)
    {
        Vector2 touchPos = isMouse ? Mouse.current.position.ReadValue() : touches[0].screenPosition;
        UnityEngine.InputSystem.TouchPhase phase = isMouse ? GetMousePhase() : touches[0].phase;

        if (phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            touchStartPos = touchPos;
            isDragging = false;
            targetNote = GetClickedNote(touchPos);
        }
        else if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            if (!isDragging)
            {
                if ((touchPos - touchStartPos).sqrMagnitude > DRAG_THRESHOLD_SQR)
                {
                    isDragging = true;
                }
            }

            if (isDragging && selectedNote != null && targetNote == selectedNote)
            {
                MoveSelectedObject(touchPos);
            }
        }
        else if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            if (!isDragging)
            {
                if (targetNote != null)
                {
                    SelectNote(targetNote);
                }
                else
                {
                    if (selectedNote != null)
                    {
                        DeselectNote();
                    }
                    else
                    {
                        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds))
                        {
                            CreateNewNote(hits[0].pose);
                        }
                    }
                }
            }
            isDragging = false;
            targetNote = null;
        }
    }

    void MoveSelectedObject(Vector2 touchPos)
    {
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds))
        {
            Pose hitPose = hits[0].pose;
            selectedNote.transform.position = hitPose.position;
        }
    }

    void HandleTwoFingerManipulation(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches)
    {
        if (selectedNote == null || touches.Count < 2) return;

        Vector2 p1 = touches[0].screenPosition;
        Vector2 p2 = touches[1].screenPosition;

        float currentDistance = Vector2.Distance(p1, p2);
        float currentAngle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;

        if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Began ||
            touches[1].phase == UnityEngine.InputSystem.TouchPhase.Began ||
            initialPinchDistance == 0)
        {
            initialPinchDistance = currentDistance;
            initialScale = selectedNote.transform.localScale;
            initialPinchAngle = currentAngle;
            initialRotation = selectedNote.transform.rotation;
        }
        else
        {
            if (initialPinchDistance > 0.01f)
            {
                float factor = currentDistance / initialPinchDistance;
                Vector3 newScale = initialScale * factor;
                newScale = Vector3.Max(newScale, Vector3.one * 0.1f);
                newScale = Vector3.Min(newScale, Vector3.one * 3.0f);
                selectedNote.transform.localScale = newScale;
            }

            float angleDelta = currentAngle - initialPinchAngle;
            Quaternion rotationZ = Quaternion.Euler(0, 0, angleDelta);
            selectedNote.transform.rotation = initialRotation * rotationZ;
        }
    }

    UnityEngine.InputSystem.TouchPhase GetMousePhase()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) return UnityEngine.InputSystem.TouchPhase.Began;
        if (Mouse.current.leftButton.wasReleasedThisFrame) return UnityEngine.InputSystem.TouchPhase.Ended;
        return UnityEngine.InputSystem.TouchPhase.Moved;
    }

    GameObject GetClickedNote(Vector2 touchPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.CompareTag("Note")) return hit.collider.gameObject;
            if (hit.transform.parent != null && hit.transform.parent.CompareTag("Note")) return hit.transform.parent.gameObject;
        }
        return null;
    }

    bool IsPointerOverUI(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches, bool isMouse)
    {
        if (EventSystem.current == null) return false;
        if (isMouse) return EventSystem.current.IsPointerOverGameObject();
        if (touches.Count > 0) return EventSystem.current.IsPointerOverGameObject(touches[0].touchId);
        return false;
    }

    void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log("Status: " + message);
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

        if (promise.State == PromiseState.Done)
        {
            string cloudId = promise.Result.CloudAnchorId;
            ShowStatus("保存成功！");
            noteObject.name = cloudId;
            SaveAnchorData(cloudId, memoText);
            TMP_Text textComponent = noteObject.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = memoText;
            yield return new WaitForSeconds(2f);
            ShowStatus("保存完了");
        }
        else
        {
            ShowStatus("保存失敗: " + promise.State);
        }
    }

    public void OnDeleteButtonClicked()
    {
        if (selectedNote == null) return;
        string targetId = selectedNote.name;
        if (targetId.StartsWith("ua-")) RemoveAnchorData(targetId);
        spawnedObjects.Remove(selectedNote);
        Destroy(selectedNote);
        DeselectNote();
        ShowStatus("削除しました");
    }

    void RemoveAnchorData(string cloudId)
    {
        string currentIds = PlayerPrefs.GetString(SAVE_KEY_IDS, "");
        if (currentIds.Contains(cloudId))
        {
            currentIds = currentIds.Replace(cloudId + ",", "");
            PlayerPrefs.SetString(SAVE_KEY_IDS, currentIds);
        }
        PlayerPrefs.DeleteKey("Memo_" + cloudId);
        PlayerPrefs.Save();
    }

    public void OnLoadButtonClicked()
    {
        string storedIds = PlayerPrefs.GetString(SAVE_KEY_IDS, "");
        if (string.IsNullOrEmpty(storedIds)) { ShowStatus("保存データなし"); return; }
        string[] ids = storedIds.Split(',');
        ShowStatus(ids.Length + " 個のメモを復元中...");
        foreach (string id in ids) if (!string.IsNullOrEmpty(id)) StartCoroutine(ResolveCloudAnchor(id));
    }

    IEnumerator ResolveCloudAnchor(string cloudId)
    {
        var promise = anchorManager.ResolveCloudAnchorAsync(cloudId);
        while (promise.State == PromiseState.Pending) yield return null;
        if (promise.State == PromiseState.Done)
        {
            var result = promise.Result;
            if (result.Anchor == null) yield break;

            GameObject restoredObject = Instantiate(objectToPlace, result.Anchor.transform.position, result.Anchor.transform.rotation);
            restoredObject.transform.SetParent(result.Anchor.transform, false);
            restoredObject.name = cloudId;
            string savedText = PlayerPrefs.GetString("Memo_" + cloudId, "MEMO");
            TMP_Text textComponent = restoredObject.GetComponentInChildren<TMP_Text>();
            if (textComponent != null) textComponent.text = savedText;
            spawnedObjects.Add(restoredObject);
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
        if (deleteButton != null) deleteButton.SetActive(true);
    }

    public void OnEditButtonClicked()
    {
        if (selectedNote == null) return;
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
        if (deleteButton != null) deleteButton.SetActive(false);
        noteInputField.DeactivateInputField();
        isMultiTouchMode = false;
    }
}