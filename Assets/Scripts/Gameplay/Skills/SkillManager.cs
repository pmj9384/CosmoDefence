using System;
using System.Collections.Generic;
using UnityEngine;

// 스킬 시스템의 조정자 — 순수 코어(PlayerLevel/PlayerSkills/SkillDraft/DamageCalculator)를 소유하고
// 게임 이벤트(처치·볼 히트)와 상태 전환(SkillSelection), UI를 잇는다. 계산·규칙은 전부 코어 몫.
public class SkillManager : InGameManager
{
    [SerializeField] private SkillSelectionPanel selectionPanel;

    public PlayerSkills PlayerSkills => playerSkills;
    public PlayerLevel PlayerLevel => playerLevel;

    private PlayerSkills playerSkills;
    private PlayerLevel playerLevel;
    private readonly System.Random rng = new();
    private int pendingDrafts;                              // 연속 레벨업 시 3택지를 연달아 띄우기 위한 큐

    [SerializeField] private int initialNormalBalls = 5;   // 시작 노멀볼 수 (원작 재확인: 5발 — 2026-07-07 정정)

    // 발사 볼 인벤토리 — 보유 볼 각각이 개체 (필드에 나가 있거나 대기). 규칙은 BallInventory(순수 코어) 몫
    private BallInventory ballInventory;
    public int NormalBallLevel { get; private set; } = 1;  // 노멀볼 레벨 (전투 정보 ◆xN) — 채움 카드로 개수와 함께 성장
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

        GameManager.MonsterManager.OnMonsterKilled += HandleMonsterKilled;
        GameManager.MonsterManager.OnMonsterDespawned += HandleMonsterDespawned;
        GameManager.MonsterManager.OnFieldCleared += HandleFieldCleared;
        GameManager.BallManager.OnBallHitMonster += HandleBallHit;

        // 레벨업 0.35s 지연 중 퍼즈로 이탈하면 openDelay가 무산되는데, 되살릴 트리거가 다음 킬뿐이라
        // 킬 없이 죽으면 그 선택이 증발한다 (검수 v5 #5) — GamePlay 복귀 시 잔여 드래프트를 재개방
        GameManager.AddGameStateEnterAction(GameManager.GameState.GamePlay, () =>
        {
            if (pendingDrafts > 0 && openDelay == null)
                openDelay = StartCoroutine(OpenSelectionAfterGaugeFill());
        });
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
            spritePath = def.iconName,   // 볼 스프라이트 = 카드 아이콘과 동일 에셋 (CSV가 SSOT)
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

    // ── 레벨업 → 3택지 ────────────────────────────────────────────

    private void HandleMonsterKilled(Monster monster)
    {
        daggerRule.Forget(monster.GetInstanceID());   // 풀 재사용 대비 — 죽은 몬스터의 1회 소모 기록 해제

        // 마지막 성냥: 사망 폭발 (연쇄 사망 시 재귀적으로 다시 발동 — 의도된 연쇄)
        int matchLevel = playerSkills.GetLevel(SkillId.LastMatch);
        if (matchLevel > 0)
            lastMatch.Explode(monster.transform.position, playerSkills.Table[SkillId.LastMatch].GetLevel(matchLevel));

        pendingDrafts += playerLevel.AddKill();
        // 이미 선택 중이면 큐에만 쌓고, 선택이 끝날 때 이어서 연다.
        // 즉시 열지 않고 잠깐 지연 — 레벨 게이지가 "꽉 차는" 연출을 보여준 뒤 창이 뜬다 (원작 시퀀스)
        if (pendingDrafts > 0 && GameManager.CurrentState == GameManager.GameState.GamePlay && openDelay == null)
            openDelay = StartCoroutine(OpenSelectionAfterGaugeFill());
    }

    private Coroutine openDelay;
    private const float SelectionOpenDelay = 0.35f;  // 게이지 채움 연출 시간 — 0.6은 "한 템포 늦음"으로 체감 (유저 2026-07-07)

    private System.Collections.IEnumerator OpenSelectionAfterGaugeFill()
    {
        yield return new WaitForSeconds(SelectionOpenDelay);   // 스케일 시간 — 이 동안 게임은 계속 돈다 (원작)
        openDelay = null;
        if (pendingDrafts > 0 && GameManager.CurrentState == GameManager.GameState.GamePlay)
            OpenSelection();
    }

    private void OpenSelection()
    {
        List<SkillId> cards = SkillDraft.Draw(playerSkills, rng);
        if (cards.Count == 0)           // 채움 카드 덕에 사실상 불가능 — 방어적 안전망
        {
            pendingDrafts = 0;
            // 연속 레벨업 체인 "도중" 소진이면 상태가 SkillSelection에 잠긴 채 방치되던 실버그 —
            // 복귀시켜 줄 사람이 없으므로 여기서 직접 GamePlay로
            if (GameManager.CurrentState == GameManager.GameState.SkillSelection)
                GameManager.SetGameState(GameManager.GameState.GamePlay);
            return;
        }

        GameManager.SetGameState(GameManager.GameState.SkillSelection);
        selectionPanel.Show(cards, playerSkills, playerLevel.Level, HandleCardPicked);
    }

    private void HandleCardPicked(SkillId picked)
    {
        ApplyPick(picked);
        selectionPanel.Hide();
        pendingDrafts--;

        if (pendingDrafts > 0)
        {
            OpenSelection();            // 연속 레벨업 — 다음 드래프트 (상태는 SkillSelection 유지)
        }
        else
        {
            GameManager.SetGameState(GameManager.GameState.GamePlay);
        }
    }

    // 카드 1장 선택의 실제 효과 적용 (드래프트 UI와 분리 — 에디터 디버그 획득도 이 경로를 그대로 탄다)
    private void ApplyPick(SkillId picked)
    {
        if (picked == SkillId.NormalBall)
        {
            NormalBallLevel++;              // 채움 카드 — 노멀볼 레벨과 개수가 함께 성장 (◆xN)
            ballInventory.Add(null);
        }
        else
        {
            bool isActiveBall = playerSkills.Table[picked].kind == SkillKind.ActiveBall;
            bool wasMax = playerSkills.GetLevel(picked) >= PlayerSkills.MaxLevel;
            bool isNew = !playerSkills.Has(picked);
            bool acquired = playerSkills.Acquire(picked);   // 만석 신규·만렙이면 false(레벨 안 오름)

            // 액티브볼 볼 추가 조건: (신규 획득 성공) 또는 (만렙 재선택 = "+1개"). 개수만 늘고 레벨은 Lv3 고정.
            // 만석이라 획득 실패한 신규는 제외 — 안 그러면 레벨0(미보유) 볼이 인벤토리에 들어가 발사 시 GetLevel(0) 크래시
            if (isActiveBall && ((isNew && acquired) || wasMax))
                ballInventory.Add(picked);
        }
    }

#if UNITY_EDITOR
    // 에디터 전용 디버그 치트 진입점 — 스킬 1레벨업(미보유면 획득). 만렙 액티브볼이면 개수 +1.
    // CheatWindow(Tools/Cheats)의 버튼에서 Play 중 호출. 빌드엔 안 들어감.
    public void DebugLevelUp(SkillId id) => ApplyPick(id);
#endif
}
