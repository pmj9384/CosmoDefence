using System.Collections.Generic;
using UnityEngine;

public class GachaTenResultPopup : GachaResultPopupBase
{
    [SerializeField] private List<GachaResultItemUI> slots;

    public void ShowWithResults(List<(SkinDataTable.SkinRawData skin, bool isNew)> results)
    {
        for (int i = 0; i < Mathf.Min(results.Count, slots.Count); i++)   // 슬롯 배선 부족 시 예외 대신 표시 생략
            slots[i].Setup(results[i].skin, results[i].isNew);
        Show();
    }
}
