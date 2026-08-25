using System;
using UnityEngine;

// Shooter GO의 매니저 관문 (2026-07-07 유저 결정) — 컴포넌트마다 매니저를 직렬화로 들면서
// 한 오브젝트에 참조가 3종(Ball/Game/Player) 흩어지던 것을 여기 하나로 통합.
// 형제(Shooter/PlayerDeathPose/PlayerHitFlash)는 GetComponent로 이 관문만 본다 — 배선 3줄 → 1줄.
// GameManager는 이 관문 "내부"에만 존재하고, 밖으로는 필요한 창구만 좁게 노출한다.
public class PlayerContext : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    public BallManager Balls => gameManager.BallManager;          // 발사/조준 창구
    public PlayerHealth Health => gameManager.PlayerManager.Health;   // 피격 연출용

    // Shooter가 화면 중앙 정렬용으로 쓰는 값만 좁게 노출 — FieldManager 존재 자체는 모르게
    public float FieldCenterX => (gameManager.FieldManager.LeftWall + gameManager.FieldManager.RightWall) * 0.5f;

    // 죽음 연출용 — 상태 머신 전체가 아니라 "GameOver 진입"만 계약으로 노출
    public void AddGameOverEnter(Action action) => gameManager.AddGameStateEnterAction(GameManager.GameState.GameOver, action);
    public void RemoveGameOverEnter(Action action)
    {
        if (gameManager != null)
            gameManager.RemoveGameStateEnterAction(GameManager.GameState.GameOver, action);
    }
}
