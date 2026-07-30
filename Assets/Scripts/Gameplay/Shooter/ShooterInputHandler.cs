using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShooterInputHandler
{
    public event Action<Vector2> OnDirectionChanged;

    private readonly Transform origin;

    public ShooterInputHandler(Transform origin)
    {
        this.origin = origin;
    }

    public void Tick()
    {
        if (Touchscreen.current != null)
            HandleTouch();
        else
            HandleMouse();
    }

    private void HandleTouch()
    {
        var touch = Touchscreen.current.primaryTouch;
        if (touch.press.isPressed)
            UpdateDirection(touch.position.ReadValue());
    }

    private void HandleMouse()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
            UpdateDirection(Mouse.current.position.ReadValue());
    }

    // 조준 각도 한계 — 수직(위) 기준 좌우 최대 각. 필드 하단 끝자락 방향(≈75°)까지만 꺾임 (원작 감각)
    private const float MaxAngleFromUp = 75f;

    private void UpdateDirection(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;   // MainCamera 태그 부재 — 드래그 첫 프레임 NRE 방지
        float camZ = Mathf.Abs(cam.transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZ));

        Vector2 dir = ((Vector2)worldPos - (Vector2)origin.position).normalized;

        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;   // 위 기준 좌우 각 (+오른쪽)
        angle = Mathf.Clamp(angle, -MaxAngleFromUp, MaxAngleFromUp);
        float rad = angle * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

        OnDirectionChanged?.Invoke(dir);
    }
}
