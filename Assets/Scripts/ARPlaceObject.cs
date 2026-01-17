using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceObject : MonoBehaviour
{
    [Header("設定")]
    public ARTrackedImageManager trackedImageManager;
    public ARRaycastManager raycastManager;
    public GameObject objectToPlace;

    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    public void PlaceOrUpdateByQR(MemoData data)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.AllTypes))
        {
            Pose hitPose = hits[0].pose;
            Vector3 forward = hitPose.rotation * Vector3.forward;
            forward.y = 0;
            Quaternion uprightRotation = Quaternion.LookRotation(forward, Vector3.up);

            if (spawnedObjects.ContainsKey(data.txt))
            {
                UpdateMemoTransform(spawnedObjects[data.txt], hitPose.position, uprightRotation, data);
            }
            else
            {
                GameObject obj = Instantiate(objectToPlace, hitPose.position, uprightRotation);
                if (obj.TryGetComponent<ARMemo>(out var memo)) memo.Initialize(data);
                obj.AddComponent<ARAnchor>();
                spawnedObjects.Add(data.txt, obj);
            }
        }
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added) UpdateImage(trackedImage);
        foreach (var trackedImage in eventArgs.updated) UpdateImage(trackedImage);
        foreach (var trackedImage in eventArgs.removed)
        {
            string name = trackedImage.referenceImage.name;
            if (spawnedObjects.ContainsKey(name))
            {
                Destroy(spawnedObjects[name]);
                spawnedObjects.Remove(name);
            }
        }
    }

    void UpdateImage(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;
        MemoData data = new MemoData { v = 1, txt = name, fc = "#FFFFFF", bc = "#000000", sz = 1.0f };

        if (!spawnedObjects.ContainsKey(name))
        {
            GameObject newObj = Instantiate(objectToPlace, trackedImage.transform.position, trackedImage.transform.rotation);
            if (newObj.TryGetComponent<ARMemo>(out var memo)) memo.Initialize(data);
            spawnedObjects[name] = newObj;
        }

        GameObject obj = spawnedObjects[name];

        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            obj.SetActive(true);
            UpdateMemoTransform(obj, trackedImage.transform.position, trackedImage.transform.rotation, data);
        }
        else
        {
            obj.SetActive(false);
        }
    }

    private void UpdateMemoTransform(GameObject obj, Vector3 pos, Quaternion rot, MemoData data)
    {
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        if (obj.TryGetComponent<ARMemo>(out var memo)) memo.Initialize(data);

        var anchor = obj.GetComponent<ARAnchor>();
        if (anchor != null) Destroy(anchor);
        obj.AddComponent<ARAnchor>();
    }
}