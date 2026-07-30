using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 정보 창 (원작 #57) — 소스(볼/성냥)별 [아이콘·◆레벨·이름·누적·최대 대비 비율 바·DPS] 리스트.
// 퍼즈의 통계 버튼으로 열리고, 아무 곳 터치로 닫혀 퍼즈로 복귀 (상태는 GameStop 유지).
// 행은 빌더가 최대 개수만큼 사전 조립 — 이 스크립트는 데이터 채움/표시만.
public class CombatInfoPanel : UIElement
{
    [Header("씬 참조 (빌더 조립)")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private Button closeButton;        // 전체 화면 터치 영역
    [SerializeField] private TMP_Text progressText;     // "N%"
    [SerializeField] private Slider progressSlider;
    [SerializeField] private GameObject[] rows;          // 행 루트 (최대 7: 노멀+액티브4+성냥+여유)
    [SerializeField] private Image[] icons;
    [SerializeField] private TMP_Text[] levels;          // "◆x N"
    [SerializeField] private TMP_Text[] names;
    [SerializeField] private TMP_Text[] totals;          // 콤마 포맷
    [SerializeField] private Slider[] ratios;            // 최대 누적 대비
    [SerializeField] private TMP_Text[] dpsTexts;
    [SerializeField] private SkillIconTable iconTable;   // 스킬↔아이콘 (Resources.Load 대체)

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
    }

    public override void Show()
    {
        Refresh();
        overlay.SetActive(true);
    }

    public override void Hide() => overlay.SetActive(false);

    private void Close()
    {
        Hide();
        gameUIManager.ShowUIElement<PausePanel>();   // 퍼즈로 복귀
    }

    private void Refresh()
    {
        var stats = gameManager.StatsManager.Combat;
        var skills = gameManager.SkillManager.PlayerSkills;
        float elapsed = gameManager.StatsManager.CombatElapsed;

        // 진행도 = 다음 보스까지 (HUD와 같은 정의 — 무한모드 처치% 오버슛 대체)
        float bossProgress = gameManager.WaveManager.BossProgress;
        progressText.text = $"{Mathf.RoundToInt(bossProgress * 100)}%";
        progressSlider.value = bossProgress;

        // 행 구성: 노멀(항상) → 보유 액티브 → 성냥(보유 시 — 부가 피해도 소스 집계, 원작 확인)
        var sources = new List<(SkillId? id, string name, int level)>
        {
            (null, "노멀 볼", gameManager.SkillManager.NormalBallLevel),
        };
        foreach (SkillId id in skills.Owned(SkillKind.ActiveBall))
            sources.Add((id, skills.Table[id].displayName, skills.GetLevel(id)));
        if (skills.Has(SkillId.LastMatch))
            sources.Add((SkillId.LastMatch, skills.Table[SkillId.LastMatch].displayName,
                         skills.GetLevel(SkillId.LastMatch)));

        long maxTotal = stats.MaxTotal;
        for (int i = 0; i < rows.Length; i++)
        {
            bool active = i < sources.Count;
            rows[i].SetActive(active);
            if (!active) continue;

            var (id, displayName, level) = sources[i];
            long damage = stats.TotalOf(id);

            icons[i].sprite = iconTable.Get(id ?? SkillId.NormalBall);
            levels[i].text = $"◆x{level}";
            names[i].text = displayName;
            totals[i].text = damage.ToString("N0");                    // 2,209 포맷 (원작)
            ratios[i].value = maxTotal > 0 ? (float)damage / maxTotal : 0f;
            dpsTexts[i].text = CombatStats.Dps(damage, elapsed).ToString();
        }
    }
}
