using System;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : InGameManager
{
    // 씬 직렬화 소스 (에디터 버튼이 계층에서 자동 수집) — 조회는 아래 타입 색인으로만 한다.
    // 과거엔 "enum 정수 = 리스트 위치"로 조회했는데, 씬 계층 순서와 enum 순서가 몰래 일치해야 하는
    // 암묵 계약이라 순서가 틀어지면 엉뚱한 패널이 조용히 열렸다 (아웃게임 UIManager와 같은 패턴으로 통일, 검수 v6)
    public List<UIElement> uiElements;

    private readonly Dictionary<Type, UIElement> elementsByType = new();

    public override void Initialize()
    {
        base.Initialize();
        elementsByType.Clear();
        foreach (var element in uiElements)
        {
            if (element == null) continue;
            if (!elementsByType.TryAdd(element.GetType(), element))
                Debug.LogError($"[GameUIManager] {element.GetType().Name} 중복 등록 — 씬에 같은 UIElement가 2개");
            element.SetUIManager(GameManager, this);
        }

        GameManager.AddGameStateEnterAction(GameManager.GameState.GameStop, ShowUIElement<PausePanel>);
        GameManager.AddGameStateExitAction(GameManager.GameState.GameStop, HideUIElement<PausePanel>);
        GameManager.AddGameStateEnterAction(GameManager.GameState.GameOver, ShowUIElement<ResultPanel>);
    }

    public void InitializedUIElements()
    {
        foreach (var element in uiElements)
        {
            element.Initialize();
        }
    }

    // 씬 리스트에 미등록인 패널 호출 방어 — 실물 없는 유령 구독이 상태 전환 콜스택을
    // 예외로 끊는 실버그가 있었음 (GameOver 크래시). 미등록은 경고 후 무시
    public void ShowUIElement<T>() where T : UIElement
    {
        if (TryGetElement(typeof(T), out UIElement element)) element.Show();
    }

    public void HideUIElement<T>() where T : UIElement
    {
        if (TryGetElement(typeof(T), out UIElement element)) element.Hide();
    }

    private bool TryGetElement(Type type, out UIElement element)
    {
        if (!elementsByType.TryGetValue(type, out element))
        {
            Debug.LogWarning($"[GameUIManager] {type.Name} 미등록 — uiElements 리스트 확인");
            return false;
        }
        return true;
    }
}
