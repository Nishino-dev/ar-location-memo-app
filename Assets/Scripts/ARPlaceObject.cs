using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ARPlaceObject : MonoBehaviour
{
    [Header("配置するオブジェクト")]
    public GameObject objectToPlace;

    [Header("AR設定")]
    public ARRaycastManager raycastManager;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (WasTapped())
        {
            if (IsPointerOverUI()) return;

            Vector2 touchPosition = GetTouchPosition();

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                GameObject newObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                spawnedObjects.Add(newObject);
            }
        }
    }

    public void ClearAllNotes()
    {
        foreach (var obj in spawnedObjects)
        {
            Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    bool WasTapped()
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