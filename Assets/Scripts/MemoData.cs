using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

[System.Serializable]
public class MemoData
{
    public int v;
    public string txt;
    public string fc;
    public string bc;

    public string ShortID
    {
        get
        {
            string t = txt ?? "";
            string f = fc ?? "";
            string b = bc ?? "";

            string rawData = $"{t}_{f}_{b}";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return $"{bytes[0]:X2}{bytes[1]:X2}-{bytes[2]:X2}{bytes[3]:X2}";
            }
        }
    }
}

[System.Serializable]
public class HistoryWrapper
{
    public List<MemoData> memoList = new List<MemoData>();
}