using System;
using System.Collections;
using UnityEngine;

// 보스 고유 행동·타이밍만 담당 (MonsterVisual/HpBar와 동일 패턴 — 컴포넌트가 자기 행동만, 매니저는 모름).
// 평소엔 idle, MonsterManager가 보스로 스폰할 때만 Activate(). 효과(플레이어 데미지·잡몹 스폰)는
// 이벤트로 알리고 MonsterManager가 적용한다 — 엔티티는 매니저/플레이어를 알지 않는다.
[RequireComponent(typeof(Monster))]
[RequireComponent(typeof(MonsterMover))]
public class BossController : MonoBehaviour
{
    public event Action OnSummon;       // 잡몹 소환 신호 (하강·정지 내내 주기적)
    public event Action OnAttackTick;   // 바닥 정지 중 지속공격 1틱 신호

    [SerializeField] private float summonInterval = 4f;   // 소환 주기(초)
    [SerializeField] private float attackInterval = 3f;   // 바닥 정지 후 지속공격 주기(초)
    [SerializeField] private float entryHoldSeconds = 4f; // 등장 후 제자리 대기 — 잔여 행 정리 시간 (유저 제안 2026-08-03)

    private Monster monster;
    private MonsterMover mover;
    private bool active;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        mover = GetComponent<MonsterMover>();
    }

    // MonsterManager가 보스 스폰 직후 호출 — 이때부터 보스 행동 시작
    public void Activate()
    {
        if (active) return;
        active = true;
        mover.OnReachedBottom += HandleReachedBottom;   // 바닥 도달 → 지속공격 개시
        monster.OnDied += HandleDied;
        StartCoroutine(EntryThenAct());                  // 등장 대기 → 하강·소환 개시
    }

    // 등장 연출: 제자리 버티기 — 하강만 멈추고 피격은 허용 (잔여를 빨리 정리한 만큼 프리딜 보상).
    // 소환도 대기 후 시작 — 대기 중 잡몹이 새로 깔리면 "정리 시간"이라는 취지가 무너진다
    private IEnumerator EntryThenAct()
    {
        mover.enabled = false;
        yield return new WaitForSeconds(entryHoldSeconds);   // 스케일 시간 — 퍼즈·선택창과 함께 멈춤
        if (!active) yield break;
        mover.enabled = true;
        StartCoroutine(SummonLoop());                    // 하강 중에도 소환 (원작 관찰 없음 — 유저 확정)
    }

    // 풀 회수(SetActive false) 시 확실히 정지 — 다음 재사용이 잡몹으로 나와도 보스 코루틴이 안 남게
    private void OnDisable() => Deactivate();

    private void Deactivate()
    {
        if (!active) return;
        active = false;
        mover.OnReachedBottom -= HandleReachedBottom;
        monster.OnDied -= HandleDied;
        StopAllCoroutines();
        mover.enabled = true;   // 풀 재사용 방어 — 등장 대기 중 죽으면 꺼진 채 반환돼 다음 잡몹이 안 내려온다
    }

    private void HandleDied(Monster _) => Deactivate();

    private void HandleReachedBottom(MonsterMover _) => StartCoroutine(AttackLoop());

    private IEnumerator SummonLoop()
    {
        var wait = new WaitForSeconds(summonInterval);   // 스케일 시간 — 퍼즈/게임오버 때 함께 멈춤
        while (true)
        {
            yield return wait;
            OnSummon?.Invoke();
        }
    }

    private IEnumerator AttackLoop()
    {
        var wait = new WaitForSeconds(attackInterval);
        while (true)
        {
            yield return wait;
            OnAttackTick?.Invoke();
        }
    }
}
