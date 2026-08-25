using UnityEngine;

public static class DataTableManager
{
    private static SkinDataTable _skinDataTable;
    public static SkinDataTable SkinDataTable => _skinDataTable ??= LoadTable<SkinDataTable>("SkinData");

    private static T LoadTable<T>(string filename) where T : DataTable, new()
    {
        var table = new T();
        table.Load(filename);
        return table;
    }
}
