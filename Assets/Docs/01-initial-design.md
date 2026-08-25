# 통통디펜스 핀볼 마스터 1스테이지 — 설계 문서

## 개요

PurpleCow 채용과제. Unity 6000.3.10 / Android 타겟. 핀볼 볼을 발사해 몬스터를 처치하고 20웨이브를 클리어하는 로그라이크 디펜스 게임.

---

## 아키텍처

### 핵심 원칙

- **Manager 등록/초기화**: AnimalBreakOut 패턴 — `GameManager`가 태그 "Manager"로 InGameManager를 수집하고 `SetGameManager(this)`로 참조 주입
- **Manager 간 통신**: C# event/Action — Manager가 다른 Manager를 직접 호출하지 않고 이벤트로 분리
- **GameManager**: MonoBehaviour (싱글톤 아님) — 씬 단위로 생존, 아웃게임 씬에 영향 없음

### 씬 오브젝트 구조

```
[GameManager]              MonoBehaviour, List<IManager> 등록/초기화
[BallManager]              InGameManager (태그: Manager)
  - ShooterInputHandler      MonoBehaviour, 스와이프 입력
  - ShooterAimer             MonoBehaviour, Raycast 궤적 LineRenderer
[MonsterManager]           InGameManager (태그: Manager)
[WaveManager]              InGameManager (태그: Manager)
[SkillManager]             InGameManager (태그: Manager)
[UIManager]                InGameManager (태그: Manager), GameUIManager
[Canvas]
  - HUDPanel               UIElement
  - SkillSelectionPanel    UIElement
  - GameOverPanel          UIElement
  - GameClearPanel         UIElement
```

### GameState

```
WaitLoading → GameReady → GamePlay ⇄ SkillSelection
                                  → GameOver
                                  → GameClear
```

---

## 볼 시스템 (BallManager)

### 이동 방식

- **실제 이동**: `Rigidbody2D` + `gravityScale = 0` + `linearVelocity` 직접 설정
- **벽 반사**: `FixedUpdate()`에서 x 위치 체크 후 `velocity.x` 수동 반전 (BubbleShooter 동일 방식)
- **궤적 미리보기**: `Physics2D.Raycast` 연속 호출 (벽 반사 횟수만큼) → `LineRenderer`로 렌더링
- **착지 없음**: 몬스터 충돌 시 데미지 적용 후 볼 지속 또는 파괴 (스킬에 따라 다름)

### 발사 방식

- 하단 고정 위치에서 **쿨다운마다 자동 발사**
- **스와이프 좌우**: 발사 각도 조정 (`ShooterInputHandler` → `BallManager` 방향 업데이트)

### 기본 수치

| 항목 | 값 |
|---|---|
| 노멀 볼 충돌 데미지 | 8 |
| 기본 치명타 확률 | 0% |
| 기본 치명타 데미지율 | 50% |

### Ball 클래스

```
Ball : MonoBehaviour
  - Rigidbody2D
  - int wallBounceCount          // 마법 거울 패시브용
  - List<IActiveSkillEffect>     // 장착된 액티브 스킬 효과
  - event Action<Ball, Vector2>  OnHitWall
  - event Action<Ball, Monster>  OnHitMonster
```

---

## 몬스터 시스템 (MonsterManager)

- 웨이브마다 MonsterManager가 몬스터를 `MonsterField` 영역에 스폰
- 오브젝트 풀링으로 재사용
- 몬스터는 행(Row) 단위로 배치, 웨이브 종료 시 한 행씩 전진

```
Monster : MonoBehaviour, IDamageable
  - int maxHp, currentHp
  - int row, col                 // 필드 내 위치
  - event Action<Monster> OnDied
  - void TakeDamage(int damage, bool isCritical)
```

---

## 웨이브 시스템 (WaveManager)

- 총 20웨이브
- 각 웨이브마다 **킬 조건 수** 설정 → 달성 시 스킬 선택 UI 트리거
- 웨이브 내 모든 몬스터 처치 → 다음 웨이브

### 이벤트

```csharp
public event Action OnKillConditionMet;   // → SkillManager 구독
public event Action OnWaveAllClear;       // → 다음 웨이브 or GameClear
```

---

## 스킬 시스템 (SkillManager)

