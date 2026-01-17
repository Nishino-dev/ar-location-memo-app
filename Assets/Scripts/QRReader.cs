using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

[RequireComponent(typeof(ARAnchorManager))]
public class QRReader : MonoBehaviour
{
    public GameObject objectToSpawn;
    public ARRaycastManager raycastManager;
    private Camera mainCamera;
    private ARCameraManager cameraManager;
    private bool isScanning = false;
    private float nextScanTime = 0;
    public float scanInterval = 0.5f;

    void Start()
    {
        cameraManager = GetComponentInChildren<ARCameraManager>();
        if (cameraManager != null)
        {
            mainCamera = cameraManager.GetComponent<Camera>();
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
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
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

    private async void ProcessScan(byte[] pixels, int width, int height)
    {
        string decodedText = await Task.Run(() => QRDecoder.Decode(pixels, width, height));

        if (!string.IsNullOrEmpty(decodedText))
        {
            UnityMainThreadExecutor.Instance().Enqueue(() => {
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
        nextScanTime = Time.time + scanInterval;
        isScanning = false;
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