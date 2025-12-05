using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class ARPlaceObject : MonoBehaviour
{
    [Header("配置するオブジェクト")]
    public GameObject objectToPlace;

    [Header("AR設定")]
    public ARRaycastManager raycastManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        bool isPressed = false;
        Vector2 touchPosition = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            isPressed = true;
            touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isPressed = true;
            touchPosition = Mouse.current.position.ReadValue();
        }

        if (isPressed && raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
        }
    }
}