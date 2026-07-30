using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameUIManager))]
public class GameUIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // enum 생성 버튼은 제거 (검수 v6): 조회가 타입 딕셔너리로 바뀌어 UIElementEnums 자체가 사라짐.
        // 리스트는 여전히 계층 자동 수집 — 순서는 이제 조회에 영향 없음
        if (GUILayout.Button("Refresh UIElements List"))
        {
            var uiManager = (GameUIManager)target;
            Undo.RecordObject(uiManager, "Refresh UIElements List");
            uiManager.uiElements = uiManager.GetComponentsInChildren<UIElement>(true).ToList();
            EditorUtility.SetDirty(uiManager);
        }

        if (GUILayout.Button("Apply Kostar Font to All TMP"))
        {
            FontApplier.ApplyKostarFont();
        }

        base.OnInspectorGUI();
    }
}
