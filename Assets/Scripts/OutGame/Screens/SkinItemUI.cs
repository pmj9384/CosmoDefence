using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinItemUI : MonoBehaviour
{
    [SerializeField] private Image cardBackground;   // 카드 배경(루트) — 미해금이면 어둡게 죽인다
    [SerializeField] private Image skinImage;
    [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private TMP_Text gradeText;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;

    // 미해금 카드는 배경 스프라이트를 곱연산으로 죽여 "아직 없는 것"으로 읽히게 한다
    private static readonly Color LockedCardTint = new Color32(0x6E, 0x74, 0x82, 0xFF);

    private string skinId;

    public void Setup(SkinDataTable.SkinRawData data)
    {
        skinId = data.SkinId;
        skinNameText.text = data.SkinName;
        gradeText.text = data.Grade;
        SkinIconView.Apply(skinImage, data.SkinId);

        equipButton.onClick.AddListener(OnEquipClicked);
        Refresh();
    }

    public void Refresh()
    {
        SkinUserData skinUserData = GameDataManager.Instance.SkinUserData;
        bool isOwned = skinUserData.IsOwned(skinId);

        // 미해금은 버튼을 통째로 감춘다 — 눌러도 아무 일 없는 버튼이 터치 타겟으로 남지 않게
        equipButton.gameObject.SetActive(isOwned);
        cardBackground.color = isOwned ? Color.white : LockedCardTint;
        if (!isOwned) return;

        bool isEquipped = skinUserData.EquippedSkinId == skinId;
        equipButton.interactable = !isEquipped;
        equipButtonText.text = isEquipped ? "장착중" : "장착";
    }

    private void OnEquipClicked()
    {
        GameDataManager.Instance.SkinUserData.Equip(skinId);
    }

    private void OnDestroy()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveAllListeners();
    }
}