### 보유 제한

| 구분 | 최대 보유 |
|---|---|
| 액티브 스킬 | 4개 |
| 패시브 스킬 | 2개 |

### 선택 규칙

- 조건 충족 시 3택지 노출
- 미보유 스킬 → Lv.1만 노출
- 최대 보유 시 → 보유 스킬 업그레이드만 노출 (액티브/패시브 별도)
- 동일 스킬 카드 중복 없음 (레벨업만 가능)

### 액티브 스킬 (볼 타입)

| 스킬 | Lv1 | Lv2 | Lv3 | 볼 데미지 |
|---|---|---|---|---|
| 파이어볼 | 4초 화상 최대3중첩, 중첩당 초당 8피해 | 4.5초 최대4중첩, 초당 10피해 | 5초 최대5중첩, 초당 12피해 | 21/24/27 |
| 아이스볼 | 30% 5초 냉동, 이동속도-10%, 추가피해 10% | 35% 6초, -15%, 추가피해 15% | 40% 7초, -20%, 추가피해 20% | 25/37/50 |
| 레이저볼 | 같은 행 모든 적 7피해 | 11피해 | 15피해 | 11/15/19 |
| 고스트볼 | 충돌 시 적 관통 | 관통 | 관통 | 14/21/28 |
| 클러스터볼 | 타격 시 40% 확률 10피해 특수볼 생성 | 50% 확률 15피해 | 60% 확률 20피해 | 27/30/33 |

### 패시브 스킬

| 스킬 | Lv1 | Lv2 | Lv3 |
|---|---|---|---|
| 따뜻한 양철 심장 | 노멀 볼 추가 피해 +20% | +30% | +40% |
| 마법 거울 | 벽 충돌마다 다음 타격 +20% | +40% | +60% |
| 자수정 단검 | 적 전면 타격 시 치명타 확률 +10% (타격 적 한정 1회) | +20% | +30% |
| 에메랄드 단검 | 적 후면 타격 시 치명타 확률 +20% (타격 적 한정 1회) | +30% | +40% |
| 마지막 성냥 | 적 사망 시 근처 적에게 폭발 10피해 | 20피해 | 30피해 |

### 스킬 인터페이스

```csharp
interface IActiveSkillEffect
    void OnHitMonster(Ball ball, Monster monster)

interface IPassiveSkillEffect
    void OnHitWall(Ball ball)           // 마법 거울
    void OnMonsterDied(Monster monster) // 마지막 성냥
    void ModifyDamage(ref DamageInfo info, Ball ball, Monster monster) // 양철심장, 단검
```

---

## UI 시스템

모든 인게임 UI는 `UIElement` 서브클래스로 구현 (Show/Hide 방식).

| 패널 | 내용 |
|---|---|
| HUDPanel | 현재 웨이브, 보유 스킬 아이콘 |
| SkillSelectionPanel | 3장 스킬 카드 선택 |
| GameOverPanel | 실패 결과, 재시작 버튼 |
| GameClearPanel | 클리어 결과, 재시작 버튼 |

---

## 이벤트 흐름 요약

```
스와이프 → ShooterInputHandler.OnDirectionChanged → BallManager 방향 업데이트

쿨다운 → BallManager: Ball 생성 및 발사

Ball.OnHitWall → SkillManager: 마법 거울 체크 (다음 타격 피해 증가)

Ball.OnHitMonster
  → DamageInfo 계산 (기본 + 패시브 보정 + 치명타 판정)
  → Monster.TakeDamage()
  → SkillManager: 액티브 효과 발동 (화상 DoT, 냉동, 레이저, 관통, 특수볼)

Monster.OnDied
  → WaveManager.OnMonsterKilled() (킬 카운트 증가)
  → SkillManager: 마지막 성냥 체크 (주변 적 폭발 피해)

WaveManager.OnKillConditionMet
  → GameManager.SetGameState(SkillSelection)
  → SkillSelectionPanel.Show()

WaveManager.OnWaveAllClear
  → 마지막 웨이브면 GameClear, 아니면 다음 웨이브 시작
```

---

## 구현 제외 항목

- 튜토리얼
- 배속 기능
- 1스테이지 보스
- 자동 조준
- 선택지 다시뽑기
- 융합 시스템
