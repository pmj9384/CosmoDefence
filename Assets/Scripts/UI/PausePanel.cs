using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 일시정지 패널 (UIElement) — 슬롯 채우기/이어하기 로직만. 배치는 씬 소관 (에디터-네이티브).
// GameStop 진입/이탈 시 GameUIManager가 Show/Hide (템플릿 구독 기존재).
// 원작의 드랍 프레임은 제거 (유저 결정 2026-07-28) — 채울 드랍 시스템이 없는 빈 껍데기였고,
// 코인 보상 정보는 결과창(ResultPanel) 몫.
public class PausePanel : UIElement
{
    [Header("씬 참조")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private List<Image> activeIcons;    // Active 슬롯 4칸의 Icon 이미지들
    [SerializeField] private List<Image> passiveIcons;   // Passive 슬롯 2칸
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button combatInfoButton;    // 전투 정보 창 열기 (#12)
    [SerializeField] private Button settingsButton;      // 볼륨 설정창 (아웃게임 이식 SettingsPanel — 씬 수동 배치)
    [SerializeField] private Button lobbyButton;         // 아웃게임(로비) 복귀 (씬 수동 배치)
    [SerializeField] private SkillIconTable iconTable;   // 스킬↔아이콘 (Resources.Load 대체)

    private void Awake()
    {
        resumeButton.onClick.AddListener(Resume);
        combatInfoButton.onClick.AddListener(OpenCombatInfo);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(GoLobby);
    }

    // 전투 정보로 전환 — 상태는 GameStop 유지, 창만 교체 (닫으면 CombatInfoPanel이 퍼즈를 되연다)
    private void OpenCombatInfo()
    {
        Hide();
        gameUIManager.ShowUIElement<CombatInfoPanel>();
    }

    // 설정으로 전환 — CombatInfo와 동일 문법 (상태 GameStop 유지, 닫으면 SettingsPanel이 퍼즈를 되연다)
    private void OpenSettings()
    {
        Hide();
        gameUIManager.ShowUIElement<SettingsPanel>();
    }

    private void GoLobby()
    {
        Time.timeScale = 1f;   // GameStop은 timeScale 0 — 복원 없이 나가면 로비가 얼어붙는다
        SaveLoadSystem.Instance.Save();   // 인게임 설정 변경(볼륨 등)을 즉시 durable — 앱 종료 저장에만 의존하지 않기 (검수 v5 #4)
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public override void Show()
    {
        RefreshSlots();
        overlay.SetActive(true);
    }

    public override void Hide() => overlay.SetActive(false);

    private void Resume() => gameManager.SetGameState(GameManager.GameState.GamePlay);

    // 보유 스킬을 슬롯에 채움 — 아이콘 경로는 SkillTable CSV의 icon 컬럼 (SSOT)
    private void RefreshSlots()
    {
        PlayerSkills skills = gameManager.SkillManager.PlayerSkills;
        FillKind(skills, SkillKind.ActiveBall, activeIcons);
        FillKind(skills, SkillKind.Passive, passiveIcons);
    }

    private void FillKind(PlayerSkills skills, SkillKind kind, List<Image> slots)
    {
        int i = 0;
        foreach (SkillId id in skills.Owned(kind))
        {
            if (i >= slots.Count) break;
            slots[i].enabled = true;
            slots[i].sprite = iconTable.Get(id);
            i++;
        }
        for (; i < slots.Count; i++)   // 빈 슬롯 = 아이콘 숨김 (어두운 안판이 보임 — 선택창과 동일 문법)
            slots[i].enabled = false;
    }
}
