using UnityEngine;

public class FieldManager : InGameManager
{
    public float LeftWall { get; private set; }
    public float RightWall { get; private set; }
    public float TopWall { get; private set; }
    public float BottomWall { get; private set; }

    [SerializeField] private SpriteRenderer backgroundSprite;
    // 필드 = "배경 그림 bounds 안의 비율" (2026-07-07 전환) — 기준을 그림 하나로 통일.
    // 이전엔 화면 뷰포트 %였는데, 배경(그림)과 필드(화면)의 기준이 달라 화면비가 바뀌면 어긋났다.
    // 배경이 어디로 가든/어떻게 늘어나든 필드가 따라가므로 어긋남이 구조적으로 불가능.
    // 값은 배경을 9:16 화면에 꽉 채우던 기존 눈튜닝 %를 그대로 승계 (그때 화면 % == 그림 %).
    // 오른쪽 여백이 왼쪽보다 좁은 비대칭 — 그림의 판이 중앙에서 살짝 오른쪽이라 의도된 값.
    [UnityEngine.Serialization.FormerlySerializedAs("fieldViewportRect")]
    [SerializeField] private Rect fieldRectInBackground = new Rect(0.13f, 0.218f, 0.80f, 0.585f);

    // 논리 격자 — 필드를 columns×rows 셀로 나눈다. 모든 시스템이 (행, 열) 주소로 위치를 지목:
    // 스폰 대형, 블록 점유(1×2 등), 레이저볼 "같은 행" 판정의 공통 기반.
    [SerializeField] private int columns = 9;
    [SerializeField] private int rows = 13;

    public int Columns => columns;
    public int Rows => rows;
    public float CellWidth => (RightWall - LeftWall) / columns;
    public float CellHeight => (TopWall - BottomWall) / rows;

    // row 0 = 맨 윗줄(스폰줄), col 0 = 왼쪽. 셀의 "중심" 월드 좌표를 반환.
    public Vector2 CellToWorld(int row, int col)
        => new Vector2(LeftWall + (col + 0.5f) * CellWidth,
                       TopWall - (row + 0.5f) * CellHeight);

    public override void Initialize()
    {
        base.Initialize();
        Camera cam = Camera.main;
        float camZ = Mathf.Abs(cam.transform.position.z);

        FitBackgroundToCamera(cam, camZ);   // 배경을 먼저 펴고, 필드는 그 결과 bounds에서 파생 (그림 = SSOT)
        Bounds bg = backgroundSprite.bounds;
        LeftWall = bg.min.x + fieldRectInBackground.xMin * bg.size.x;
        RightWall = bg.min.x + fieldRectInBackground.xMax * bg.size.x;
        BottomWall = bg.min.y + fieldRectInBackground.yMin * bg.size.y;
        TopWall = bg.min.y + fieldRectInBackground.yMax * bg.size.y;

        CreateWallColliders();
        CreateFieldBoard();
    }

    // 필드 보드 — 배경(순수 우주) 위, 몬스터/볼 아래에 깔리는 격자판.
    // 그림에 좌표를 박으면 stretch/화면비로 어긋나므로, 벽처럼 코드가 생성해
    // 필드 rect(LeftWall~RightWall × BottomWall~TopWall)에 정확히 맞춘다 → 논리 격자와 100% 정렬.
    [SerializeField] private string fieldBoardResource = "Sprites/UI/FieldBoard";

    private void CreateFieldBoard()
    {
        Sprite sprite = Resources.Load<Sprite>(fieldBoardResource);
        if (sprite == null) return;

        var go = new GameObject("FieldBoard");
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -9;   // 배경(-10) 위, 몬스터/볼(0) 아래

        go.transform.position = new Vector3((LeftWall + RightWall) * 0.5f,
                                            (TopWall + BottomWall) * 0.5f, 0f);
        Vector2 baseSize = sprite.bounds.size;   // 스케일 1 기준 크기
        go.transform.localScale = new Vector3((RightWall - LeftWall) / baseSize.x,
                                              (TopWall - BottomWall) / baseSize.y, 1f);
    }

