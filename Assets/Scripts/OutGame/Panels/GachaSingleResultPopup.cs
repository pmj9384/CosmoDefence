using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaSingleResultPopup : GachaResultPopupBase
{
    [SerializeField] private Image skinIcon;
    [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private GameObject newBadge;

    public void ShowWithResult(SkinDataTable.SkinRawData data, bool isNew)
    {
        skinNameText.text = data.SkinName;
        SkinIconView.Apply(skinIcon, data.SkinId);
        newBadge.SetActive(isNew);
        Show();
    }

}
