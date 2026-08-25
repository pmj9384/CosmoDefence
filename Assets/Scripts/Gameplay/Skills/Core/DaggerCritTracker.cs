using System.Collections.Generic;

// DaggerCritTracker — 단검(자수정/에메랄드)의 "몬스터당 치명타 1회" 소모 명단을 소유.
// 몬스터를 InstanceID(int)로 식별해 엔진에 의존하지 않는다(순수 C# → 테스트 대상).
// 명단 관리 한 가지 책임만 진다 — 크리 부여 여부(각도) 판단은 DaggerCritRule이 소유.
public class DaggerCritTracker
{
    private readonly HashSet<int> consumed = new();

    public bool HasConsumed(int monsterId) => consumed.Contains(monsterId);

    public void MarkConsumed(int monsterId) => consumed.Add(monsterId);

    public void Forget(int monsterId) => consumed.Remove(monsterId);

    // 구간 전환(ClearAllMonsters) 시 명단 전체 초기화 — 풀 재사용 몬스터가 이전 소모 상태를 물려받지 않게
    public void Clear() => consumed.Clear();
}
