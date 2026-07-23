using System;
using UnityEngine;

// 비매니저(Shooter 계열)가 protected GameManager에 닿는 유일한 창구 — 필요한 것만 최소 노출.
// 볼 히트 이벤트의 중계 지점이자, 파편볼(FragmentBall) 생명주기의 집이기도 하다 (파편도 볼이므로).
public class BallManager : InGameManager
{
    [SerializeField] private GameObject fragmentPrefab;   // 클러스터 파편볼 (SkillManager에서 이관 — 백로그 D12)

    // (ball, 맞은 몬스터 콜라이더, 충돌 노멀 — 단검 전/후면 판정용)
    public event Action<Ball, Collider2D, Vector2> OnBallHitMonster;

    public GameManager.GameState CurrentGameState => GameManager.CurrentState;
    public ObjectPoolManager ObjectPool => GameManager.ObjectPool;

    private UnityEngine.Pool.ObjectPool<GameObject> fragmentPool;
    private readonly System.Random rng = new();   // 파편 산탄 방향용

    public override void Initialize()
    {
        base.Initialize();
        fragmentPool = GameManager.ObjectPool.CreateObjectPool(
            fragmentPrefab,
            createFunc: () => Instantiate(fragmentPrefab),
            onGet: o => o.SetActive(true),
            onRelease: o => o.SetActive(false));
    }

    // ── 발사/회수 창구 — 볼 인벤토리(대기열)는 SkillManager 소관 ─────────────

    public bool TryGetNextLoadout(out BallLoadout loadout) => GameManager.SkillManager.TryGetNextLoadout(out loadout);
    public void ReturnBall(SkillId? skill) => GameManager.SkillManager.ReturnBall(skill);

    public void NotifyBallHitMonster(Ball ball, Collider2D monster, Vector2 hitNormal)
        => OnBallHitMonster?.Invoke(ball, monster, hitNormal);

    // ── 파편볼 풀 — 클러스터 효과가 델리게이트로 호출, 효과 클래스는 풀의 존재를 모른다 ──

    public void SpawnFragment(Vector2 position, int damage)
    {
        FragmentBall fragment = fragmentPool.Get().GetComponent<FragmentBall>();
        fragment.OnDespawn += HandleFragmentDespawn;
        fragment.Launch(position, damage, rng);
    }

    private void HandleFragmentDespawn(FragmentBall fragment)
    {
        fragment.OnDespawn -= HandleFragmentDespawn;
        fragmentPool.Release(fragment.gameObject);
    }
}
