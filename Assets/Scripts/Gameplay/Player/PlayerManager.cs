using UnityEngine;

// 플레이어 상태의 주인 — 체력 보유, 소진 시 GameOver 전환. (몬스터 도달 = 즉사가 아니라 체력전 — 원작 관찰)
public class PlayerManager : InGameManager
{
    [SerializeField] private int maxHealth = 300;        // 원작 게이지 관찰값
    [SerializeField] private Transform playerTransform;  // Shooter — 도달 몬스터의 돌진 목적지 (수동 연결: YAML)
    [SerializeField] private float dropBelowField = 0.1f; // 판 바닥에서 얼마나 아래 설지 — 9:16 기준 기존 씬 좌표(0.15, -3.14) 재현값

    public PlayerHealth Health { get; private set; }
    public Vector2 Position => playerTransform.position;

    public override void Initialize()
    {
        base.Initialize();
        // 슈터 위치를 필드에서 파생 — 씬 고정 좌표는 화면비마다 판과의 상대 위치가 어긋남 (실기기 확인).
        // 배경→필드→슈터→(자식) HP바까지 한 좌표계 사슬로 묶여 어떤 폰에서도 정합
        var field = GameManager.FieldManager;
        playerTransform.position = new Vector3(
            (field.LeftWall + field.RightWall) * 0.5f, field.BottomWall - dropBelowField, 0f);

        Health = new PlayerHealth(maxHealth);
        Health.OnDied += HandleDied;
    }

    public override void Clear()
    {
        base.Clear();
        Health.OnDied -= HandleDied;
    }

    public void TakeDamage(int damage)
    {
        Health.TakeDamage(damage);
        SoundManager.Instance?.PlaySfx(SfxClipId.PlayerHit);
    }

    private void HandleDied()
    {
        GameManager.SetGameState(GameManager.GameState.GameOver);
    }
}
