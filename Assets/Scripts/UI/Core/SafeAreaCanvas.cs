using UnityEngine;


[RequireComponent(typeof(RectTransform))]
public class SafeAreaCanvas : MonoBehaviour
{
    [HideInInspector]
    public RectTransform rectTransform;


    private Rect lastApplied;
    private Vector2Int lastScreen;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeAreaCanvasAnchor();
    }

    // 1회 적용이 아니라 변화 감지 재적용 — 씬 리로드(재시작) 직후 프레임엔 Screen.width와
    // safeArea가 다른 기준으로 읽혀 앵커가 화면 밖(3.3배)으로 계산되던 실버그의 표준 해법.
    // 잘못 계산된 프레임이 있어도 다음 프레임에 자가 교정된다 (회전/시뮬레이터 전환도 자동 대응)
    private void Update()
    {
        // safeArea "또는" 화면 크기가 바뀌면 재적용 — 리로드 프레임엔 둘 중 어느 쪽이든 잘못 읽힐 수 있다
        // (실측: 앵커가 3.3배로 커지는 케이스와 0으로 붕괴하는 케이스 둘 다 관측됨)
        if (Screen.safeArea != lastApplied || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
            ApplySafeAreaCanvasAnchor();
    }

    public void ApplySafeAreaCanvasAnchor()
    {
        if (Screen.width <= 0 || Screen.height <= 0 || Screen.safeArea.width <= 0)
            return;   // 리로드 직후 무효 프레임 — 다음 프레임에 재시도

        var minAnchor = Screen.safeArea.position;
        var maxAnchor = Screen.safeArea.position + Screen.safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;

        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;
        lastApplied = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
    }

}

