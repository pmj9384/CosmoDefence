using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(PlayerContext))]
public class Shooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private SkillIconTable iconTable;   // 볼 스프라이트 매핑 (BallShooter에 주입)
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private ShooterVisual visual;   // 조준 연출 (파츠 배치는 씬 — 미연결이어도 동작)
    [SerializeField] private Transform aimDot;       // 조준선 끝 원형 조준점 (빌더 메뉴로 생성·연결)

    [Header("Shooting")]
    [SerializeField] private float shootCooldown = 1f;
    [SerializeField] private float ballSpeed = 12f;

    [Header("Aiming")]
    [SerializeField] private int maxBounces = 2;
    [SerializeField] private float maxDistance = 30f;

    private PlayerContext context;
    private BallShooter shooter;
    private ShooterAimer aimer;
    private ShooterInputHandler inputHandler;
    private Vector2 direction = Vector2.up;

    // GameManager.Awake()의 매니저 주입이 끝난 뒤여야 하므로 Awake가 아닌 Start에서 구성
    private void Start()
    {
        context = GetComponent<PlayerContext>();   // 매니저 관문 (형제 공유) — 배선은 관문 1곳뿐

        // 슈터 X를 필드 중심에서 파생 — 필드 rect가 바뀌어도 자동 추종, 씬 고정값 드리프트 차단.
        // FieldManager.Initialize()는 GameManager.Awake()에서 먼저 끝나므로(28행 주석 참고) 여기서 값이 준비돼 있음
        Vector3 pos = transform.position;
        pos.x = context.FieldCenterX;
        transform.position = pos;

        BallManager ballManager = context.Balls;
        inputHandler = new ShooterInputHandler(transform);
        shooter = new BallShooter(ballManager, ballPrefab, transform, shootCooldown, ballSpeed, iconTable);
        aimer = new ShooterAimer(ballManager, lineRenderer, transform, maxBounces, maxDistance);
        aimer.SetAimDot(aimDot);

        inputHandler.OnDirectionChanged += dir => direction = dir;
    }

    private void Update()
    {
        if (inputHandler == null) return;

        // 조준선은 항상 Tick — 자체 상태 가드가 정지 시 점/레티클 숨김을 담당 (스킵하면 박제됨)
        aimer.Tick(direction);

        // GamePlay가 아니면 입력·발사·캐릭터 회전 정지 — 일시정지/스킬 선택 중 캐릭터가 입력 따라 돌던 버그
        if (context.Balls.CurrentGameState != GameManager.GameState.GamePlay)
            return;

        inputHandler.Tick();
        shooter.Tick(Time.deltaTime, direction);
        if (visual != null) visual.SetAim(direction);
    }
}
