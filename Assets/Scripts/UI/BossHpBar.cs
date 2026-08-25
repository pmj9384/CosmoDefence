using UnityEngine;
using UnityEngine.UI;

// 화면 상단 보스 전용 HP바 (AnimalBreakOut 관찰 2026-07-15: 보스 진입 시 일반 진행 UI와 "교대" —
// StageManager.OnBossStageEnter가 runStageUI.Hide()+bossWayUI.Show(), Clear 시 반대).
// 이 컴포넌트는 보스 바 표시/갱신만 담당 — 일반 게이지 숨김은 소유자인 InGameHud가 같은 이벤트로 처리.
// 종료 신호는 MonsterManager.OnBossEnded 하나 — 격파·게임오버 정리 양 경로를 매니저가 묶어준다.
public class BossHpBar : UIElement
{
    [SerializeField] private Slider hpSlider;

    private Monster boss;   // 표시 중 보스 — HP 구독 해제용

    public override void Initialize()
    {
        gameManager.MonsterManager.OnBossSpawned += HandleBossSpawned;
        gameManager.MonsterManager.OnBossEnded += HandleBossEnded;
        gameObject.SetActive(false);
    }

    private void OnDestroy()   // 씬 언로드(재시작) — 매니저 쪽 구독 잔류 방지
    {
        if (gameManager == null || gameManager.MonsterManager == null) return;
        gameManager.MonsterManager.OnBossSpawned -= HandleBossSpawned;
        gameManager.MonsterManager.OnBossEnded -= HandleBossEnded;
    }

    private void HandleBossSpawned(Monster monster)
    {
        boss = monster;
        boss.OnHpChanged += HandleHpChanged;   // 구독/해제 쌍: HandleBossEnded에서 해제
        hpSlider.value = 1f;
        gameObject.SetActive(true);
    }

    private void HandleHpChanged(int current, int max) => hpSlider.value = (float)current / max;

    private void HandleBossEnded()
    {
        if (boss != null)
        {
            boss.OnHpChanged -= HandleHpChanged;
            boss = null;
        }
        gameObject.SetActive(false);
    }

    public override void Hide() => HandleBossEnded();
}
