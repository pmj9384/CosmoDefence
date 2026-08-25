---
paths:
  - "Assets/Scripts/**/*.cs"
---

# C# 코드 스타일·주석 규약 (읽기 쉬움 = 결과물)

전제: 코드의 독자는 **면접의 나**와 **다음 에이전트** 둘 다다. 둘 다 파일만 읽고 추론할 수 있어야 한다. 읽기 어렵거나 일관성 없으면 그 코드는 미완성이다.

## 주석

- **클래스 머리 주석 (필수, 특히 매니저)** — 2~3줄로 "무엇을 소유/책임지는가 · 누구와 어떻게(이벤트/프로퍼티) 협력하는가". 파일 열자마자 역할이 잡히게.
- **WHY 주석 (적극)** — 비자명한 결정·버그 회피·트레이드오프의 *이유*를 남긴다. 예: `pendingRelease`를 LateUpdate에서 처리하는 이유. 6개월 뒤의 나/다음 에이전트는 못 물어본다.
- **WHAT 주석 금지** — 코드를 그대로 옮긴 주석(`hp -= dmg; // hp 감소`)은 소음. 달지 않는다.
- **인자는 이름으로 — 기계적 주석 금지** — 좋은 파라미터 이름이 1순위. 공개 API 중 *비자명한* 것만 XML `<summary>`/`<param>`. 인자마다 다는 주석은 시그니처가 바뀌면 거짓말이 된다.
- **주변과 같은 밀도** — 파일마다 주석 밀도·관용구를 맞춘다. 새 스타일을 국소 도입하지 않는다.

## 추상화

- **추상화가 주석보다 강한 가드다** — 인터페이스·단일책임·명확한 타입명으로 "여기선 이것만"을 강제하면, 에이전트가 혼자 판단해도 안 샌다.
- 로직을 기존 매니저에 욱여넣지 말 것 — 설계 첫 제안은 항상 SRP 분리 버전.
- 매니저 3종 세트(프로퍼티 + RegisterManager + "Manager" 태그), 매니저 간 참조는 `GameManager.{Manager}` 경유, 프리팹 인스턴스는 값만 받고 이벤트만 발행 — 이 경계를 깨지 않는다.

## 좋은 예 / 나쁜 예

좋음:
```csharp
// MonsterManager — 몬스터 스폰·필드 점유·데미지 이벤트 중계를 소유.
// GameManager로 형제 매니저를 참조하고, Monster 인스턴스와는 이벤트로만 대화한다.
public class MonsterManager : InGameManager
{
    // 물리 콜백 도중 콜라이더를 끄면 볼 반사가 깨져서, 제거를 LateUpdate로 미룬다.
    private readonly List<Monster> pendingRelease = new();
```

나쁨:
```csharp
public class MonsterManager : InGameManager  // 몬스터 매니저
{
    private readonly List<Monster> pendingRelease = new(); // 대기 리스트
    public void Spawn(int row) => spawner.Spawn(row);      // row에 스폰
```
