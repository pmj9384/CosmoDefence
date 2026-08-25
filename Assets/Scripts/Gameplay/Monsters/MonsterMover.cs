using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterMover : MonoBehaviour
{
    public event Action<MonsterMover> OnReachedBottom;
    public event Action<MonsterMover> OnReachedPlayer;   // 돌진 도착 — 충돌 데미지 처리는 매니저 몫

    // 냉동 등 상태이상의 감속 적용 지점 (1 = 정상 속도)
    public float SpeedMultiplier { get; set; } = 1f;

    private const float ChargeArriveRadius = 0.3f;

    // 전방(아래) 몬스터 감지 — 냉동 등으로 앞이 느려지면 파고들지 않고 줄서서 대기 (실기기 발견 버그).
    // 반드시 "정상 격자 틈"(한 칸 0.49 - 블록 0.43 ≈ 0.06)보다 작아야 한다 — 크면 정상 하강까지
    // 막혀서 뒷줄이 못 따라옴 (0.12였을 때 실버그: 낀 몬스터가 찔끔찔끔 처짐)
    private const float FrontGap = 0.03f;

    private Rigidbody2D rb;
    private Collider2D body;
    private int monsterMask;
    private float moveSpeed;
    private float failY;
    private bool reachedBottom;
    private bool charging;
    private Vector2 chargeTarget;
    private float chargeSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        monsterMask = LayerMask.GetMask("Monster");
    }

    public void Initialize(float speed, float failY)
    {
        moveSpeed = speed;
        this.failY = failY;
        reachedBottom = false;
        charging = false;       // 풀 재사용 대비 초기화
        SpeedMultiplier = 1f;
    }

    private float chargeDelay;

    // 바닥 도달 후 delay초 머물렀다가 플레이어를 향해 돌진 (원작: 3초 뒤 회수되며 1회 데미지 — 대기 중 처치 가능)
    public void ChargeTo(Vector2 target, float speed, float delay)
    {
        chargeTarget = target;
        chargeSpeed = speed;
        chargeDelay = delay;
        charging = true;
    }

    private void FixedUpdate()
    {
        if (charging) { TickCharge(); return; }   // 돌진은 관통 의도 — 전방 감지 안 함
        if (reachedBottom) return;
        if (IsBlockedByFrontMonster()) return;    // 앞(아래) 몬스터가 코앞이면 이 프레임 대기

        Vector2 nextPosition = rb.position + Vector2.down * (moveSpeed * SpeedMultiplier) * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);

        if (nextPosition.y <= failY)
        {
            reachedBottom = true;
            OnReachedBottom?.Invoke(this);
        }
    }

    // 자기 콜라이더 아래변 "폭 전체"를 BoxCast — 몬스터끼리는 물리 해소가 없어서(kinematic)
    // 감속된 앞 몬스터를 뒤가 파고들던 문제를 "이동 전 양보"로 해결 [가정: 원작도 겹치지 않고 대기].
    // 중심 한 줄 Raycast였을 땐 폭 넓은 가로 블록(2×1)가 자기 아래 1×1을 비껴 못 보고 밀고 내려감 (실기기 발견)
    private bool IsBlockedByFrontMonster()
    {
        Bounds b = body.bounds;
        Vector2 origin = new(b.center.x, b.min.y - 0.02f);
        Vector2 size = new(b.size.x * 0.9f, 0.02f);
        return Physics2D.BoxCast(origin, size, 0f, Vector2.down, FrontGap, monsterMask).collider != null;
    }

    private void TickCharge()
    {
        if (chargeDelay > 0f)   // 도달 후 대기 (이 동안 볼에 맞아 죽을 수 있음)
        {
            chargeDelay -= Time.fixedDeltaTime;
            return;
        }

        Vector2 toTarget = chargeTarget - rb.position;
        if (toTarget.magnitude < ChargeArriveRadius)
        {
            charging = false;
            OnReachedPlayer?.Invoke(this);
            return;
        }
        rb.MovePosition(rb.position + toTarget.normalized * (chargeSpeed * Time.fixedDeltaTime));
    }
}
