using System.Collections.Generic;
using UnityEngine;

// 스킬 시스템의 조정자 — 순수 코어(PlayerSkills/DamageCalculator 등)를 소유하고 게임 이벤트(처치·볼 히트)를
// 중계한다. 계산·규칙은 코어 몫, 레벨업→3택지 진행은 LevelUpDraftController 몫 (검수 v6 분리).
public class SkillManager : InGameManager
{
    [SerializeField] private SkillSelectionPanel selectionPanel;

    public PlayerSkills PlayerSkills => playerSkills;
    public PlayerLevel PlayerLevel => playerLevel;
    public int NormalBallLevel => draft.NormalBallLevel;   // 전투 정보 ◆xN — 소유는 드래프트 진행자

    private PlayerSkills playerSkills;
    private PlayerLevel playerLevel;
    private LevelUpDraftController draft;
    private readonly System.Random rng = new();

    [SerializeField] private int initialNormalBalls = 5;   // 시작 노멀볼 수 (원작 재확인: 5발 — 2026-07-07 정정)

    // 발사 볼 인벤토리 — 보유 볼 각각이 개체 (필드에 나가 있거나 대기). 규칙은 BallInventory(순수 코어) 몫
    private BallInventory ballInventory;
    private readonly DaggerCritRule daggerRule = new();    // 단검 전면/후면 크리 규칙 (순수 C#, 테스트 대상)

    // 스킬 "행동"은 클래스 단위로 분리(SRP) — 여기는 디스패치만
    private Dictionary<SkillId, IOnHitEffect> onHitEffects;
    private LastMatchEffect lastMatch;

    public override void Initialize()
    {
        base.Initialize();

        var csv = Resources.Load<TextAsset>("Tables/SkillTable");
        playerSkills = new PlayerSkills(SkillTableParser.Parse(csv.text));
        playerLevel = new PlayerLevel();

        ballInventory = new BallInventory();
        for (int i = 0; i < initialNormalBalls; i++)
            ballInventory.Add(null);   // 시작 구성: 노멀볼 N발

        onHitEffects = new Dictionary<SkillId, IOnHitEffect>
        {
            { SkillId.FireBall, new FireBallEffect() },
            { SkillId.IceBall, new IceBallEffect(rng) },
            { SkillId.LaserBall, new LaserBallEffect(GameManager.MonsterManager) },
            { SkillId.ClusterBall, new ClusterBallEffect(rng, GameManager.BallManager.SpawnFragment) },   // 파편 스폰은 볼의 집(BallManager) 소관
            // GhostBall은 온히트 효과 없음 — 관통은 Ball의 레이어/센서 거동
        };
        lastMatch = new LastMatchEffect(GameManager.FieldManager);
        draft = new LevelUpDraftController(GameManager, playerSkills, playerLevel,
            ballInventory, selectionPanel, rng, StartCoroutine);   // 코루틴 러너만 빌려줌 — 진행 로직은 저쪽 소유

        GameManager.MonsterManager.OnMonsterKilled += HandleMonsterKilled;
        GameManager.MonsterManager.OnMonsterDespawned += HandleMonsterDespawned;
        GameManager.MonsterManager.OnFieldCleared += HandleFieldCleared;
        GameManager.BallManager.OnBallHitMonster += HandleBallHit;

        GameManager.AddGameStateEnterAction(GameManager.GameState.GamePlay, draft.TryReopenPending);
    }

    public override void Clear()
    {
        base.Clear();
        GameManager.MonsterManager.OnMonsterKilled -= HandleMonsterKilled;
        GameManager.MonsterManager.OnMonsterDespawned -= HandleMonsterDespawned;
        GameManager.MonsterManager.OnFieldCleared -= HandleFieldCleared;
        GameManager.BallManager.OnBallHitMonster -= HandleBallHit;
    }

    // 도달 돌진으로 소멸한 몬스터 — 처치가 아니라서 HandleMonsterKilled를 안 타므로,
    // 단검 "적당 1회" 기록을 여기서 지워야 풀 재사용 몬스터가 소모 상태로 시작하지 않는다 (검수 v2 #2)
    private void HandleMonsterDespawned(Monster monster) => daggerRule.Forget(monster.GetInstanceID());

    // 필드 일괄 정리(구간 전환·게임오버) — 처치/소멸 이벤트를 안 타고 사라진 몬스터들의 명단을 통째로 비운다.
    // 이게 없으면 풀 재사용 몬스터가 이전 소모 상태를 물려받아 단검 크리가 영구 미발동 (검수 v2 #2)
    private void HandleFieldCleared() => daggerRule.Clear();

