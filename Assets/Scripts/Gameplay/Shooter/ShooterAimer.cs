using System.Collections.Generic;
using UnityEngine;

// 조준 표시 — 원작(#52) 방식 그대로: 선이 아니라 "작은 점 스프라이트를 경로에 나열" + 끝점 레티클.
// (LineRenderer 점선 텍스처는 Tile 모드 타일링 제어가 불안정해서 폐기 — 점 나열이 밀도/크기 완전 제어)
// 점 오브젝트는 동적 연출용 풀 — 데미지 팝업과 같은 "코드 생성 예외" 부류.
public class ShooterAimer
{
    private const float LineStartOffset = 0.45f;   // 캐릭터 스프라이트를 뚫고 시작하지 않게
    private const float DotSpacing = 0.28f;        // 점 간격 [눈튜닝]
    private const float ReticleSpinSpeed = 120f;   // 레티클 회전 속도(도/초) — 원작 연출 [눈튜닝]
    private const float DotSize = 0.55f;           // 점 크기(스케일) [눈튜닝]
    private const int MaxDots = 80;

    private readonly BallManager ballManager;
    private readonly Transform origin;
    private readonly int maxBounces;
    private readonly float maxDistance;
    private readonly int aimMask;
    private readonly int monsterLayer;

    private readonly List<Vector3> points = new();          // 재사용 (Update 내 할당 회피)
    private readonly List<SpriteRenderer> dots = new();     // 점 풀 — 필요한 만큼만 켬
    private readonly Transform dotsRoot;
    private readonly Sprite dotSprite;
    private readonly float wallPadding;   // 반사점 안쪽 당김 (FieldManager SSOT에서 주입)

    private Transform aimDot;   // 끝점 레티클 (씬 오브젝트 — Shooter가 넘겨줌)
    public void SetAimDot(Transform dot) => aimDot = dot;

    public ShooterAimer(BallManager ballManager, LineRenderer lineRenderer, Transform origin, int maxBounces, float maxDistance)
    {
        this.ballManager = ballManager;
        this.origin = origin;
        this.maxBounces = maxBounces;
        this.maxDistance = maxDistance;
        wallPadding = ballManager.WallPadding;   // 생성 시점(Shooter.Start)엔 FieldManager 선등록 — 안전
        aimMask = LayerMask.GetMask("Wall", "Monster");
        monsterLayer = LayerMask.NameToLayer("Monster");

        if (lineRenderer != null) lineRenderer.positionCount = 0;   // 옛 실선 잔재 끔 (컴포넌트는 무해)

        dotSprite = Resources.Load<Sprite>("Sprites/UI/aim_dot");
        dotsRoot = new GameObject("AimDots").transform;
    }

    public void Tick(Vector2 direction)
    {
        if (ballManager.CurrentGameState != GameManager.GameState.GamePlay)
        {
            HideAll();
            return;
        }

        CalculateTrajectory(origin.position, direction);
        DrawDots();

        if (aimDot != null)   // 위치 = 조준 "결과" 반영
        {
            aimDot.gameObject.SetActive(true);
            aimDot.position = points[points.Count - 1];
            aimDot.Rotate(0f, 0f, -ReticleSpinSpeed * Time.deltaTime);   // 원작: 레티클 시계방향(오른쪽) 회전
        }
    }

    // 경로 세그먼트를 따라 일정 간격으로 점 배치 (꺾임 지점을 넘어 이어짐)
    private void DrawDots()
    {
        int used = 0;
        float leftover = 0f;   // 세그먼트 경계에서 간격 이어붙이기

        for (int s = 0; s < points.Count - 1 && used < MaxDots; s++)
        {
            Vector3 a = points[s];
            Vector3 b = points[s + 1];
            float length = Vector3.Distance(a, b);
            Vector3 dir = (b - a) / Mathf.Max(length, 0.0001f);

            for (float t = leftover; t < length && used < MaxDots; t += DotSpacing)
            {
                SpriteRenderer dot = GetDot(used++);
                dot.gameObject.SetActive(true);
                dot.transform.position = a + dir * t;
            }
            leftover = (leftover + Mathf.Ceil((length - leftover) / DotSpacing) * DotSpacing) - length;
        }

        for (int i = used; i < dots.Count; i++)   // 남는 점은 끔
            dots[i].gameObject.SetActive(false);
    }

    private SpriteRenderer GetDot(int index)
    {
        while (dots.Count <= index)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(dotsRoot, false);
            go.transform.localScale = Vector3.one * DotSize;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.color = new Color(0.75f, 0.75f, 0.72f, 0.9f);   // 원작: 회색
            sr.sortingOrder = 4;
            dots.Add(sr);
        }
        return dots[index];
    }

    private void HideAll()
    {
        for (int i = 0; i < dots.Count; i++)
            dots[i].gameObject.SetActive(false);
        if (aimDot != null) aimDot.gameObject.SetActive(false);
    }

    private void CalculateTrajectory(Vector2 startPos, Vector2 dir)
    {
        points.Clear();
        Vector2 vel = dir.normalized;
        startPos += vel * LineStartOffset;
        points.Add(startPos);

        Vector2 pos = startPos;
        float remaining = maxDistance;

        for (int bounce = 0; bounce < maxBounces && remaining > 0; bounce++)
        {
            var hit = Physics2D.Raycast(pos, vel, remaining, aimMask);
            if (hit.collider == null)
            {
                points.Add(pos + vel * remaining);
                break;
            }

            // 벽 반사점 표시는 안쪽으로 당김 — 물리 벽은 그림 벽보다 wallPadding만큼 바깥이라
            // 그대로 그리면 조준선이 그림 벽을 뚫고 꺾여 보임 (값은 FieldManager가 SSOT, 생성자에서 주입)
            bool isWall = hit.collider.gameObject.layer != monsterLayer;
            points.Add(isWall ? hit.point + hit.normal * wallPadding : hit.point);
            remaining -= hit.distance;

            if (!isWall)
                break;

            vel = Vector2.Reflect(vel, hit.normal);
            // 표면 정확히 그 점에서 재출발하면 같은 벽을 거리 0으로 재히트 → 진행 방향으로 살짝 밀어 탈출
            pos = hit.point + vel * 0.01f;
        }
    }
}
