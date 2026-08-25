# TongTongDefence — 프로젝트 규약

PurpleCow 채용과제. **유저가 모든 코드를 면접에서 설명할 수 있어야 하므로, 유저의 이해가 결과물의 일부다.**
혼자 달리지 말 것 — 승인 게이트 밖에서 코드 파일을 수정하지 않는다.

작업 리듬·승인 게이트·설계 원칙의 SSOT = `~/.claude/rules/` (유저 스코프, 자동 로드).
운영 절차의 SSOT = `/dev-cycle` 스킬. 이 파일엔 **프로젝트 특수사항만** 남긴다.

## 개발 모드

- **기본 = `/dev-cycle`** (동기): 설계 루프(유저 주도) → 승인 → 구현 → 리뷰 위임 → 검증 → 커밋. 새 설계/로직 전부
- **예외 = `/auto-mode`** (배치): 유저 명시 발동 + 사전 승인 설계만. **복귀 게이트 = 검증 → 워크스루 → [가정] 결정 → 머지** — "승인 없이 만들 수는 있어도 이해 없이 머지할 수는 없다"
- 값 튜닝(숫자 1~2개)은 "X를 Y로 바꿈, ㄱ?" 한 줄 승인으로 축약
- 새 시스템은 원작 관찰/기획서 대조(design-check 0-A)가 설계보다 먼저
- 구현 후 전수 리뷰는 `/refactor-check` (발견만, 수정은 승인 후 별도 태스크)
- 검증: 컴파일은 AI 선확인(MCP recompile), 동작은 **유저 Play Mode** (체크리스트 제시)

## 레퍼런스 스킬 (이름만 알지 말고 실제 호출)

- 플랜 문서 작성 시 `superpowers:writing-plans`, 완료 선언 전 `superpowers:verification-before-completion`,
  구현 후 리뷰 시 `superpowers:requesting-code-review` — 해당 시점에 반드시 Skill 툴로 호출해 체크리스트를 탄다.

## 프로젝트 관례 (상세는 design-check 스킬 · 코드 스타일은 .claude/rules/code-style.md)

- 매니저: InGameManager 상속, 태그 "Manager", GameManager 프로퍼티+RegisterManager 등록 3종 세트
- 매니저 간 참조는 `GameManager.{Manager}` 경유. 프리팹 인스턴스(Ball/Monster)는 매니저를 모름 — 값만 전달
- 오브젝트 생성은 ObjectPool. FindObjectOfType/GameObject.Find 금지
- 필드 지오메트리는 FieldManager가 SSOT — 위치는 CellToWorld(행,열)로 지목
- **에이전트 창구**: MCP(인스펙터·씬·컴파일)는 메인 세션만. 워크트리 에이전트는 `.cs`만 — `.unity`/`.prefab` 편집 금지, 씬 작업은 인스펙터 값 표(명세서)로 산출
- 커밋 워크플로우: 구현 → 유저 Play 확인 → 즉시 커밋 → 다음 세션 리뷰
- **브랜치 (git-flow 축약)**: feature/* → **develop** 머지 (Task/기능 단위 분기, 한 브랜치 몰빵 금지).
  **master는 빌드용** — develop이 빌드 검증(APK)된 시점에만 develop → master 머지
