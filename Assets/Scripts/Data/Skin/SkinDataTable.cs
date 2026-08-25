using System;
using System.Collections.Generic;
using UnityEngine;

public class SkinDataTable : DataTable
{
    [Serializable]
    public class SkinRawData
    {
        public string SkinId { get; set; }
        public string SkinName { get; set; }
        public string Grade { get; set; }
        public float GachaRate { get; set; }
        public bool IsDefault { get; set; }
    }

    private readonly Dictionary<string, SkinRawData> table = new();

    public IReadOnlyCollection<SkinRawData> All => table.Values;

    public override void Load(string filename)
    {
        var path = string.Format(FormatPath, filename);
        var textAsset = Resources.Load<TextAsset>(path);
        var list = LoadCSV<SkinRawData>(textAsset.text);
        table.Clear();
        foreach (var data in list)
        {
            if (!table.ContainsKey(data.SkinId))
                table.Add(data.SkinId, data);
            else
                Debug.LogError($"SkinDataTable 중복 키: {data.SkinId}");
        }
    }

    public SkinRawData Get(string skinId)
    {
        table.TryGetValue(skinId, out var data);
        return data;
    }
}
