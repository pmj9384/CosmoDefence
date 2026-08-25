using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 결과 팝업 (기획서 필수 5항, UIElement) — 지표 계산/딜레이 로직만. 배치는 씬 소관 (에디터-네이티브).
// 무한모드라 승리 상태가 없다 — 타이틀은 항상 Game Over (원작의 Clear/Fail 이원 표시는 스테이지제 전제라 미적용, 검수 v5 #9)
public class ResultPanel : UIElement
{
    [SerializeField] private float failDelay = 1f;      // 죽음 연출(쓰러짐) 뜸

    [Header("씬 참조")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button lobbyButton;    // 아웃게임 복귀 — 뽑기/스킨 루프의 닫는 고리 (씬 수동 배치)

    private void Awake()
    {
        restartButton.onClick.AddListener(() => gameManager.RestartGame());
        if (lobbyButton != null) lobbyButton.onClick.AddListener(GoLobby);
    }

    private void GoLobby()
    {
        Time.timeScale = 1f;   // 결과 상태는 timeScale 0 — 복원 없이 씬을 바꾸면 로비가 얼어붙는다
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public override void Show()
    {
        StartCoroutine(ShowAfter(failDelay));
    }

    public override void Hide() => overlay.SetActive(false);

    // 지표는 딜레이가 끝난 뒤 읽는다 — GameOver 진입 액션(StatsManager.SubmitScore 등)이 모두 끝난 시점이라
    // 최고기록 갱신 순서에 안전. (결과 상태는 timeScale 0이라 반드시 Realtime 딜레이)
    private IEnumerator ShowAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // 무한모드: 진행도% 대신 이번 점수 + 최고(+신기록) + 코인 보상. 점수·코인 = StatsManager SSOT
        StatsManager stats = gameManager.StatsManager;
        titleText.text = "Game Over";
        string score = stats.IsNewRecord
            ? $"SCORE {stats.Score.Current:N0}   신기록!"
            : $"SCORE {stats.Score.Current:N0}";
        infoText.text = $"{score}\n최고 {stats.Best:N0}   +{stats.CoinsEarned:N0} 코인";
        overlay.SetActive(true);
    }
}
