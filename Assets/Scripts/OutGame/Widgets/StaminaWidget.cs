using TMPro;

public class StaminaWidget : UIWidget
{
    private TMP_Text staminaText;

    private void Awake() => staminaText = GetComponent<TMP_Text>();

    protected override void Subscribe()
    {
        GameDataManager.Instance.StaminaSystem.onStaminaChanged += UpdateUI;
        Refresh();
    }

    protected override void Unsubscribe()
    {
        if (!GameDataManager.HasInstance) return;   // 씬 teardown — Instance 접근이 죽은 싱글톤을 재생성하므로 금지
        GameDataManager.Instance.StaminaSystem.onStaminaChanged -= UpdateUI;
    }

    public override void Refresh()
        => UpdateUI(GameDataManager.Instance.StaminaSystem.CurrentStamina);

    private void UpdateUI(int stamina)
        => staminaText.text = $"{stamina}/{StaminaSystem.MaxStamina}";
}
