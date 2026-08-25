using UnityEngine;
using UnityEngine.UI;

public class StaminaEmptyPopup : UIPopup
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Hide);
        backgroundButton.onClick.AddListener(Hide);
    }
}
