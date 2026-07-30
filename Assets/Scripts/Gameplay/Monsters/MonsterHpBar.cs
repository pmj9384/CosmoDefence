using UnityEngine;

// 몬스터 체력바 — 표시 갱신만 담당. 바의 구조·배치·크기·색은 전부 프리팹 소관 (에디터-네이티브,
// 2026-07-07 전환: 코드 생성 폐기 — "고정 구조 1개짜리는 프리팹 저작이 표준"이라는 유저 지적 반영).
// 다수 개체라 캔버스 대신 SpriteRenderer (모바일 관례). HP 값은 Monster 이벤트 구독.
public class MonsterHpBar : MonoBehaviour
{
    [Header("프리팹 배선 (HpBar > Background/Fill)")]
    [SerializeField] private GameObject barRoot;
    [SerializeField] private Transform fill;

    [Header("점유 높이별 바 y (유저 실측 2026-07-07 — 프리팹에서 튜닝)")]
    [SerializeField] private float barY1x1 = -0.635f;
    [SerializeField] private float barYTall = -1.1f;  // 세로 2칸(사슴)

    private const float DrainSpeed = 2.5f;   // 스르륵 줄어드는 속도(비율/초) — HUD 게이지와 감각 통일

    private Monster monster;
    private float baseWidth;    // 프리팹에서 저작된 가득 폭/기준 위치 — 코드가 덮지 않고 읽어서 씀
    private float baseX;
    private float displayRatio = 1f;
    private float targetRatio = 1f;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        monster.OnHpChanged += Refresh;
        baseWidth = fill.localScale.x;
        baseX = fill.localPosition.x;
    }

    private void OnDestroy()
    {
        if (monster != null) monster.OnHpChanged -= Refresh;
    }

    // 점유 높이별 바 위치 — 계산식 대신 실측값 (세로 멀티셀은 루트가 점유 중앙이라 좌표계가 다름)
    public void AlignToBottomCell(int cellHeight)
    {
        Vector3 pos = barRoot.transform.localPosition;
        pos.y = cellHeight > 1 ? barYTall : barY1x1;
        barRoot.transform.localPosition = pos;
    }

    private void OnEnable()
    {
        if (barRoot != null) barRoot.SetActive(false);   // 풀 재사용 — 피격 전엔 숨김
        displayRatio = targetRatio = 1f;
    }

    private void Refresh(int current, int max)
    {
        if (current <= 0) { barRoot.SetActive(false); return; }

        barRoot.SetActive(true);
        targetRatio = (float)current / max;   // 표시는 Update에서 보간
    }

    // 스르륵 줄어드는 연출 — 왼쪽 기준 채움: 폭 스케일 + 중심 보정 (Y/높이/색은 프리팹 값 그대로)
    private void Update()
    {
        if (!barRoot.activeSelf || displayRatio == targetRatio) return;

        displayRatio = Mathf.MoveTowards(displayRatio, targetRatio, DrainSpeed * Time.deltaTime);
        Vector3 scale = fill.localScale;
        scale.x = baseWidth * displayRatio;
        fill.localScale = scale;

        Vector3 pos = fill.localPosition;
        pos.x = baseX - baseWidth * (1f - displayRatio) * 0.5f;
        fill.localPosition = pos;
    }
}
