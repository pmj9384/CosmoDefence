using System.Collections.Generic;

// 플레이어의 스킬 보유 상태 — 보유/레벨만 안다 (효과 적용은 SkillManager, 수치는 SkillDef).
// 기획서: 액티브 최대 4, 패시브 최대 2, 레벨 1~3.
public class PlayerSkills
{
    public const int MaxActive = 4;
    public const int MaxPassive = 2;
    public const int MaxLevel = 3;

    private readonly IReadOnlyDictionary<SkillId, SkillDef> table;
    private readonly Dictionary<SkillId, int> levels = new();

    public IReadOnlyDictionary<SkillId, SkillDef> Table => table;

    public PlayerSkills(IReadOnlyDictionary<SkillId, SkillDef> table)
    {
        this.table = table;
    }

    public int GetLevel(SkillId id) => levels.TryGetValue(id, out int lv) ? lv : 0;
    public bool Has(SkillId id) => levels.ContainsKey(id);

    public bool IsFull(SkillKind kind)
    {
        int count = 0;
        foreach (SkillId id in levels.Keys)
            if (table[id].kind == kind) count++;
        return count >= (kind == SkillKind.ActiveBall ? MaxActive : MaxPassive);
    }

    // 신규 획득(Lv1) 또는 레벨업. 규칙 위반(만석 신규/만렙 초과)은 false — 호출측(3택지)이 걸러주지만 방어
    public bool Acquire(SkillId id)
    {
        int current = GetLevel(id);
        if (current >= MaxLevel) return false;
        if (current == 0 && IsFull(table[id].kind)) return false;

        levels[id] = current + 1;
        return true;
    }

    // 보유 스킬의 현재 레벨 a값 — 미보유는 0 (효과 없음과 등가라 호출측의 보유 분기가 사라진다).
    // 레벨과 테이블을 둘 다 아는 곳은 여기뿐이라 이 조회의 정위치 (SkillManager에서 이사)
    public float PassiveValue(SkillId id)
    {
        int level = GetLevel(id);
        return level == 0 ? 0f : table[id].GetLevel(level).a;
    }

    public IEnumerable<SkillId> Owned(SkillKind kind)
    {
        foreach (SkillId id in levels.Keys)
            if (table[id].kind == kind) yield return id;
    }
}
