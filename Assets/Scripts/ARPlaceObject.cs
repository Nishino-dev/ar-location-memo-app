using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceObject : MonoBehaviour
{
    [Header("設定")]
    public ARTrackedImageManager trackedImageManager;
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

        if (!spawnedObjects.ContainsKey(name))
        {
            GameObject newObj = Instantiate(objectToPlace);
            newObj.name = name;

            ARMemo memo = newObj.GetComponent<ARMemo>();
            if (memo != null)
            {
                memo.Initialize(name);
            }

            spawnedObjects[name] = newObj;
        }

        GameObject obj = spawnedObjects[name];

        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            obj.SetActive(true);
            obj.transform.position = trackedImage.transform.position;
            obj.transform.rotation = Quaternion.LookRotation(trackedImage.transform.forward, trackedImage.transform.up);
        }
        else
        {
            obj.SetActive(false);
        }
    }
}