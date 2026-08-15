using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : InGameManager
{
    public event Action<Monster> OnMonsterKilled;   // 처치된 몬스터 전달 (레벨 카운트·치명타 1회 소모 해제·성냥 폭발 위치)
    public event Action<Monster> OnMonsterDespawned; // 처치가 아닌 소멸(도달 돌진) — 몬스터 단위 외부 기록 정리용 (단검 등)
    public event Action<SkillId?, int> OnDamageDealt; // (소스, 데미지) — 전투 정보 집계용 재방송 (StatsManager 구독)
    public event Action<Monster> OnBossSpawned;       // 보스 인스턴스 전달 — 보스 HP바(UI)가 HP 이벤트를 구독할 창구
    public event Action OnBossEnded;                  // 보스 종료(격파·게임오버 정리 공통) — HUD 교대 복귀 신호
    public event Action OnFieldCleared;

    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject bossPrefab;          // 2×2 보스 전용 프리팹 (Monster 복제 + BossController + Body 중앙정렬)
    [SerializeField] private GameObject damagePopupPrefab;   // TMP 데미지 플로터 (빌더 메뉴로 생성·연결)
    [SerializeField] private float moveSpeed = 0.2f;     // 필드 높이 기준 하강 속도
    [SerializeField] private MonsterTypeData[] types;    // 몬스터 4종 구성 (Inspector 수동 연결)
    [SerializeField] private int attackDamage = 50;      // 돌진 충돌 데미지 — HP 300 기준 6대 사망 (유저 확정 2026-07-07)
    [SerializeField] private float chargeSpeed = 4f;     // 도달 후 플레이어 돌진 속도
    [SerializeField] private float chargeDelay = 3f;     // 도달 후 돌진까지 대기 (원작 관찰: 약 3초, 이때 처치 가능)
    [SerializeField] private int topSpawnRowOffset = 1;  // 스폰 시작 행 — 원작은 판 상단 한 행 아래부터 (완충 행:
                                                         // 웨이브 스폰이 상단의 볼과 겹쳐 벽 밖으로 밀어내던 실기기 버그의 원작식 해법)
    [SerializeField] private MonsterTypeData bossType;   // 2×2 보스 (Inspector 수동 연결 — body/blockSprite/width2/height2/hpMultiplier)
    [SerializeField] private int bossTickDamage = 50;    // 보스 바닥 지속공격 1틱 데미지 (주기는 BossController 소유)
    [SerializeField] private int summonAddHp = 30;       // 보스 소환 잡몹 HP (튜닝)

    private MonsterSpawner spawner;
    private MonsterSpawner bossSpawner;   // 보스 전용 풀 — 일반 몹과 프리팹이 달라 반환도 이쪽으로 라우팅
    private MonsterField field;
    private FieldManager fieldManager;   // 격자 지오메트리의 주인 — 위치는 전부 CellToWorld로 지목
    private float failY;

    // 사망 몬스터의 풀 반환 대기 목록 — 물리 콜백 "도중" 콜라이더를 끄면 볼의 반사 계산이
    // 반쪽으로 끝나 속도가 0이 되는 버그가 있어, 물리적 제거만 프레임 끝(LateUpdate)으로 미룬다.
    // (논리적 죽음 — 킬 카운트/웨이브 판정 — 은 즉시)
    private readonly List<Monster> pendingRelease = new();

    private Monster activeBoss;          // 현재 보스 — 이벤트 구독 해제(격파/정리) 판정
    private int summonCount;             // 소환 잡몹 열 순환용

    private DamagePopupSpawner popupSpawner;   // 데미지 플로터 (내부 컴포지션 — Spawner/Field와 동일 패턴)

    public override void Initialize()
    {
        base.Initialize();
        fieldManager = GameManager.FieldManager;
        spawner = new MonsterSpawner(GameManager.ObjectPool, monsterPrefab);
        bossSpawner = new MonsterSpawner(GameManager.ObjectPool, bossPrefab);
        field = new MonsterField();
        popupSpawner = new DamagePopupSpawner(GameManager.ObjectPool, damagePopupPrefab);
        failY = fieldManager.BottomWall + fieldManager.CellHeight * 0.5f; // 판 바닥 반 칸 위. 필요 시 Shooter 배치와 맞춰 미세조정

        // GameOver 진입 시 필드 정리 (기존엔 도달 즉시 GameOver+정리였으나 체력전으로 바뀌며 상태 액션으로 이동)
        GameManager.AddGameStateEnterAction(GameManager.GameState.GameOver, ClearAllMonsters);
    }

    public override void Clear()
    {
        base.Clear();
        GameManager.RemoveGameStateEnterAction(GameManager.GameState.GameOver, ClearAllMonsters);
        ClearAllMonsters();
    }

    // 컨베이어 하강 속도 — WaveManager가 행 간격(한 칸 시간)을 이 값으로 계산
    public float MoveSpeed => moveSpeed;

    // 필드 소탕 (보스 격파 후 새 구간 전환용) — "처치"가 아니라 제거: 킬 이벤트·점수·레벨 없이 풀 반환
    public void ClearField() => ClearAllMonsters();

    // 행 단위 스폰 (행 컨베이어 — 원작 확정 관찰 2026-07-07). 매판 동일 패턴, Random 금지.
    // gridRowOffset: 스폰 기준 행에서 아래로 몇 칸 (시작 5행 일괄 배치용 — 스트림 중엔 0)
    public void SpawnRow(RowCell[] rowCells, int baseHp, int gridRowOffset = 0)
    {
        int row = topSpawnRowOffset + gridRowOffset;   // 원작: 첫 줄 위 완충 행
        for (int col = 0; col < rowCells.Length; col++)
        {
            if (!rowCells[col].IsUnit) continue;
            MonsterTypeData type = TypeFor(rowCells[col].code);
            // 멀티셀: 앵커 칸 기준 점유 영역의 "중심" — 드리프터(1×2)은 위로 반 칸, 벌워크(2×1)는 오른쪽으로 반 칸
            Vector2 pos = fieldManager.CellToWorld(row, col)
                        + new Vector2(fieldManager.CellWidth * (type.width - 1) * 0.5f,
                                      fieldManager.CellHeight * (type.height - 1) * 0.5f);
            SpawnMonster(type, pos, baseHp, spawner, HandleMonsterReachedBottom);
        }
    }

    // 보스 스폰 — 필드 정중앙에 2×2 보스 1기(전용 프리팹/풀). 잡몹과 달리 바닥에서 돌진 안 함(null 핸들러 →
    // MonsterMover가 자동 정지) — 정지 후 공격·소환은 BossController가 이벤트로 알리고 여기서 적용.
    // 반환값을 WaveManager가 격파 감시에 씀. baseHp에 bossType.hpMultiplier가 곱해져 왕창이 된다.
    public Monster SpawnBoss(int baseHp)
    {
        // 2칸 폭은 그리드 앵커+오프셋으로는 홀수열 필드에서 반 칸 쏠린다 → 중앙 열 좌표에
        // 보스 중심(중앙 피벗)을 직접 얹는다. 세로만 2칸 중심으로 반 칸 올림.
        Vector2 pos = fieldManager.CellToWorld(topSpawnRowOffset, fieldManager.Columns / 2);
        pos.y += fieldManager.CellHeight * (bossType.height - 1) * 0.5f;
        activeBoss = SpawnMonster(bossType, pos, baseHp, bossSpawner, reachedBottomHandler: null);

        BossController boss = activeBoss.GetComponent<BossController>();
        boss.OnAttackTick += HandleBossAttack;   // 바닥 정지 중 주기 공격 → 플레이어 데미지로 적용
        boss.OnSummon += HandleBossSummon;        // 소환 → 잡몹 스폰으로 적용
        boss.Activate();
        OnBossSpawned?.Invoke(activeBoss);
        // 경보음·보스 BGM은 예고 국면(WaveManager.OnBossIncoming)으로 이동 — 스폰 순간엔 소리 없음
        return activeBoss;
    }

    // 몬스터 1기 스폰의 공통 절차. 위치·풀·바닥핸들러를 호출자가 정한다
    // (보스: 전용 풀 + null 핸들러 — 돌진 대신 정지, 바닥 행동은 BossController가 mover 이벤트로 직접 잡음).
    private Monster SpawnMonster(MonsterTypeData type, Vector2 pos, int baseHp, MonsterSpawner sp,
                                 Action<MonsterMover> reachedBottomHandler)
    {
        Monster monster = sp.Spawn(pos, Mathf.RoundToInt(baseHp * type.hpMultiplier));
        // 블록 스프라이트가 점유 크기 그대로 제작됨(Block_1x1/1x2/2x1/2x2, 1칸=1WU) — 균등 스케일 유지
        monster.transform.localScale = Vector3.one * fieldManager.CellWidth;
        monster.GetComponent<MonsterVisual>().Apply(type);
        monster.GetComponent<MonsterHpBar>().AlignToBottomCell(type.height);
        // 콜라이더는 블록 스프라이트 크기(0.96/칸)에 맞춤 — 기존 1×1 판정과 동일 기준
        monster.GetComponent<BoxCollider2D>().size = new Vector2(type.width * 0.96f, type.height * 0.96f);

        MonsterMover mover = monster.GetComponent<MonsterMover>();
        // 정지선은 높이만큼 올린다 — MonsterMover는 rb.position(=중심)으로 바닥 도달을 판정하는데
        // failY는 1×1 기준값이라, 세로로 긴 블록(드리프터 1×2·보스 2×2)이 그대로 쓰면 (height-1)/2 칸만큼
        // 판 바닥을 뚫고 내려간다. 스폰 좌표의 멀티셀 보정과 같은 식 (높이 1은 0이라 무변화).
        mover.Initialize(moveSpeed, failY + fieldManager.CellHeight * (type.height - 1) * 0.5f);
        if (reachedBottomHandler != null) mover.OnReachedBottom += reachedBottomHandler;
        field.Add(monster);
        monster.OnDied += HandleMonsterDied;
        monster.OnDamaged += HandleMonsterDamaged;
        return monster;
    }

    // 보스 지속공격 1틱 — 플레이어 데미지 적용(몬스터→플레이어 데미지는 원래 이 매니저 몫)
    private void HandleBossAttack()
    {
        popupSpawner.Show(GameManager.PlayerManager.Position + Vector2.up * 0.5f, bossTickDamage, false);
        GameManager.PlayerManager.TakeDamage(bossTickDamage);
    }

    // 보스 소환 — 잡몹 1마리를 상단에 스폰(열 순환). field/풀 공유라 잡몹과 동일하게 하강·돌진
    private void HandleBossSummon()
    {
        int col = summonCount++ % fieldManager.Columns;
        Vector2 pos = fieldManager.CellToWorld(topSpawnRowOffset, col);   // 잡몹 1×1 — 오프셋 불필요
        SpawnMonster(types[0], pos, summonAddHp, spawner, HandleMonsterReachedBottom);
    }

    // 풀 반환 라우팅 — 보스는 전용 프리팹/풀이라 BossController 유무로 판별(일반 몹엔 없음)해 제 풀로 되돌린다
    private void ReleaseMonster(Monster monster)
    {
        if (monster.GetComponent<BossController>() != null) bossSpawner.Release(monster);
        else spawner.Release(monster);
    }

    // 보스 이벤트 구독 해제 — 격파/필드정리 양쪽에서 호출(풀 재사용 시 중복 구독 방지)
    private void DetachBoss()
    {
        if (activeBoss == null) return;
        BossController boss = activeBoss.GetComponent<BossController>();
        boss.OnAttackTick -= HandleBossAttack;
        boss.OnSummon -= HandleBossSummon;
        activeBoss = null;
        OnBossEnded?.Invoke();   // 격파(HandleMonsterDied)·게임오버 정리(ClearAllMonsters) 양 경로 공통 지점
    }

    // 패턴 코드 → 타입: 숫자 코드는 배열 인덱스, 멀티셀 앵커는 이름 검색 (배열 순서 계약 회피)
    private MonsterTypeData TypeFor(int code)
    {
        if (code == RowCell.DrifterAnchor) return FindType("Drifter");
        if (code == RowCell.BulwarkAnchor) return FindType("Bulwark");
        return types[code % types.Length];
    }

    private MonsterTypeData FindType(string name)
    {
        foreach (MonsterTypeData t in types)
            if (t.typeName == name) return t;
        throw new System.InvalidOperationException($"types에 '{name}' 없음 — Inspector 확인");
    }

    // 레이저볼 "같은 행" 쿼리 — 기준 Y에서 반 칸 이내의 활성 몬스터 (연속 하강이라 행 인덱스보다 Y밴드가 정확)
    public List<Monster> GetMonstersNearRow(float y)
    {
        var result = new List<Monster>();
        float halfCell = fieldManager.CellHeight * 0.5f;
        foreach (Monster m in field.ActiveMonsters)
            if (Mathf.Abs(m.transform.position.y - y) <= halfCell)
                result.Add(m);
        return result;
    }

    private void HandleMonsterDied(Monster monster)
    {
        bool isBoss = monster == activeBoss;   // DetachBoss가 activeBoss를 비우므로 판별을 먼저
        if (isBoss) DetachBoss();              // 보스 격파 → 이벤트 해제 (BossController는 스스로 코루틴 정리)

        SoundManager.Instance?.PlaySfx(isBoss ? SfxClipId.BossDie : SfxClipId.MonsterDie);
        if (isBoss) SoundManager.Instance?.PlayBgm(BgmClipId.InGame);   // 격파 경로만 BGM 복귀 — 게임오버 정리는 제외

        Unsubscribe(monster);
        field.Remove(monster);
        pendingRelease.Add(monster);   // 물리적 제거는 LateUpdate에서

        OnMonsterKilled?.Invoke(monster);
        if (field.IsEmpty)   // 동기 스폰이라 스폰 도중 조기 발화 경합이 없음 (코루틴/가드 삭제됨)
            OnFieldCleared?.Invoke();
    }

    // 이 프레임의 모든 물리 스텝/충돌 콜백이 끝난 뒤 — 볼 반사가 온전히 완료된 다음 실제로 끈다
    private void LateUpdate()
    {
        if (pendingRelease.Count == 0) return;
        foreach (Monster monster in pendingRelease)
            ReleaseMonster(monster);
        pendingRelease.Clear();
    }

    // 바닥 도달 → 플레이어에게 돌진 (원작: 부딪히며 1회 데미지 주고 소멸. 멈춰서 지속 공격은 보스 전용)
    private void HandleMonsterReachedBottom(MonsterMover mover)
    {
        mover.OnReachedPlayer += HandleMonsterCrashed;
        mover.ChargeTo(GameManager.PlayerManager.Position, chargeSpeed, chargeDelay);
    }

    // 돌진 도착 — 플레이어 피격 + 몬스터 소멸. "처치"가 아니므로 OnMonsterKilled 미발행
    // (도달 소멸로 레벨업/성냥 폭발이 발동하면 안 됨). 필드가 비면 웨이브는 정상 진행.
    // 순서 주의: 몬스터 정리 먼저, 데미지(GameOver 전환 유발 가능)는 마지막 —
    // 전환 액션에서 예외가 나도 유령 몬스터(구독/필드 잔류)가 안 생기게 (실사례: GameUIManager 크래시)
    private void HandleMonsterCrashed(MonsterMover mover)
    {
        Monster monster = mover.GetComponent<Monster>();
        Unsubscribe(monster);
        field.Remove(monster);
        pendingRelease.Add(monster);
        OnMonsterDespawned?.Invoke(monster);   // 풀 재사용 대비 — 외부의 몬스터 단위 기록(단검 소모 등) 정리 신호

        popupSpawner.Show(GameManager.PlayerManager.Position + Vector2.up * 0.5f, attackDamage, false);
        GameManager.PlayerManager.TakeDamage(attackDamage);
        if (field.IsEmpty)
            OnFieldCleared?.Invoke();
    }

    // 데미지 플로터 — 모든 데미지 소스(볼/화상/레이저/파편/폭발)가 Monster 이벤트로 모이므로 여기서 일괄 표시.
    // 위치 = 몬스터 중앙 (원작 관찰 — 겹쳐도 그대로, 유저 확정)
    private void HandleMonsterDamaged(Monster monster, int damage, bool isCritical, SkillId? source)
    {
        popupSpawner.Show(monster.transform.position, damage, isCritical);
        SoundManager.Instance?.PlaySfx(SfxClipId.MonsterHit, 0.3f);   // 고빈도 타격음 — 작게 (장르 관행), 스팸은 쿨다운 방어
        OnDamageDealt?.Invoke(source, damage);
    }

    private void ClearAllMonsters()
    {
        DetachBoss();   // 보스가 살아있는 채 정리(게임오버/씬언로드)돼도 이벤트 잔류 없게
        foreach (Monster monster in field.ActiveMonsters)
        {
            if (monster == null) continue;   // 씬 언로드 시 몬스터가 매니저보다 먼저 Destroy되는 순서 대비
            Unsubscribe(monster);
            ReleaseMonster(monster);
        }
        field.Clear();
        OnFieldCleared?.Invoke();   // 일괄 정리도 "필드 비워짐"으로 통지 — 처치/소멸 이벤트를 안 타므로 외부 몬스터 단위 기록(단검 명단)을 여기서 초기화시킨다
    }

    private void Unsubscribe(Monster monster)
    {
        monster.OnDied -= HandleMonsterDied;
        monster.OnDamaged -= HandleMonsterDamaged;
        MonsterMover mover = monster.GetComponent<MonsterMover>();
        mover.OnReachedBottom -= HandleMonsterReachedBottom;
        mover.OnReachedPlayer -= HandleMonsterCrashed;
    }
}
