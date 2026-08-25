using System.Collections;
using UnityEngine;

// 행 컨베이어 (원작 확정 관찰 2026-07-07): 시작 5행 일괄 → "앞 행이 한 칸 내려올 때마다" 다음 행 스폰.
// 행 간격을 시간이 아니라 거리(한 칸)로 정의 — 줄들이 격자 간격을 유지하고, 세로 1×2의 위 칸 점유가 정합된다.
// 빈 행도 패턴의 일부(그룹 간격). 클리어 = 행 소진 + 필드 전멸. 전멸해도 다음 행은 제 박자에 온다 [가정].
public class WaveManager : InGameManager
{
    public event System.Action OnBossIncoming;   // 보스 스폰 예고 (WARNING 배너) — 실제 스폰은 bossWarningDelay 뒤

    // 진행도(처치 비율)의 분모 — 패턴의 총 유닛 수 (멀티셀도 1유닛)

    // 다음 보스까지의 진행도(0~1) — 무한모드 HUD 게이지용. 구간(배치 이후 컨베이어) 내 행수 / 간격.
    // 구간 소진(==간격) = 보스 스폰 대기 한 칸 — 100% 유지로 "보스 온다" 예고 (유저 확정 2026-07-15).
    // 그 너머(보스 중·소탕·숨고르기)는 0. 배치 5행은 카운트 제외
    public float BossProgress
    {
        get
        {
            int rows = nextRow - cycleStartRow;
            if (rows == bossWaveInterval) return 1f;
            return rows > bossWaveInterval ? 0f : (float)rows / bossWaveInterval;
        }
    }

    [SerializeField] private int initialRows = 5;      // 구간 시작 일괄 스폰 행 수 (원작 관찰)
    [SerializeField] private int baseHp = 30;          // 행 HP = baseHp + 행번호 × hpPerRow [튜닝]
    [SerializeField] private int hpPerRow = 10;
    [Min(1)][SerializeField] private int bossWaveInterval = 20; // 구간 길이 — 배치 후 몇 행 뒤 보스인지 (설계 §2-③)
    [SerializeField] private float bossClearDelay = 3f; // 보스 격파 → 소탕 후 다음 구간 배치까지 숨고르기(초)
    [SerializeField] private float bossWarningDelay = 2.5f; // WARNING 예고 → 실제 스폰까지 (배너 깜빡임 2초와 맞물림)

    private StagePattern pattern;
    private MonsterManager monsterManager;
    private int nextRow;
    private int cycleStartRow;     // 현 구간 시작 시점의 누적 행 — 진행도·보스 트리거 기준점 (배치마다 갱신)
    private bool hasStarted;
    private Monster currentBoss;   // 진행 중 보스 — 격파 신호 대조용
    private bool bossAlive;

    public override void Initialize()
    {
        base.Initialize();
        monsterManager = GameManager.MonsterManager;

        var csv = Resources.Load<TextAsset>("Tables/StagePattern");
        pattern = StagePattern.Parse(csv.text, GameManager.FieldManager.Columns);   // 엄격 파서 — 오염 즉시 예외

        monsterManager.OnMonsterKilled += HandleMonsterKilled;   // 보스 격파 감지 → 컨베이어 재개
        GameManager.AddGameStateEnterAction(GameManager.GameState.GamePlay, TryStart);
    }

    public override void Clear()
    {
        base.Clear();
        StopAllCoroutines();
        monsterManager.OnMonsterKilled -= HandleMonsterKilled;
        GameManager.RemoveGameStateEnterAction(GameManager.GameState.GamePlay, TryStart);
    }

    private void TryStart()
    {
        if (hasStarted) return;   // SkillSelection 복귀 등 GamePlay 재진입 대비
        hasStarted = true;

        SpawnBatch();   // 첫 구간 시작 (보스 격파 후 재시작과 동일 코드)
        StartCoroutine(ConveyorLoop());
    }

    // 구간 시작 일괄 배치 — 패턴 이어지는 위치(% Count)부터 5행, 먼저 나온 행이 아래.
    // 게임 시작과 보스 격파 후가 같은 절차라 공유 ("다시 처음처럼" — 유저 확정 2026-07-15)
    private void SpawnBatch()
    {
        int batch = Mathf.Min(initialRows, pattern.Rows.Count);
        for (int i = 0; i < batch; i++)
            monsterManager.SpawnRow(pattern.Rows[(nextRow + i) % pattern.Rows.Count],
                                    RowHp(nextRow + i), gridRowOffset: batch - 1 - i);
        nextRow += batch;
        cycleStartRow = nextRow;   // 새 구간 기준점 — 진행도 0%부터
    }

    private IEnumerator ConveyorLoop()
    {
        // "한 칸 내려오는 시간"마다 다음 행 — 스케일 시간이라 정지(선택/퍼즈) 동안 이동과 함께 멈춰 간격 유지
        float interval = GameManager.FieldManager.CellHeight / monsterManager.MoveSpeed;
        var wait = new WaitForSeconds(interval);

        // 무한 모드: CSV를 순환(% Count)해 끝없이 공급. nextRow는 시작부터의 누적 행 번호라
        // RowHp(nextRow)가 한 바퀴마다 계속 커진다 — 체력 점증이 인덱싱만으로 자동으로 붙는다.
        while (true)
        {
            yield return wait;
            if (nextRow - cycleStartRow >= bossWaveInterval)
                yield return BossWave();   // 구간 소진 → 보스 관문 (격파까지 줄 스폰 멈춤)
            else
            {
                monsterManager.SpawnRow(pattern.Rows[nextRow % pattern.Rows.Count], RowHp(nextRow));
                nextRow++;
            }
        }
    }

    // 보스 관문: 보스 격파까지 대기 → 소환 잔몹 소탕(처치 아님 — 점수/레벨 없음) → 숨고르기 → 새 구간 배치.
    // 소탕·배치는 코루틴(물리 콜백 밖)에서 실행 — 충돌 도중 콜라이더를 끄면 볼 반사가 깨지는 기존 버그 회피 구조 유지.
    private IEnumerator BossWave()
    {
        // 예고 국면: WARNING 배너 + 경보음 + 보스 BGM을 먼저 — "온다"를 보여준 뒤 등장 (유저 확정 2026-07-15)
        OnBossIncoming?.Invoke();
        SoundManager.Instance?.PlaySfx(SfxClipId.BossAppear);
        SoundManager.Instance?.PlayBgm(BgmClipId.Boss);
        yield return new WaitForSeconds(bossWarningDelay);

        currentBoss = monsterManager.SpawnBoss(RowHp(nextRow));
        nextRow++;   // 보스도 HP 곡선의 한 칸 소모 — 구간이 반복돼도 난이도는 계속 상승
        bossAlive = true;
        while (bossAlive)
            yield return null;

        monsterManager.ClearField();                       // 잔몹 일괄 제거 — 관문 보상은 보스 +100으로 이미 지급
        yield return new WaitForSeconds(bossClearDelay);   // 숨고르기 (스케일 시간 — 퍼즈와 함께 멈춤)
        SpawnBatch();                                      // 다음 구간 — 시작과 동일하게 5행 일괄
    }

    private void HandleMonsterKilled(Monster monster)
    {
        if (monster == currentBoss) { bossAlive = false; currentBoss = null; }
    }

    private int RowHp(int rowIndex) => baseHp + rowIndex * hpPerRow;
}
