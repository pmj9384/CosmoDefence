using System.Collections;
using TMPro;
using UnityEngine;

// 보스 등장 경고 배너 — 예고 신호(WaveManager.OnBossIncoming)를 스스로 구독해 켜지고,
// 깜빡이다 스스로 꺼진다 (아케이드 슈팅 관행). BossHpBar와 무관 — 각자 자기 신호만 (SRP, 유저 확정 2026-07-15)
public class BossWarning : UIElement
{
    [SerializeField] private float flashInterval = 0.25f;
    [SerializeField] private int flashCount = 8;   // 짝수로 끝나야 마지막이 어두운 상태에서 꺼짐 (~2초)

    private TMP_Text text;

    private void Awake() => text = GetComponent<TMP_Text>();

    public override void Initialize()
    {
        gameManager.WaveManager.OnBossIncoming += HandleBossIncoming;
        // 예고 중 게임오버(이전 몹에게 사망) — 스케일 시간 깜빡임이 얼어붙은 채 결과창을 덮는 것 방지
        gameManager.AddGameStateEnterAction(GameManager.GameState.GameOver, Hide);
        gameObject.SetActive(false);
    }

    private void OnDestroy()   // 씬 언로드(재시작) — 매니저 쪽 구독 잔류 방지
    {
        if (gameManager == null) return;
        if (gameManager.WaveManager != null)
            gameManager.WaveManager.OnBossIncoming -= HandleBossIncoming;
        gameManager.RemoveGameStateEnterAction(GameManager.GameState.GameOver, Hide);
    }

    private void HandleBossIncoming() => gameObject.SetActive(true);   // OnEnable이 깜빡임 시작

    public override void Hide() => gameObject.SetActive(false);

    private void OnEnable() => StartCoroutine(Flash());

    private IEnumerator Flash()
    {
        var wait = new WaitForSeconds(flashInterval);   // 스케일 시간 — 퍼즈 중 함께 멈춤
        for (int i = 0; i < flashCount; i++)
        {
            text.alpha = i % 2 == 0 ? 1f : 0.15f;
            yield return wait;
        }
        gameObject.SetActive(false);
    }
}
