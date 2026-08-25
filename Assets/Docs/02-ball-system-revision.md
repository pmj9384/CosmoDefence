# 통통디펜스 볼 시스템 설계 개정 — 2026-07-01

> 이 문서는 `2026-06-30-설계.md`의 **"볼 시스템(BallManager)" 섹션만** 구현 과정에서 드러난 실제 구조로 갱신한 것이다. 몬스터/웨이브/스킬/UI 시스템은 변경 없음 — 기존 문서 참고.

## 변경 사유

Task 1 구현 착수 직후 design-check 스킬로 재검증한 결과, 원래 볼 시스템 설계의 BallManager가 SRP를 위반하고 있었다:

> "쿨다운마다 볼을 생성/발사하고, **그리고** 방향을 갱신하고, **그리고** 필드 경계를 계산하고, **그리고** 충돌 이벤트로 데미지까지 처리한다" — 책임 4개가 한 클래스에 뭉쳐있었음.

또한 실제 플레이 테스트 중 "발사 위치 → 터치 지점" 조준 방식(포인트 투 슛)이 필요하다는 게 드러나 스와이프 델타 방식에서 변경됨.

---

## 씬 오브젝트 구조 (갱신)

```
[BallManager]              InGameManager (태그: Manager) — 얇은 조정자
  - BallShooter             MonoBehaviour, 쿨다운→발사, ObjectPool 사용
  - ShooterInputHandler     MonoBehaviour, 포인트 투 슛 방향 계산
  - ShooterAimer            MonoBehaviour, Raycast 궤적 LineRenderer (예정, Task 5)

[Shooter]                   최상위 오브젝트, 발사 위치(월드 좌표)만 가짐
                            — BallManager 하위가 아님. "실제 위치를 갖는
                              엔티티"는 로직 전용 매니저 밑에 두지 않는다.

[Ball.prefab]               개별 인스턴스, ObjectPoolManager로 풀링
```

### 오브젝트 배치 원칙 (신규 확립)

- **자기만의 위치가 필요 없고 씬에 단일 개체로만 존재하는 로직**(BallManager/BallShooter/InputHandler/Aimer) → 같은 오브젝트에 형제 컴포넌트로 모음. `GetComponent<T>()`로 서로를 직접 찾음 (씬 전체 검색이 아니라 같은 오브젝트 안에서만 찾는 것이므로 design-check의 "FindObjectOfType 금지" 규칙과 무관).
- **실제 월드 위치를 갖는 엔티티**(Shooter, Ball) → 별도 오브젝트/프리팹으로 분리.

---

## BallManager — 역할 재정의

```csharp
public class BallManager : InGameManager
{
    public float LeftWall { get; }
    public float RightWall { get; }
    public float TopWall { get; }

    // BallShooter 등 형제 컴포넌트가 protected GameManager에 접근 못 하므로
    // 필요한 것만 최소한으로 노출하는 패스스루
    public GameManager.GameState CurrentGameState => GameManager.CurrentState;
    public ObjectPoolManager ObjectPool => GameManager.ObjectPool;

    public override void Initialize()
    {
        // 카메라 기준 FieldBounds 계산만 함 — 그 외 아무 로직 없음
    }
}
```

**BallManager가 하지 않는 것**: 발사 로직, 방향 상태 보관, 충돌 이벤트 처리 — 전부 하위 컨트롤러 몫.

---

## BallShooter (신규 컴포넌트, 원래 계획엔 없었음)

기존 설계문서는 "BallManager가 쿨다운마다 자동 발사"라고만 돼 있었으나, 이 로직을 `BallShooter`로 분리했다.

```csharp
public class BallShooter : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform shootOrigin;   // Shooter 오브젝트 참조
    [SerializeField] private float shootCooldown = 1f;
    [SerializeField] private float ballSpeed = 12f;

    // Awake에서 GetComponent<ShooterInputHandler>()로 형제 참조 후 직접 구독
    // Start에서 GameManager.ObjectPool로 Ball 풀 생성
    // Update에서 CurrentGameState == GamePlay일 때만 쿨다운 진행 → Shoot()
}
```

`ObjectPoolManager`(AnimalBreakOut 템플릿에서 가져왔지만 어제까지 미사용 상태였음)를 여기서 처음 실사용.

---

## ShooterInputHandler — 포인트 투 슛으로 변경

**기존 설계(스와이프 델타)**: 드래그 시작점 대비 이동 거리/방향으로 각도 조정.

**변경된 설계(포인트 투 슛)**: 발사 위치(`Shooter` 오브젝트)에서 터치/마우스 스크린 좌표를 월드로 변환한 지점을 향하는 방향을 매 프레임 계산.

```csharp
private void UpdateDirection(Vector2 screenPos)
{
    Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZ));
    Vector2 dir = ((Vector2)worldPos - (Vector2)shootOrigin.position).normalized;
    if (dir.y < 0.1f) dir = new Vector2(dir.x, 0.1f).normalized;   // 아래 방향 발사 방지
    OnDirectionChanged?.Invoke(dir);
}
```

`OnDirectionChanged` 이벤트 시그니처(`Action<Vector2>`)는 변경 없음 — `BallShooter`/`ShooterAimer`는 이벤트만 구독하면 되므로 이 변경에 영향받지 않음.

---

## 이벤트 흐름 (갱신)

```
터치/마우스 누르고 있음 → ShooterInputHandler.UpdateDirection()
  → OnDirectionChanged 발행 (발사 위치 → 터치 지점 방향)
      ├──▶ BallShooter가 직접 구독 (다음 발사 방향에 반영)
      └──▶ ShooterAimer가 직접 구독 (궤적 미리보기 갱신, Task 5 예정)

쿨다운 만료 → BallShooter.Shoot()
  → ObjectPool.Get()으로 Ball 인스턴스 획득
  → Ball.Launch(direction, speed, FieldBounds)

Ball.FixedUpdate() → 좌/우/위 벽 반사 (좌표 비교, 물리 콜라이더 없음)
```

---

## 알려진 트레이드오프 / 임시 조치

- **GameManager.SkipTitle = true (Awake, 임시)**: 아웃게임 씬이 없어서 `GamePlay` 상태 진입이 안 되던 문제 해결용. 아웃게임 씬 추가되면 제거하고 정상 씬 전환 흐름으로 교체.
- **"Monster" 태그를 몬스터 시스템보다 먼저 등록**: `Ball.OnTriggerEnter2D`의 `CompareTag("Monster")`가 미등록 태그로 크래시하던 문제 해결. 태그만 등록, 몬스터 오브젝트/시스템은 여전히 없음.
- **ObjectPool.Release() 미구현**: 현재 `BallShooter`는 `Get()`만 하고 반환 로직이 없음. 몬스터 충돌/필드 이탈 처리 시스템과 함께 추가 예정.
