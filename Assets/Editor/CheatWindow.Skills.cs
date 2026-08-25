using UnityEditor;
using UnityEngine;

// TongTongDefence 고유 치트 — CheatWindow(base)의 partial 훅 DrawProjectCheats()를 채운다.
// 스킬은 이 프로젝트 전용이라 base(템플릿)에 두지 않고 여기서만 잇는다.
public partial class CheatWindow
{
    private SkillId skillToLevel = SkillId.FireBall;

    partial void DrawProjectCheats()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("스킬", EditorStyles.boldLabel);
        skillToLevel = (SkillId)EditorGUILayout.EnumPopup("대상", skillToLevel);
        if (GUILayout.Button("선택 스킬 +1레벨 (미보유면 획득 / 만렙 액티브볼은 +1개)"))
        {
            // 디버그라 Find 허용 — SkillManager는 주입식이라 static Instance가 없다
            SkillManager skills = FindAnyObjectByType<SkillManager>();
            if (skills != null) skills.DebugLevelUp(skillToLevel);
            else Debug.LogWarning("[Cheat] SkillManager 없음 — 인게임 씬에서만 동작");
        }

        if (GUILayout.Button("전 스킬 만렙 (노멀볼 제외)"))
        {
            SkillManager skills = FindAnyObjectByType<SkillManager>();
            if (skills == null) { Debug.LogWarning("[Cheat] SkillManager 없음 — 인게임 씬에서만 동작"); return; }

            // 노멀볼은 무한 성장이라 "만렙" 개념이 없어 제외. 나머지는 Lv3까지 (Acquire가 만석/만렙 방어)
            foreach (SkillId id in System.Enum.GetValues(typeof(SkillId)))
            {
                if (id == SkillId.NormalBall) continue;
                for (int i = 0; i < PlayerSkills.MaxLevel; i++)
                    skills.DebugLevelUp(id);
            }
        }
    }
}
