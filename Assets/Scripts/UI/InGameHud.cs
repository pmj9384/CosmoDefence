using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 상단 HUD — 값 갱신 로직만. 배치/색/크기는 전부 씬(Inspector) 소관 (에디터-네이티브 전환, 2026-07-06).
// 텍스트는 변경 감지 캐시로 값이 바뀐 프레임에만 할당(GC 회피), 게이지는 목표값으로 부드럽게 차오름 (원작 관찰).
// UIElement 상속: 템플릿 의미는 "GameUIManager가 주입·관리하는 UI 단위" — 상시 표시라 Show/Hide는 미오버라이드
public class InGameHud : UIElement
{
    [Header("씬 참조 (TopBar 아래 조립됨)")]
    [SerializeField] private Slider progressSlider;   // 진행도 게이지 = 다음 보스까지 (핸들 없는 Slider)
    [SerializeField] private TMP_Text progressText;   // "N%"
    [SerializeField] private TMP_Text scoreText;      // 점수 (가중: 잡몹10/보스100) — 씬에서 신규 연결
    [SerializeField] private Slider levelSlider;      // 레벨(킬) 게이지
    [SerializeField] private GameObject levelBadge;   // ◆ 다이아 (선택창 동안 함께 숨김)
    [SerializeField] private TMP_Text levelText;      // "Lv.N"
    [SerializeField] private Button pauseButton;

    [SerializeField] private float gaugeFillSpeed = 1.5f;   // 게이지가 목표값까지 차오르는 속도(비율/초) [눈튜닝]
    [SerializeField] private Color levelFillColor = new(0.95f, 0.6f, 0.15f);   // 평상시 주황
    [SerializeField] private Color levelFullColor = new(0.9f, 0.25f, 0.15f);   // 꽉 참 = 빨강 (원작 레벨업 바)

    private int lastProgressPercent = -1;
    private int lastScore = -1;
    private int lastLevel = -1;
    private int lastKillsIntoLevel = -1;
    private float progressTarget;
    private float levelTarget;
    private bool fillingToFull;   // 레벨업 연출: 게이지를 먼저 꽉 채우는 중
    private Image levelFill;      // 색 전환용 (Slider fillRect에서 획득)
    private bool bossActive;      // 보스 동안 진행도·레벨 게이지 숨김 ↔ 보스 HP바 교대 (AnimalBreakOut 패턴)

    private void Start()   // 매니저 Initialize 완료 후 (관례)
    {
        pauseButton.onClick.AddListener(TogglePause);
        levelFill = levelSlider.fillRect.GetComponent<Image>();
    }

    public override void Initialize()
    {
        gameManager.MonsterManager.OnBossSpawned += HandleBossSpawned;
        gameManager.MonsterManager.OnBossEnded += HandleBossEnded;
    }

    private void OnDestroy()
    {
        if (gameManager == null || gameManager.MonsterManager == null) return;
        gameManager.MonsterManager.OnBossSpawned -= HandleBossSpawned;
        gameManager.MonsterManager.OnBossEnded -= HandleBossEnded;
    }

    private void HandleBossSpawned(Monster _) => bossActive = true;
    private void HandleBossEnded() => bossActive = false;

    private void Update()
    {
        var skillManager = gameManager.SkillManager;
        var waveManager = gameManager.WaveManager;
        if (skillManager == null || skillManager.PlayerLevel == null) return;

        // 레벨 UI 숨김 = 스킬 선택(선택창 바가 대신) 또는 보스(보스 HP바와 교대 — 유저 확정 2026-07-15)
        bool hideLevel = gameManager.CurrentState == GameManager.GameState.SkillSelection || bossActive;
        if (levelSlider.gameObject.activeSelf == hideLevel)
        {
            levelSlider.gameObject.SetActive(!hideLevel);
            levelText.gameObject.SetActive(!hideLevel);
            if (levelBadge != null) levelBadge.SetActive(!hideLevel);   // 배선 전이어도 나머지는 동작
        }

        // 진행도 게이지도 보스 동안 숨김 (보스까지 진행도가 어차피 0 — 자리는 보스 HP바가 사용)
        if (progressSlider.gameObject.activeSelf == bossActive)
        {
            progressSlider.gameObject.SetActive(!bossActive);
            progressText.gameObject.SetActive(!bossActive);
        }

        // 진행도 = 다음 보스까지 (무한모드: 유한 처치%는 오버슛하므로 보스 관문 카운트로 대체)
        float bossProgress = waveManager.BossProgress;
        int percent = Mathf.RoundToInt(bossProgress * 100);
        if (percent != lastProgressPercent)
        {
            lastProgressPercent = percent;
            progressTarget = bossProgress;
            progressText.text = $"{percent}%";
        }

        // 점수 (가중) — StatsManager가 SSOT. 값 바뀐 프레임에만 할당(GC 회피)
        StatsManager stats = gameManager.StatsManager;
        if (scoreText != null && stats != null && stats.Score != null && stats.Score.Current != lastScore)
        {
            lastScore = stats.Score.Current;
            scoreText.text = $"Score: {stats.Score.Current:N0}";   // 라벨 포함 — 숫자만 있으면 뭔지 안 읽힘 (유저 2026-07-27)
        }

        // 레벨 게이지 = 다음 레벨까지의 킬 진행 [가정A]
        PlayerLevel level = skillManager.PlayerLevel;
        if (level.Level != lastLevel || level.KillsIntoLevel != lastKillsIntoLevel)
        {
            // 첫 동기화(lastLevel == -1)는 레벨업이 아님 — 연출 없이 값만 맞춤 (시작 시 꽉 차던 버그)
            if (level.Level != lastLevel && lastLevel != -1)
                fillingToFull = true;   // 레벨업: 먼저 "꽉" 채우는 연출 — 리셋은 가득 찬 뒤 (원작 시퀀스)

            lastLevel = level.Level;
            lastKillsIntoLevel = level.KillsIntoLevel;
            levelText.text = $"Lv.{level.Level}";
        }
        levelTarget = fillingToFull ? 1f : (float)level.KillsIntoLevel / Mathf.Max(1, level.KillsToNext);

        // 부드럽게 차오르는 연출 (원작) — 목표값은 즉시, 표시는 보간
        progressSlider.value = Mathf.MoveTowards(progressSlider.value, progressTarget, gaugeFillSpeed * Time.deltaTime);
        levelSlider.value = Mathf.MoveTowards(levelSlider.value, levelTarget, gaugeFillSpeed * Time.deltaTime);
        // 꽉 차면 빨갛게 — HUD 바 하나가 "레벨업 바" 역할까지 (선택창 전용 바 없음, 유저 확정)
        levelFill.color = levelSlider.value >= 0.999f ? levelFullColor : levelFillColor;

        // 꽉 찼으면 0으로 스냅 후 이월분부터 다시 참 — 스킬 선택(정지) 동안엔 가득 찬 채 유지되고,
        // 선택을 마치고 재개된 첫 프레임에 리셋된다 ("선택하면 0으로" — 유저 확정)
        if (fillingToFull && levelSlider.value >= 0.999f &&
            gameManager.CurrentState == GameManager.GameState.GamePlay)
        {
            fillingToFull = false;
            levelSlider.value = 0f;
        }
    }

    private void TogglePause()
    {
        // GamePlay에서만 진입 — 스킬 선택/결과 중 이중 정지 방지
        if (gameManager.CurrentState == GameManager.GameState.GamePlay)
            gameManager.SetGameState(GameManager.GameState.GameStop);
    }
}
