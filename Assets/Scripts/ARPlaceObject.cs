using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceObject : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject objectToPlace;
    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

    public void PlaceOrUpdateByQR(MemoData data, Vector2[] corners, Quaternion qrRotation)
    {
        if (data == null || string.IsNullOrEmpty(data.txt) || corners == null || corners.Length < 4) return;
        if (spawnedObjects.ContainsKey(data.txt)) return;

        Vector2 qrScreenPos = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector3 targetPos;
        Quaternion targetRot;
        Vector3 normal = Vector3.up;

        if (raycastManager.Raycast(qrScreenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            targetPos = hitPose.position;
            normal = hitPose.up;

            Vector3 toCamera = Camera.main.transform.position - targetPos;
            Vector3 forward = Vector3.ProjectOnPlane(toCamera, normal).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, normal).normalized;

            targetRot = Quaternion.LookRotation(forward, normal);

            if (qrRotation != Quaternion.identity)
            {
                targetRot *= Quaternion.AngleAxis(qrRotation.eulerAngles.z, normal);
            }

            targetPos += normal * 0.01f;
        }
        else
        {
            targetPos = Camera.main.ScreenToWorldPoint(new Vector3(qrScreenPos.x, qrScreenPos.y, 0.4f));
            targetRot = Quaternion.LookRotation(Camera.main.transform.forward);
        }

        GameObject obj = Instantiate(objectToPlace, targetPos, targetRot);

        if (obj.TryGetComponent<ARMemo>(out var memo))
        {
            memo.Initialize(data);
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            Vector3 centerOffset = obj.transform.position - bounds.center;
            obj.transform.position += centerOffset;
        }

        obj.AddComponent<ARAnchor>();

        spawnedObjects.Add(data.txt, obj);
    }
}