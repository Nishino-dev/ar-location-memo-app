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
    public float scanInterval = 0.5f;

    void Start()
    {
        cameraManager = GetComponentInChildren<ARCameraManager>();
        if (cameraManager != null)
        {
            cameraManager.autoFocusRequested = true;
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

    public void OnMLKitResult(string decodedText)
    {
        UnityMainThreadExecutor.Instance().Enqueue(() => {
            if (string.IsNullOrEmpty(decodedText)) return;

            MemoData data = null;
            try
            {
                data = JsonUtility.FromJson<MemoData>(decodedText);
            }
            catch
            {
                if (decodedText.StartsWith("{")) return;
                data = new MemoData { v = 1, txt = decodedText, fc = "#000000", bc = "#FFFFFF", sz = 1.0f };
            }

            if (data != null)
            {
                var placer = FindObjectOfType<ARPlaceObject>();
                if (placer != null) placer.PlaceOrUpdateByQR(data);
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