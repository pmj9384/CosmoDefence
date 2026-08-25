using UnityEngine;

// 인게임 수치 집계의 단일 창구 (AnimalBreakOut InGameCountManager 패턴) —
// 이벤트로 흘러오는 카운트류는 전부 여기로 모은다. 계산 규칙은 순수 코어(CombatStats) 몫.
// UI(전투 정보 창)는 GameManager.StatsManager 한 곳만 본다.
public class StatsManager : InGameManager
{
    [SerializeField] private int killPoints = 10;    // 잡몹 1처치 점수 (튜닝)
    [SerializeField] private int bossPoints = 100;   // 보스 1처치 점수 (튜닝)
    [SerializeField] private int scorePerCoin = 5;   // 점수 N점 = 1코인 — 판당 ~1뽑기 페이스 [가정, 튜닝]

    public CombatStats Combat { get; private set; }
    public ScoreCounter Score { get; private set; }   // 이번 런 점수
    // 최고기록 SSOT = PlayerAccountData (아웃게임 이식으로 세이브 체인 도입 — PlayerPrefs에서 이관 2026-07-15)
    public int Best => GameDataManager.Instance.PlayerAccountData.BestScore;
    public bool IsNewRecord { get; private set; }     // 이번 런이 최고 갱신했는지 (결과창 표시용)
    public int CoinsEarned { get; private set; }      // 이번 런 코인 보상 (점수 비례, 게임오버 지급 — 결과창 표시용)

    // 전투 경과(초) — Time.time은 timeScale을 타므로 정지(선택/퍼즈/결과) 시간이 자동 제외된다 [가정, 원작 DPS 감각]
    public float CombatElapsed => Time.time - combatStartTime;

    private float combatStartTime;

    public override void Initialize()
    {
        base.Initialize();
        Combat = new CombatStats();
        Score = new ScoreCounter();       // 매 런 새로 — RestartGame 시 0부터
        IsNewRecord = false;
        combatStartTime = Time.time;

        GameManager.MonsterManager.OnDamageDealt += HandleDamageDealt;
        GameManager.MonsterManager.OnMonsterKilled += HandleMonsterKilled;   // 카운트 허브 관례 — 점수 적립
        GameManager.AddGameStateEnterAction(GameManager.GameState.GameOver, SubmitScore);
    }

    public override void Clear()
    {
        base.Clear();
        GameManager.MonsterManager.OnDamageDealt -= HandleDamageDealt;
        GameManager.MonsterManager.OnMonsterKilled -= HandleMonsterKilled;
        GameManager.RemoveGameStateEnterAction(GameManager.GameState.GameOver, SubmitScore);
    }

    private void HandleDamageDealt(SkillId? source, int damage) => Combat.Add(source, damage);

    // 보스 판별 = BossController 유무(MonsterManager.ReleaseMonster와 동일 판별자). 보스 100 / 잡몹 10
    private void HandleMonsterKilled(Monster monster)
        => Score.Add(monster.GetComponent<BossController>() != null ? bossPoints : killPoints);

    // GameOver 진입 시 정산: 코인 지급(점수 비례, 항상) + 최고기록 갱신 (ResultPanel은 딜레이 후 읽으므로 순서 안전)
    private void SubmitScore()
    {
        var account = GameDataManager.Instance.PlayerAccountData;
        CoinsEarned = scorePerCoin > 0 ? Score.Current / scorePerCoin : 0;
        account.AddCoins(CoinsEarned);
        IsNewRecord = account.TryUpdateBestScore(Score.Current);
        SaveLoadSystem.Instance.Save();   // 코인·기록 즉시 파일 저장 (모바일 강제종료 대비 — 게임오버는 드문 이벤트)
    }
}
