using UnityEngine;
using System.Collections.Generic;
using System.IO;
using NativeShareNamespace;

public class QRPrintManager : MonoBehaviour
{
    public int a4Width = 2480;
    public int a4Height = 3508;
    private int cols = 2;
    private int rows = 3;

    [Range(0.0f, 0.3f)] public float innerMarginRatio = 0.1f;
    [Range(0.0f, 0.5f)] public float textHeightRatio = 0.15f;
    public int cutLineLength = 40;
    public int cutLineWidth = 2;
    public int dashStep = 10;

    [Header("Print Settings")]
    public int printSafetyMargin = 100;

    public void GenerateAndShareHistory(List<Texture2D> qrList, int c, int r, List<string> labels)
    {
        this.cols = c;
        this.rows = r;
        int maxPerPage = cols * rows;
        int totalPages = (qrList.Count + maxPerPage - 1) / maxPerPage;
        List<string> filePaths = new List<string>();

        for (int p = 0; p < totalPages; p++)
        {
            int start = p * maxPerPage;
            int count = Mathf.Min(maxPerPage, qrList.Count - start);
            Texture2D sheet = GeneratePrintTexture(qrList.GetRange(start, count), labels.GetRange(start, count));

            string path = Path.Combine(Application.temporaryCachePath, $"qr_page_{p}.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            filePaths.Add(path);

            if (Application.isPlaying) Destroy(sheet);
            else DestroyImmediate(sheet);
        }

        NativeShare share = new NativeShare();
        filePaths.ForEach(p => share.AddFile(p));
        share.Share();
    }

    public Texture2D GeneratePrintTexture(List<Texture2D> qrList, List<string> labels)
    {
        Texture2D tex = new Texture2D(a4Width, a4Height, TextureFormat.RGB24, false);
        Color32[] bg = new Color32[a4Width * a4Height];
        for (int i = 0; i < bg.Length; i++) bg[i] = Color.white;
        tex.SetPixels32(bg);

        int usableW = a4Width - (printSafetyMargin * 2);
        int usableH = a4Height - (printSafetyMargin * 2);

        int cellW = usableW / cols;
        int cellH = usableH / rows;

        int totalGridW = cellW * cols;
        int totalGridH = cellH * rows;

        int offsetX = printSafetyMargin + (usableW - totalGridW) / 2;
        int offsetY = printSafetyMargin + (usableH - totalGridH) / 2;

        int shortSide = Mathf.Min(cellW, cellH);
        int innerMargin = Mathf.RoundToInt(shortSide * innerMarginRatio);
        int contentH = cellH - (innerMargin * 2);
        int textH = Mathf.RoundToInt(contentH * textHeightRatio);
        int qrSize = Mathf.Min(cellW - (innerMargin * 2), contentH - textH);

        DrawAllGridLines(tex, offsetX, offsetY, cellW, cellH);

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (index >= qrList.Count) break;

                int baseX = offsetX + (c * cellW);
                int baseY = offsetY + ((rows - 1 - r) * cellH);

                int qrX = baseX + (cellW - qrSize) / 2;
                int qrY = baseY + (cellH - (qrSize + textH)) / 2 + textH;

                DrawQR(tex, qrList[index], qrX, qrY, qrSize);

                int textCenterX = baseX + (cellW / 2);
                int textY = baseY + (cellH - (qrSize + textH)) / 2 + (textH / 2) - (4 * 5 / 2);
                DrawID(tex, labels[index], textCenterX, textY, 5);

                index++;
            }
        }
        tex.Apply();
        return tex;
    }

    void DrawAllGridLines(Texture2D tex, int startX, int startY, int cw, int ch)
    {
        int endX = startX + cw * cols;
        int endY = startY + ch * rows;

        for (int c = 0; c <= cols; c++)
        {
            int x = startX + (c * cw);
            DrawDashedLine(tex, x, startY, x, endY);
        }
        for (int r = 0; r <= rows; r++)
        {
            int y = startY + (r * ch);
            DrawDashedLine(tex, startX, y, endX, y);
        }
    }

    void DrawDashedLine(Texture2D tex, int x1, int y1, int x2, int y2)
    {
        int dx = Mathf.Abs(x2 - x1), dy = Mathf.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        int stepCount = 0;

        while (true)
        {
            if ((stepCount / dashStep) % 2 == 0)
            {
                for (int i = -cutLineWidth; i <= cutLineWidth; i++)
                    for (int j = -cutLineWidth; j <= cutLineWidth; j++)
                    {
                        int px = x1 + i, py = y1 + j;
                        if (px >= 0 && px < a4Width && py >= 0 && py < a4Height)
                            tex.SetPixel(px, py, Color.gray);
                    }
            }
            if (x1 == x2 && y1 == y2) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x1 += sx; }
            if (e2 < dx) { err += dx; y1 += sy; }
            stepCount++;
        }
    }

    void DrawQR(Texture2D canvas, Texture2D qr, int x, int y, int size)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int srcX = i * qr.width / size;
                int srcY = j * qr.height / size;
                canvas.SetPixel(x + i, y + j, qr.GetPixel(srcX, srcY));
            }
        }
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

    void DrawID(Texture2D tex, string id, int cx, int py, int s)
    {
        if (string.IsNullOrEmpty(id)) return;
        int totalW = (id.Length * 5 * s) + ((id.Length - 1) * 2 * s);
        int sx = cx - (totalW / 2);
        for (int i = 0; i < id.Length; i++)
        {
            char c = char.ToUpper(id[i]);
            if (!fontData.ContainsKey(c)) continue;
            byte[] g = fontData[c];
            int charX = sx + (i * 7 * s);
            for (int gx = 0; gx < 5; gx++)
                for (int gy = 0; gy < 8; gy++)
                    if (((g[gx] >> gy) & 1) == 1)
                        for (int ox = 0; ox < s; ox++)
                            for (int oy = 0; oy < s; oy++)
                            {
                                int dx = charX + (gx * s) + ox;
                                int dy = py + (gy * s) + oy;
                                if (dx >= 0 && dx < a4Width && dy >= 0 && dy < a4Height) tex.SetPixel(dx, dy, Color.black);
                            }
        }
    }
}