    // 배경을 화면(카메라 rect)에 정확히 맞춤 — 가로/세로 독립 배율(stretch fill).
    // 지급 리소스가 세로 원화를 2048² 정사각으로 압축해 담은 텍스처라 세로로 펴는 것이 원본 비율 복원.
    private void FitBackgroundToCamera(Camera cam, float camZ)
    {
        Vector3 screenBottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, camZ));
        Vector3 screenTopRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, camZ));
        float screenWidth = screenTopRight.x - screenBottomLeft.x;
        float screenHeight = screenTopRight.y - screenBottomLeft.y;

        backgroundSprite.drawMode = SpriteDrawMode.Simple;
        backgroundSprite.sortingOrder = -10; // 보드(-9)·몬스터/볼(0)보다 뒤
        Vector2 baseSize = backgroundSprite.sprite.bounds.size; // 스케일 1 기준 크기
        backgroundSprite.transform.localScale = new Vector3(
            screenWidth / baseSize.x, screenHeight / baseSize.y, 1f);
        backgroundSprite.transform.position = new Vector3(
            (screenBottomLeft.x + screenTopRight.x) * 0.5f,
            (screenBottomLeft.y + screenTopRight.y) * 0.5f,
            backgroundSprite.transform.position.z);
    }

    // 볼 반사 바닥을 판 바닥보다 얼마나 아래(뿌리지대) 둘지 — 슈터가 이 사이에 서 있어야
    // "볼이 슈터를 지나 내려가 → 바닥에 튕겨 → 슈터에게 돌아오는" 원작 회수 연출이 나온다
    [SerializeField] private float bottomWallDrop = 0.8f; // 슈터(약 -3.1)와 볼 반지름(0.2)을 고려한 여유 — 스폰 겹침 방지
    // 반사 벽(좌/우/상)을 판 가장자리보다 바깥으로 미는 여유 — 반사 공간 확보용.
    // 격자/몬스터 좌표는 불변, 벽만 밀림. Play 보며 인스펙터 튜닝.
    [SerializeField] private float wallPadding = 0.15f;

    public float WallPadding => wallPadding;   // 조준선 반사점 당김이 같은 값을 써야 함 (SSOT — 검수 v6)

    private void CreateWallColliders()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        float thickness = 1f;
        float left = LeftWall - wallPadding;
        float right = RightWall + wallPadding;
        float top = TopWall + wallPadding;
        float bounceFloorY = BottomWall - bottomWallDrop;
        float height = top - bounceFloorY; // 측벽도 반사 바닥까지 내려 필드를 완전한 사각형으로 밀폐
        float width = right - left;

        CreateWall("LeftWall", new Vector2(left - thickness / 2f, (top + bounceFloorY) / 2f), new Vector2(thickness, height), wallLayer, isTrigger: false);
        CreateWall("RightWall", new Vector2(right + thickness / 2f, (top + bounceFloorY) / 2f), new Vector2(thickness, height), wallLayer, isTrigger: false);
        CreateWall("TopWall", new Vector2((left + right) / 2f, top + thickness / 2f), new Vector2(width + thickness * 2f, thickness), wallLayer, isTrigger: false);
        // 반사 바닥 — 판 바닥(BottomWall 프로퍼티)보다 bottomWallDrop 아래(뿌리지대, 슈터 발밑).
        // 볼이 한 번 튕기면 회수 모드로 전환되어 슈터에게 돌아감 (Ball.OnCollisionEnter2D의 노멀 판정)
        CreateWall("BounceFloor", new Vector2((left + right) / 2f, bounceFloorY - thickness / 2f), new Vector2(width + thickness * 2f, thickness), wallLayer, isTrigger: false);
    }

    private void CreateWall(string name, Vector2 position, Vector2 size, int layer, bool isTrigger)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(transform);
        wall.transform.position = position;
        wall.layer = layer;
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = isTrigger;
    }

#if UNITY_EDITOR
    // Scene 뷰에 논리 격자(9×13)를 초록 선으로 그린다 — 몬스터/그림/필드를 눈으로 비교하는 진단 도구.
    // 에디터 전용(빌드 제외), Play 중이 아니어도 보임 (fieldViewportRect에서 즉석 계산)
    private void OnDrawGizmos()
    {
        if (backgroundSprite == null || columns <= 0 || rows <= 0) return;
        // 배경 bounds 기준 — 에디트 모드에서도 "그림 대비" 격자 정렬을 그대로 보여준다
        Bounds bg = backgroundSprite.bounds;
        Vector3 bl = new Vector3(bg.min.x + fieldRectInBackground.xMin * bg.size.x,
                                 bg.min.y + fieldRectInBackground.yMin * bg.size.y, 0f);
        Vector3 tr = new Vector3(bg.min.x + fieldRectInBackground.xMax * bg.size.x,
                                 bg.min.y + fieldRectInBackground.yMax * bg.size.y, 0f);
        float cellW = (tr.x - bl.x) / columns;
        float cellH = (tr.y - bl.y) / rows;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.9f);
        for (int c = 0; c <= columns; c++)
        {
            float x = bl.x + c * cellW;
            Gizmos.DrawLine(new Vector3(x, bl.y, 0f), new Vector3(x, tr.y, 0f));
        }
        for (int r = 0; r <= rows; r++)
        {
            float y = bl.y + r * cellH;
            Gizmos.DrawLine(new Vector3(bl.x, y, 0f), new Vector3(tr.x, y, 0f));
        }
    }
#endif
}
