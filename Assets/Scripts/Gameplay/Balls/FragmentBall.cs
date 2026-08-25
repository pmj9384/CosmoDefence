using System;
using UnityEngine;

// 클러스터볼 파편 — 타격 지점에서 위쪽 랜덤 방향으로 날아가 처음 만난 몬스터에 피해 주고 소멸 [가정2].
// 트리거 방식: 물리 반사 없이 통과하다가 몬스터만 감지 (벽엔 안 튕기고 수명/화면 밖으로 정리).
// 스폰 순간 원본 몬스터와 겹쳐 있으므로 잠깐의 무장 지연(ArmDelay)으로 즉발 자폭을 방지.
[RequireComponent(typeof(Rigidbody2D))]
public class FragmentBall : MonoBehaviour
{
    public event Action<FragmentBall> OnDespawn;   // 풀 반환은 소유자(BallManager) 몫

    private const float Lifetime = 3f;
    private const float Speed = 8f;
    private const float ArmDelay = 0.15f;

    private static int monsterLayer = -1;   // 문자열 레이어 조회 반복 회피 (충돌 경로)

    private int damage;
    private bool despawned;   // 같은 물리 패스에 큐된 두 번째 트리거의 이중 데미지 차단
    private float lifeTimer;
    private float armTimer;
    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 position, int fragmentDamage, System.Random rng)
    {
        transform.position = position;
        damage = fragmentDamage;
        lifeTimer = Lifetime;
        armTimer = ArmDelay;
        despawned = false;
        if (monsterLayer < 0) monsterLayer = LayerMask.NameToLayer("Monster");

        float angle = Mathf.Lerp(30f, 150f, (float)rng.NextDouble()) * Mathf.Deg2Rad;   // 위쪽 반원
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Speed;
    }

    private void Update()
    {
        armTimer -= Time.deltaTime;
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f && !despawned)
        {
            despawned = true;
            OnDespawn?.Invoke(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (armTimer > 0f || despawned) return;
        if (other.gameObject.layer != monsterLayer) return;

        despawned = true;   // "처음 만난 몬스터에" 계약 — 겹친 2기가 같은 패스에 트리거돼도 1회만
        other.GetComponent<Monster>()?.TakeDamage(damage, false, SkillId.ClusterBall);   // 부가 피해, 집계는 클러스터 귀속
        OnDespawn?.Invoke(this);
    }
}
