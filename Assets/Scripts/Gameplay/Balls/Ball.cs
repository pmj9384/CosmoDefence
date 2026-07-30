using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    private static int wallLayer = -1, monsterLayer = -1;   // 충돌마다 문자열 레이어 조회 회피
    public event Action<Ball, Collider2D, Vector2> OnHitMonster;   // (볼, 몬스터 콜라이더, 충돌 노멀)
    public event Action<Ball> OnExitField;

    public int WallBounceCount { get; private set; }

    // 발사 시 주입되는 타입 파라미터 — Ball은 매니저를 모르고, 데미지 계산은 SkillManager 몫
    public SkillId? ActiveSkill { get; private set; }
    public int SkillLevel { get; private set; }
    public int BaseDamage { get; private set; }

    private const float ReturnArriveRadius = 0.3f;

    [SerializeField] private GameObject ghostSensor;   // 자식 관통 센서 — 고스트볼일 때만 활성

    private Rigidbody2D rb;
    private float launchSpeed;
    private Vector2 returnTarget;   // 회수 목적지(슈터 위치) — 발사 시 값만 전달받음 (매니저 모름)
    private bool isReturning;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float speed, Vector2 returnTarget, BallLoadout loadout)
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = direction.normalized * speed;
        launchSpeed = speed;
        this.returnTarget = returnTarget;
        isReturning = false;
        WallBounceCount = 0;

        ActiveSkill = loadout.skill;
        SkillLevel = loadout.level;
        BaseDamage = loadout.damage;

        // 고스트볼 = 몬스터 통과(GhostBall×Monster 매트릭스 OFF) + 자식 센서로 히트 감지.
        // 벽/바닥과는 여전히 충돌 → 바닥 회수 경로 동일 (원작 관찰)
        bool isGhost = loadout.skill == SkillId.GhostBall;
        gameObject.layer = LayerMask.NameToLayer(isGhost ? "GhostBall" : "Ball");
        if (ghostSensor != null) ghostSensor.SetActive(isGhost);
    }

    // 관통은 물리 접촉이 없어 노멀이 없음 — 진행 방향의 반대로 근사 (단검 전/후면 판정용)
    public void NotifyGhostHit(Collider2D monster)
        => OnHitMonster?.Invoke(this, monster, -rb.linearVelocity.normalized);

    private void FixedUpdate()
    {
        if (!isReturning)
        {
            // 발사볼은 항상 launchSpeed로 날아야 함(충돌마다 재정규화) — 속도가 죽어 있으면
            // 원인 불문 고장(예: 벽 접촉 중 timeScale 정지 → 반발 없는 겹침 해소로 속도 소멸).
            // 방향 정보는 이미 소실됐으므로 복원 대신 아래로 떨어뜨려 회수 루프로 탈출 (최후 보루)
            if (rb.linearVelocity.sqrMagnitude < 0.01f)
                rb.linearVelocity = Vector2.down * launchSpeed;
            return;
        }

        // 바닥에 한 번 튕긴 뒤에는 슈터를 향해 유도 비행 → 도착하면 회수(풀 반환은 BallShooter 몫)
        Vector2 toTarget = returnTarget - (Vector2)transform.position;
        if (toTarget.magnitude < ReturnArriveRadius)
        {
            OnExitField?.Invoke(this);
            return;
        }
        rb.linearVelocity = toTarget.normalized * launchSpeed;
    }

    // 수평 잠금 방지 임계 — 수평 기준 약 ±8.6° 이내면 이만큼 기울임 [눈튜닝]
    private const float MinVerticalRatio = 0.15f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 물리 반사 후 부동소수점 드리프트를 발사 속력으로 재정규화 (모든 충돌 공통).
        // 속도가 0으로 죽은 비정상 케이스(동시 접촉 상쇄 등)는 아래로 떨어뜨려 회수 루프로 탈출 (안전망)
        Vector2 velocity = rb.linearVelocity;
        Vector2 dir = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.down;

        // 수평 잠금 방지 (breakout류 공통 처리): 수직 성분이 임계 미만이면 기울여줌 — 아니면
        // 좌우 벽의 x반사만으로는 영원히 수평 왕복 (실기기 재현: 스폰 밀림으로 vy≈0 → 상단 복도에 갇힘).
        // 부호 유지, 완전 0이면 아래(회수 방향). 조준이 ±75° 제한이라 정상 플레이에선 발동 안 함
        if (Mathf.Abs(dir.y) < MinVerticalRatio)
        {
            float x = dir.x == 0f ? 1f : Mathf.Sign(dir.x);
            dir = new Vector2(x * Mathf.Sqrt(1f - MinVerticalRatio * MinVerticalRatio),
                              dir.y > 0f ? MinVerticalRatio : -MinVerticalRatio);
        }

        rb.linearVelocity = dir * launchSpeed;

        int layer = collision.gameObject.layer;
        if (wallLayer < 0) { wallLayer = LayerMask.NameToLayer("Wall"); monsterLayer = LayerMask.NameToLayer("Monster"); }
        if (layer == wallLayer)
        {
            WallBounceCount++;   // "벽" 카운트는 벽만 — 패시브 '마법 거울' 판정 근거
            // 바닥 벽(노멀이 위)에 한 번 튕기면 회수 모드. Wall 레이어 가드 필수 —
            // 블록 윗면에 떨어져도(노멀 위) 회수되면 안 되기 때문
            if (collision.GetContact(0).normal.y > 0.5f)
                isReturning = true;
        }
        else if (layer == monsterLayer)
        {
            // 원작: 볼은 블록(몬스터)에 맞으면 데미지를 주고 튕겨나감 — 소멸하지 않음.
            // 데미지 계산·적용은 이벤트를 받은 SkillManager가 담당 (Ball은 사실만 보고)
            OnHitMonster?.Invoke(this, collision.collider, collision.GetContact(0).normal);
        }
    }
}
