package com.nishino_dev.arlocationmemoapp;

import android.graphics.Bitmap;
import android.graphics.Color;
import android.util.Log;
import com.google.mlkit.vision.barcode.BarcodeScanner;
import com.google.mlkit.vision.barcode.BarcodeScannerOptions;
import com.google.mlkit.vision.barcode.BarcodeScanning;
import com.google.mlkit.vision.barcode.common.Barcode;
import com.google.mlkit.vision.common.InputImage;
import com.unity3d.player.UnityPlayer;

public class MLKitScanner {
    private static final String TAG = "MLKitScanner";
    private static final BarcodeScanner scanner;

    static {
        BarcodeScannerOptions options = new BarcodeScannerOptions.Builder()
                .setBarcodeFormats(Barcode.FORMAT_QR_CODE)
                .build();
        scanner = BarcodeScanning.getClient(options);
    }

    public static void scanImage(byte[] data, int width, int height, int rotation) {
        if (data == null || data.length == 0) return;

        try {
            Bitmap bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
            int[] pixels = new int[width * height];
            for (int i = 0; i < pixels.length; i++) {
                int gray = data[i] & 0xff;
                pixels[i] = Color.rgb(gray, gray, gray);
            }
            bitmap.setPixels(pixels, 0, width, 0, 0, width, height);

            InputImage image = InputImage.fromBitmap(bitmap, rotation);

            scanner.process(image)
                .addOnSuccessListener(barcodes -> {
                    for (Barcode barcode : barcodes) {
                        String result = extractText(barcode);
                        if (result != null && !result.isEmpty()) {
                            UnityPlayer.UnitySendMessage("XR Origin", "OnMLKitResult", result);
                        }
                    }
                })
                .addOnFailureListener(e -> Log.e(TAG, "Scan fail", e));
        } catch (Exception e) {
            Log.e(TAG, "Error: " + e.getMessage());
        }
    }

    private static String extractText(Barcode barcode) {
        byte[] bytes = barcode.getRawBytes();
        if (bytes != null) {
            try {
                return new String(bytes, "UTF-8");
            } catch (Exception e) {
            }
        }

        String rawValue = barcode.getRawValue();
        if (rawValue != null && !rawValue.isEmpty()) return rawValue;

        String displayValue = barcode.getDisplayValue();
        if (displayValue != null && !displayValue.isEmpty()) return displayValue;

        return null;
    }
}