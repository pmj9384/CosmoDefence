using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : UIElement
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeButton;

    public override void Initialize()
    {
        gameObject.SetActive(false);
        closeButton.onClick.AddListener(Close);
        bgmSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetBgmVolume(v));
        sfxSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetSfxVolume(v));
    }

    // 닫으면 퍼즈로 복귀 — CombatInfoPanel과 동일 문법 (퍼즈에서 열리는 창, 상태는 GameStop 유지) [이식 개조]
    private void Close()
    {
        Hide();
        gameUIManager.ShowUIElement<PausePanel>();
    }

    public override void Show()
    {
        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);   // TongTong SoundManager는 PascalCase (이식 개조)
        sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
        gameObject.SetActive(true);
    }

    public override void Hide() => gameObject.SetActive(false);
}
