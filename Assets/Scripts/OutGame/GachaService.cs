using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GachaService
{
    public const int DrawOneCost = 100;
    public const int DrawTenCost = 900;

    public static bool CanDrawOne() => GameDataManager.Instance.PlayerAccountData.Coins >= DrawOneCost;
    public static bool CanDrawTen() => GameDataManager.Instance.PlayerAccountData.Coins >= DrawTenCost;

    public static (SkinDataTable.SkinRawData skin, bool isNew) DrawOne()
    {
        if (!CanDrawOne())
        {
            return (null, false);
        }
        GameDataManager.Instance.PlayerAccountData.SpendCoin(DrawOneCost);
        var skin = WeightedRandom();
        bool isNew = GameDataManager.Instance.SkinUserData.Unlock(skin.SkinId);
        SaveLoadSystem.Instance.Save();   // 구매는 즉시 durable — 뽑기 직후 강제종료 시 롤백 방지 (게임오버 정산과 같은 정책, 검수 v5 #3)
        return (skin, isNew);
    }
    public static List<(SkinDataTable.SkinRawData skin, bool isNew)> DrawTen()
    {
        if (!CanDrawTen()) return null;

        GameDataManager.Instance.PlayerAccountData.SpendCoin(DrawTenCost);
        var results = new List<(SkinDataTable.SkinRawData, bool)>();

        for (int i = 0; i < 10; i++)
        {
            var skin = WeightedRandom();
            bool isNew = GameDataManager.Instance.SkinUserData.Unlock(skin.SkinId);
            results.Add((skin, isNew));
        }
        SaveLoadSystem.Instance.Save();   // 10연도 동일 — 구매 즉시 durable (검수 v5 #3)

        return results;
    }
    private static SkinDataTable.SkinRawData WeightedRandom()
    {
        var skins = DataTableManager.SkinDataTable.All;
        float total = 0f;
        foreach (var s in skins)
        {
            total += s.GachaRate;
        }
        float rand = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var s in skins)
        {
            cumulative += s.GachaRate;
            if (rand < cumulative) return s;
        }
        return skins.Last();
    }

}
