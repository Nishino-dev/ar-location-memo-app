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
    [Header("AR Settings")]
    public GameObject objectToSpawn;
    public ARRaycastManager raycastManager;
    private Camera mainCamera;

    [Header("Mode Settings")]
    public bool isWideMode = false;

    [Header("Focus Mode Settings")]
    public float focusCropFactor = 0.4f;
    public int focusDownscale = 1;

    [Header("Wide Mode Settings")]
    public float wideCropFactor = 1.0f;
    public int wideDownscale = 2;

    [Header("General Settings")]
    public float scanInterval = 0.5f;

    private ARCameraManager cameraManager;
    private bool isScanning = false;
    private float nextScanTime = 0;

    private HashSet<string> scannedQRTexts = new HashSet<string>();

    private class ScanResult
    {
        public string Text;
        public float CenterX;
        public float CenterY;
    }

    void Start()
    {
        cameraManager = GetComponentInChildren<ARCameraManager>();

        if (cameraManager != null)
        {
            mainCamera = cameraManager.GetComponent<Camera>();
            cameraManager.autoFocusRequested = true;
        }

        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
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

        float currentCrop = isWideMode ? wideCropFactor : focusCropFactor;
        int currentDownscale = isWideMode ? wideDownscale : focusDownscale;

        int cropWidth = (int)(image.width * currentCrop);
        int cropHeight = (int)(image.height * currentCrop);
        int startX = (image.width - cropWidth) / 2;
        int startY = (image.height - cropHeight) / 2;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(startX, startY, cropWidth, cropHeight),
            outputDimensions = new Vector2Int(cropWidth / currentDownscale, cropHeight / currentDownscale),
            outputFormat = TextureFormat.R8,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();

        byte[] pixelData = buffer.ToArray();
        buffer.Dispose();

        ScanInBackground(pixelData, conversionParams.outputDimensions.x, conversionParams.outputDimensions.y,
                         startX, startY, currentDownscale, image.width, image.height);
    }

    private async void ScanInBackground(byte[] pixels, int width, int height,
                                        int offsetX, int offsetY, int downscale, int origW, int origH)
    {
        string decodedText = await Task.Run(() => QRDecoder.Decode(pixels, width, height));

        if (!string.IsNullOrEmpty(decodedText))
        {
            float centerX = (width / 2.0f * downscale + offsetX) / origW;
            float centerY = (height / 2.0f * downscale + offsetY) / origH;
            SpawnAndLock(decodedText, centerX, centerY);
        }

        nextScanTime = Time.time + scanInterval;
        isScanning = false;
    }

    private void SpawnAndLock(string qrText, float normX, float normY)
    {
        if (scannedQRTexts.Contains(qrText)) return;

        Vector3 viewportPoint = new Vector3(normX, 1.0f - normY, 0);
        Vector2 screenPosition = mainCamera.ViewportToScreenPoint(viewportPoint);
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = hits[0];

            GameObject obj = Instantiate(objectToSpawn, hit.pose.position, hit.pose.rotation);

            ARMemo memo = obj.GetComponent<ARMemo>();
            if (memo != null)
            {
                memo.Initialize(qrText);
            }

            obj.AddComponent<ARAnchor>();
            scannedQRTexts.Add(qrText);
        }
    }
}