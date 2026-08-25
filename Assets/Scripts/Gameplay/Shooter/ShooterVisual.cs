using UnityEngine;

// 캐릭터 조준 연출 (원작 파츠 분리 의도 재현):
//   좌우 전환 = "루트 스케일 X 반전" (2D 미러링 정석) — 파츠 위치·아트가 통째로 거울상이 되므로
//   위치를 코드로 만질 필요가 없다 (배치는 전부 Inspector 소관).
//   머리 = 목 피벗 기준 기울임(각도 제한) / 지팡이 = 손잡이 피벗 기준 조준 회전.
public class ShooterVisual : MonoBehaviour
{
    [SerializeField] private Transform headPivot;      // 빈 부모 = 목 위치
    [SerializeField] private Transform weaponPivot;    // 빈 부모 = 손잡이 위치

    [SerializeField] private float maxHeadTilt = 35f;  // 고개 기울기 한계
    [SerializeField] private float turnSpeed = 720f;   // 회전 보간(도/초). 0 이하 = 즉시
    [SerializeField] private float weaponAngleOffset;  // 지팡이 아트 기울기 상쇄 [눈튜닝]
    // 아트 기본 방향(스케일 +1일 때 캐릭터가 보는 쪽)이 반대면 반전
    [SerializeField] private bool artFacesLeft = true;

    private float baseScaleX;
    private float currentAngle;

    private void Awake() => baseScaleX = Mathf.Abs(transform.localScale.x);

    public void SetAim(Vector2 direction)
    {
        bool mirror = artFacesLeft ? direction.x > 0f : direction.x < 0f;

        // 좌우 전환: 루트 미러 — 위치/아트/피벗 전부 자동으로 거울상
        Vector3 scale = transform.localScale;
        scale.x = mirror ? -baseScaleX : baseScaleX;
        transform.localScale = scale;

        // 회전각은 "미러 전 공간" 기준으로 계산 — 부모 스케일이 시각적으로 뒤집어 준다
        float localX = mirror ? -direction.x : direction.x;
        float targetAngle = Mathf.Atan2(direction.y, localX) * Mathf.Rad2Deg - 90f;   // 위(0,1) = 0°
        currentAngle = turnSpeed > 0f
            ? Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime)
            : targetAngle;

        if (headPivot != null)
            headPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(currentAngle, -maxHeadTilt, maxHeadTilt));
        if (weaponPivot != null)
            weaponPivot.localRotation = Quaternion.Euler(0f, 0f, currentAngle + weaponAngleOffset);
    }
}