    // ── 발사 로테이션 ─────────────────────────────────────────────

    // 인벤토리 대기열 앞의 볼을 발사용으로 꺼냄 — 대기 없으면 false (전부 필드에 나가 있음).
    // 레벨/데미지는 "발사 시점"에 조회 — 비행 중 레벨업이 다음 발사부터 반영된다
    public bool TryGetNextLoadout(out BallLoadout loadout)
    {
        if (!ballInventory.TryTakeNext(out SkillId? skill))
        {
            loadout = default;
            return false;
        }

        if (skill == null)
        {
            loadout = BallLoadout.Normal;
            return true;
        }

        int level = playerSkills.GetLevel(skill.Value);
        SkillDef def = playerSkills.Table[skill.Value];
        loadout = new BallLoadout
        {
            skill = skill,
            level = level,
            damage = def.GetLevel(level).ballDamage,
        };
        return true;
    }

    // 회수된 볼을 대기열 뒤로 — 먼저 회수된 순 재발사 (원작 규칙)
    public void ReturnBall(SkillId? skill) => ballInventory.Return(skill);

    // ── 데미지 파이프라인 ─────────────────────────────────────────

    private void HandleBallHit(Ball ball, Collider2D monsterCollider, Vector2 hitNormal)
    {
        Monster monster = monsterCollider.GetComponent<Monster>();
        if (monster == null) return;

        MonsterStatusEffects status = monster.GetComponent<MonsterStatusEffects>();

        var ctx = new DamageCalculator.Context
        {
            baseDamage = ball.BaseDamage,
            isNormalBall = ball.ActiveSkill == null,
            tinHeartBonus = playerSkills.PassiveValue(SkillId.TinHeart),
            mirrorPerBounce = playerSkills.PassiveValue(SkillId.MagicMirror),
            wallBounces = ball.WallBounceCount,
            targetFrozen = status != null && status.IsFrozen,
            frozenBonus = status != null ? status.FrozenDamageBonus : 0f,
            critChance = CritChanceFor(monster, hitNormal),
            critMultiplier = 1.5f,  // 기획서: 치명타 데미지율 50%
        };

        monster.TakeDamage(DamageCalculator.Calc(ctx, rng, out bool isCrit), isCrit, ball.ActiveSkill);
        ApplyOnHitEffect(ball, monster, status);
    }

    // 볼 타입별 온히트 효과 — 행동은 Effects/ 클래스, 수치는 SkillTable(CSV) 현재 레벨 값
    private void ApplyOnHitEffect(Ball ball, Monster monster, MonsterStatusEffects status)
    {
        if (ball.ActiveSkill == null) return;
        if (!onHitEffects.TryGetValue(ball.ActiveSkill.Value, out IOnHitEffect effect)) return;

        effect.Apply(monster, playerSkills.Table[ball.ActiveSkill.Value].GetLevel(ball.SkillLevel));
    }

    // 판정 규칙은 DaggerCritRule(순수 코어) 소유 — 여기는 보유 패시브 값만 이어준다
    private float CritChanceFor(Monster monster, Vector2 hitNormal)
    {
        return daggerRule.CritChance(monster.GetInstanceID(), hitNormal.y,
            playerSkills.PassiveValue(SkillId.AmethystDagger), playerSkills.PassiveValue(SkillId.EmeraldDagger));
    }

    // ── 킬 이벤트 중계 — 단검 명단·성냥 폭발은 여기, 레벨업/3택지 진행은 draft 소유 ──

    private void HandleMonsterKilled(Monster monster)
    {
        daggerRule.Forget(monster.GetInstanceID());   // 풀 재사용 대비 — 죽은 몬스터의 1회 소모 기록 해제

        // 마지막 성냥: 사망 폭발 (연쇄 사망 시 재귀적으로 다시 발동 — 의도된 연쇄)
        int matchLevel = playerSkills.GetLevel(SkillId.LastMatch);
        if (matchLevel > 0)
            lastMatch.Explode(monster.transform.position, playerSkills.Table[SkillId.LastMatch].GetLevel(matchLevel));

        draft.RegisterKill();
    }

#if UNITY_EDITOR
    // 에디터 전용 디버그 치트 진입점 — 스킬 1레벨업(미보유면 획득). 만렙 액티브볼이면 개수 +1.
    // CheatWindow(Tools/Cheats)의 버튼에서 Play 중 호출. 빌드엔 안 들어감.
    public void DebugLevelUp(SkillId id) => draft.ApplyPick(id);
#endif
}
