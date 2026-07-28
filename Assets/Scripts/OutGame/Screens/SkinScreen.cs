using System.Collections.Generic;
using UnityEngine;

public class SkinScreen : UIScreen
{
    [SerializeField] private Transform ownedParent;
    [SerializeField] private Transform lockedParent;
    [SerializeField] private GameObject lockedHeader;   // 미해금이 하나도 없으면 구분 헤더까지 감춘다
    [SerializeField] private SkinItemUI skinItemPrefab;

    private readonly List<SkinItemUI> items = new();

    public override void Open()
    {
        base.Open();
        BuildList();
        GameDataManager.Instance.SkinUserData.OnSkinEquipped += OnSkinEquipped;
    }

    public override void Close()
    {
        base.Close();
        if (!GameDataManager.HasInstance) return;   // 부팅 Setup/teardown — Instance 접근이 초기화 전 생성을 유발하므로 금지
        GameDataManager.Instance.SkinUserData.OnSkinEquipped -= OnSkinEquipped;
    }

    // 화면을 열 때마다 목록을 통째로 다시 만든다 — 뽑기로 새로 얻은 스킨이 미해금 구역에서 보유 구역으로
    // 옮겨오는 것도 이 재생성이 처리하므로, 해금 순간 오브젝트를 갈아끼우는 로직이 따로 필요 없다.
    private void BuildList()
    {
        foreach (var item in items)
            Destroy(item.gameObject);
        items.Clear();

        SkinUserData skinUserData = GameDataManager.Instance.SkinUserData;
        int lockedCount = 0;

        foreach (var data in DataTableManager.SkinDataTable.All)
        {
            bool isOwned = skinUserData.IsOwned(data.SkinId);
            if (!isOwned) lockedCount++;

            var item = Instantiate(skinItemPrefab, isOwned ? ownedParent : lockedParent);
            item.Setup(data);
            items.Add(item);
        }

        lockedHeader.SetActive(lockedCount > 0);
    }

    private void OnSkinEquipped(string _)
    {
        foreach (var item in items)
            item.Refresh();
    }
}
