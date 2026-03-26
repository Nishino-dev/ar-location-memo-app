using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MemoData
{
    public int v;      // Version
    public string txt;    // Text Content
    public string fc;     // Font Color (Hex: #FFFFFF)
    public string bc;     // Background Color (Hex: #000000)
}

[System.Serializable]
public class HistoryWrapper
{
    public List<MemoData> memoList = new List<MemoData>();
}