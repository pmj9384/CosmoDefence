using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinItemUI : MonoBehaviour
{
    [SerializeField] private Image skinImage;
    [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private TMP_Text gradeText;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;

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
        bool isEquipped = GameDataManager.Instance.SkinUserData.EquippedSkinId == skinId;
        if (GameDataManager.Instance.SkinUserData.IsOwned(skinId) == false)
        {
            equipButton.interactable = false;
            equipButtonText.text = "잠금";
        }
        else
        {
            equipButton.interactable = !isEquipped;
            equipButtonText.text = isEquipped ? "장착중" : "장착";
        }
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
