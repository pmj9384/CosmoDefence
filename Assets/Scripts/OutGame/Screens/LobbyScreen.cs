using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyScreen : UIScreen
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;

    public override void Open()
    {
        base.Open();
        playButton.onClick.RemoveAllListeners();
        optionsButton.onClick.RemoveAllListeners();

        playButton.onClick.AddListener(() =>
        {
            if (!GameDataManager.Instance.StaminaSystem.TryConsumeStamina())
            {
                uiManager.ShowPopup<StaminaEmptyPopup>();
                return;
            }
            GameManager.SkipTitle = true;
            SaveLoadSystem.Instance.Save();
            SceneManager.LoadScene("InGameScene");
        });
        optionsButton.onClick.AddListener(() => uiManager.ShowPopup<OutGameSettingsPanel>());
    }
}
