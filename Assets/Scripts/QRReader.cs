using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARAnchorManager))]
public class QRReader : MonoBehaviour
{
    public GameObject objectToSpawn;
    public ARRaycastManager raycastManager;
    private ARCameraManager cameraManager;
    private bool isScanning = false;
    private float nextScanTime = 0;
    public float scanInterval = 0.25f;

    void Start()
    {
        cameraManager = GetComponentInChildren<ARCameraManager>();
        if (cameraManager != null)
        {
            cameraManager.autoFocusRequested = true;
            var configurations = cameraManager.GetConfigurations(Unity.Collections.Allocator.Temp);
            if (configurations.IsCreated && configurations.Length > 0)
            {
                int bestFps = 0;
                int bestIndex = 0;
                for (int i = 0; i < configurations.Length; i++)
                {
                    if (configurations[i].framerate.HasValue && configurations[i].framerate.Value > bestFps)
                    {
                        bestFps = configurations[i].framerate.Value;
                        bestIndex = i;
                    }
                }
                cameraManager.currentConfiguration = configurations[bestIndex];
                configurations.Dispose();
            }
        }
        if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();
    }

    void Update()
    {
        if (Time.time < nextScanTime || isScanning) return;
        TryScanQR();
    }

    void TryScanQR()
    {
        if (cameraManager == null || !cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;
        isScanning = true;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.R8,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();

        byte[] pixelData = buffer.ToArray();
        buffer.Dispose();

        ProcessScan(pixelData, conversionParams.outputDimensions.x, conversionParams.outputDimensions.y);
    }

    private void ProcessScan(byte[] pixels, int width, int height)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            try
            {
                using (AndroidJavaClass scannerClass = new AndroidJavaClass("com.nishino_dev.arlocationmemoapp.MLKitScanner"))
                {
                    scannerClass.CallStatic("scanImage", pixels, width, height, 270);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("ML Kit Call Failed: " + e.Message);
            }
        }
        nextScanTime = Time.time + scanInterval;
        isScanning = false;
    }

    public void OnMLKitResult(string rawData)
    {
        UnityMainThreadExecutor.Instance().Enqueue(() =>
        {
            if (string.IsNullOrEmpty(rawData)) return;

            string[] mainParts = rawData.Split('|');
            if (mainParts.Length < 3) return;

            string decodedText = mainParts[0];
            string[] coords = mainParts[1].Split(',');
            string[] res = mainParts[2].Split(',');

            if (coords.Length == 2 && res.Length == 2)
            {
                float rawX = float.Parse(coords[0]);
                float rawY = float.Parse(coords[1]);
                float camW = float.Parse(res[0]);
                float camH = float.Parse(res[1]);

                float normX = rawX / camW;
                float normY = rawY / camH;

                float screenAspect = (float)Screen.width / Screen.height;
                float cameraAspect = camW / camH;

                float finalX, finalY;

                if (screenAspect < cameraAspect)
                {
                    float scale = cameraAspect / screenAspect;
                    finalX = (1.0f - normY) * Screen.width;
                    finalY = (1.0f - normX) * Screen.height;
                }
                else
                {
                    float scale = screenAspect / cameraAspect;
                    finalX = (1.0f - normY) * Screen.width;
                    finalY = (1.0f - normX) * Screen.height;
                }

                Vector2 center = new Vector2(finalX, finalY);
                Vector2[] corners = new Vector2[4];
                for (int i = 0; i < 4; i++) corners[i] = center;

                MemoData data = null;
                try { data = JsonUtility.FromJson<MemoData>(decodedText); }
                catch { data = new MemoData { v = 1, txt = decodedText, fc = "#000000", bc = "#FFFFFF" }; }

                var placer = FindObjectOfType<ARPlaceObject>();
                if (placer != null) placer.PlaceOrUpdateByQR(data, corners, Quaternion.identity);
            }
        });
    }
}

public class UnityMainThreadExecutor : MonoBehaviour
{
    private static UnityMainThreadExecutor _instance;
    private readonly Queue<System.Action> _actionQueue = new Queue<System.Action>();
    public static UnityMainThreadExecutor Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadExecutor");
            _instance = go.AddComponent<UnityMainThreadExecutor>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }
    public void Enqueue(System.Action action)
    {
        lock (_actionQueue) { _actionQueue.Enqueue(action); }
    }
    void Update()
    {
        lock (_actionQueue)
        {
            while (_actionQueue.Count > 0) _actionQueue.Dequeue().Invoke();
        }
    }
}