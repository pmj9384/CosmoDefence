using NUnit.Framework;

// 단검 "몬스터당 치명타 1회" 소모 기록. 몬스터는 InstanceID(int)로 식별 —
// 순수 C#이라 실제 Monster 없이 검증 가능.
public class DaggerCritTrackerTests
{
    [Test]
    public void 처음_보는_몬스터는_소모_안된_상태()
    {
        var t = new DaggerCritTracker();
        Assert.IsFalse(t.HasConsumed(1));
    }

    [Test]
    public void 표시하면_소모됨()
    {
        var t = new DaggerCritTracker();
        t.MarkConsumed(1);
        Assert.IsTrue(t.HasConsumed(1));      // 같은 몬스터 두 번째 크리는 막힘
    }

    [Test]
    public void 잊으면_다시_소모_안된_상태()
    {
        var t = new DaggerCritTracker();
        t.MarkConsumed(1);
        t.Forget(1);                          // 사망·도달 시 풀 재사용 대비
        Assert.IsFalse(t.HasConsumed(1));
    }

    [Test]
    public void 전체_비우면_모두_초기화()      // ← 이번 버그: 구간 전환 시 명단이 안 비워졌음
    {
        var t = new DaggerCritTracker();
        t.MarkConsumed(1);
        t.MarkConsumed(2);
        t.Clear();
        Assert.IsFalse(t.HasConsumed(1));
        Assert.IsFalse(t.HasConsumed(2));
    }

    [Test]
    public void 서로_다른_몬스터는_독립()
    {
        var t = new DaggerCritTracker();
        t.MarkConsumed(1);
        Assert.IsTrue(t.HasConsumed(1));
        Assert.IsFalse(t.HasConsumed(2));     // 1을 표시해도 2는 영향 없음
    }
}
