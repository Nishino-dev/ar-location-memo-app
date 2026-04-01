using UnityEngine;
using System.Collections.Generic;
using System.IO;
using NativeShareNamespace;

public class QRPrintManager : MonoBehaviour
{
    private readonly int a4Width = 2480;
    private readonly int a4Height = 3508;

    public void GenerateAndShareHistory(List<Texture2D> qrList, int cols, int rows, List<string> idList)
    {
        if (qrList == null || qrList.Count == 0) return;

        int max = cols * rows;
        int totalPages = (qrList.Count + max - 1) / max;
        List<string> paths = new List<string>();

        for (int p = 0; p < totalPages; p++)
        {
            int start = p * max;
            int count = Mathf.Min(max, qrList.Count - start);

            Texture2D sheet = CreateOnePage(qrList.GetRange(start, count), cols, rows, idList.GetRange(start, count));
            string path = Path.Combine(Application.temporaryCachePath, $"qr_page_{p}.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            paths.Add(path);
            Destroy(sheet);
        }

        NativeShare share = new NativeShare();
        paths.ForEach(p => share.AddFile(p));
        share.Share();
    }

    private Texture2D CreateOnePage(List<Texture2D> pageQrs, int cols, int rows, List<string> idList)
    {
        Texture2D sheet = new Texture2D(a4Width, a4Height, TextureFormat.RGB24, false);
        Color[] pixels = new Color[a4Width * a4Height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

        int sideMargin = 150, hSpacing = 80, textSpace = 100;
        int qrSize = (a4Width - (sideMargin * 2) - (hSpacing * (cols - 1))) / cols;
        int blockH = qrSize + textSpace;
        int vGap = (a4Height - (blockH * rows)) / (rows + 1);
        int startX = (a4Width - ((cols * qrSize) + ((cols - 1) * hSpacing))) / 2;

        for (int i = 0; i < pageQrs.Count; i++)
        {
            int c = i % cols, r = i / cols;
            int px = startX + (c * (qrSize + hSpacing));
            int py = a4Height - (vGap + (r * (blockH + vGap))) - qrSize;

            Color[] qrPixels = GetResizedPixels(pageQrs[i], qrSize);
            for (int y = 0; y < qrSize; y++)
            {
                if (py + y >= 0 && py + y < a4Height)
                    System.Array.Copy(qrPixels, y * qrSize, pixels, (py + y) * a4Width + px, qrSize);
            }

            if (i < idList.Count) DrawID(pixels, idList[i], px + (qrSize / 2), py - 60, 6);
        }

        sheet.SetPixels(pixels);
        sheet.Apply();
        return sheet;
    }

    private Color[] GetResizedPixels(Texture2D original, int size)
    {
        RenderTexture rt = RenderTexture.GetTemporary(size, size);
        Graphics.Blit(original, rt);
        RenderTexture.active = rt;
        Texture2D temp = new Texture2D(size, size);
        temp.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        temp.Apply();
        Color[] p = temp.GetPixels();
        RenderTexture.ReleaseTemporary(rt);
        Destroy(temp);
        return p;
    }

    private readonly Dictionary<char, byte[]> fontData = new Dictionary<char, byte[]> {
        {'0',new byte[]{0x7E,0x81,0x81,0x81,0x7E}},{'1',new byte[]{0x00,0x41,0xFF,0x01,0x00}},
        {'2',new byte[]{0x41,0x87,0x89,0x91,0x61}},{'3',new byte[]{0x42,0x81,0x91,0x91,0x6E}},
        {'4',new byte[]{0x18,0x28,0x48,0xFF,0x08}},{'5',new byte[]{0xF2,0x91,0x91,0x91,0x8E}},
        {'6',new byte[]{0x7E,0x91,0x91,0x91,0x4E}},{'7',new byte[]{0x80,0x8F,0x90,0xA0,0xC0}},
        {'8',new byte[]{0x6E,0x91,0x91,0x91,0x6E}},{'9',new byte[]{0x72,0x89,0x89,0x89,0x7E}},
        {'A',new byte[]{0x7F,0x88,0x88,0x88,0x7F}},{'B',new byte[]{0xFF,0x91,0x91,0x91,0x6E}},
        {'C',new byte[]{0x7E,0x81,0x81,0x81,0x42}},{'D',new byte[]{0xFF,0x81,0x81,0x42,0x3C}},
        {'E',new byte[]{0xFF,0x91,0x91,0x91,0x81}},{'F',new byte[]{0xFF,0x90,0x90,0x90,0x80}},{'-',new byte[]{0x10,0x10,0x10,0x10,0x10}}
    };

    private void DrawID(Color[] canvas, string id, int cx, int py, int s)
    {
        int totalW = (id.Length * 5 * s) + ((id.Length - 1) * 2 * s);
        int sx = cx - (totalW / 2);

        for (int i = 0; i < id.Length; i++)
        {
            char c = char.ToUpper(id[i]);
            if (!fontData.ContainsKey(c)) continue;
            byte[] g = fontData[c];
            int charX = sx + (i * 7 * s);

            for (int gx = 0; gx < 5; gx++)
            {
                for (int gy = 0; gy < 8; gy++)
                {
                    if (((g[gx] >> gy) & 1) == 1)
                    {
                        for (int ox = 0; ox < s; ox++)
                        {
                            for (int oy = 0; oy < s; oy++)
                            {
                                int dx = charX + (gx * s) + ox, dy = py + (gy * s) + oy;
                                if (dx >= 0 && dx < a4Width && dy >= 0 && dy < a4Height) canvas[dy * a4Width + dx] = Color.black;
                            }
                        }
                    }
                }
            }
        }
    }
}