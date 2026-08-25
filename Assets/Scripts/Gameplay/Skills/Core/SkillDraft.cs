using System;
using System.Collections.Generic;

// 3택지 추출 — 기획서 규칙 (프로젝트에서 유일하게 Random이 허용된 곳: PDF가 "랜덤 추출" 명시):
//   미보유 스킬 → Lv1 카드 / 종류(액티브4·패시브2) 만석 → 그 종류는 보유 스킬만
//   Lv1~2 → 업그레이드 카드 / Lv3(만렙) → 액티브볼은 "+1개" 카드로 남고, 패시브는 볼이 없어 제외
//   노멀볼은 항상 정규 후보(무한 개수/레벨 성장) — 무한모드 후반에도 선택지가 마르지 않게
//   전체 풀에서 랜덤 / 동일 카드 중복 없음
public static class SkillDraft
{
    public const int CardCount = 3;

    public static List<SkillId> Draw(PlayerSkills owned, Random rng)
    {
        var candidates = new List<SkillId> { SkillId.NormalBall };   // 노멀볼 = 항상 정규 후보
        foreach (SkillDef def in owned.Table.Values)
        {
            int lv = owned.GetLevel(def.id);
            if (lv == 0 && owned.IsFull(def.kind)) continue;         // 만석 종류는 신규 금지
            // 만렙: 액티브볼은 "+1개" 후보로 유지, 패시브는 볼이 없으니 제외
            if (lv >= PlayerSkills.MaxLevel && def.kind != SkillKind.ActiveBall) continue;
            candidates.Add(def.id);   // lv0 → Lv1 카드 / lv1~2 → 업그레이드 / lv3 액티브 → +1개
        }

        // 비복원 무작위 추출 (후보가 3장 미만이면 있는 만큼)
        var result = new List<SkillId>();
        int picks = Math.Min(CardCount, candidates.Count);
        for (int i = 0; i < picks; i++)
        {
            int index = rng.Next(candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return result;
    }
}
