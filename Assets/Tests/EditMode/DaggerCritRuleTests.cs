using NUnit.Framework;

// 단검 전면/후면 치명타 규칙 — 충돌 노멀 방향 판정과 "몬스터당 1회" 소모의 결합 거동.
// 명단 자체의 거동은 DaggerCritTrackerTests가 담당 — 여기는 각도 판정과 소모 조건만.
public class DaggerCritRuleTests
{
    [Test]
    public void 아래에서_타격은_자수정_전면_보너스()
    {
        var rule = new DaggerCritRule();
        Assert.AreEqual(0.2f, rule.CritChance(1, -0.9f, amethystBonus: 0.2f, emeraldBonus: 0.5f));
    }

    [Test]
    public void 위에서_타격은_에메랄드_후면_보너스()
    {
        var rule = new DaggerCritRule();
        Assert.AreEqual(0.5f, rule.CritChance(1, 0.9f, amethystBonus: 0.2f, emeraldBonus: 0.5f));
    }

    [Test]
    public void 측면_타격은_보너스_없고_소모도_없다()
    {
        var rule = new DaggerCritRule();
        Assert.AreEqual(0f, rule.CritChance(1, 0.1f, 0.2f, 0.5f));      // 임계값(0.3) 안쪽 = 측면
        Assert.AreEqual(0.5f, rule.CritChance(1, 0.9f, 0.2f, 0.5f));    // 소모 안 됐으니 다음 유효타에 발동
    }

    [Test]
    public void 소모_후_재타격은_0()
    {
        var rule = new DaggerCritRule();
        rule.CritChance(1, -0.9f, 0.2f, 0.5f);
        Assert.AreEqual(0f, rule.CritChance(1, -0.9f, 0.2f, 0.5f));
    }

    [Test]
    public void 미보유_보너스0은_소모하지_않는다()
    {
        var rule = new DaggerCritRule();
        Assert.AreEqual(0f, rule.CritChance(1, -0.9f, amethystBonus: 0f, emeraldBonus: 0f));   // 획득 전 타격
        Assert.AreEqual(0.2f, rule.CritChance(1, -0.9f, amethystBonus: 0.2f, emeraldBonus: 0f)); // 획득 후 발동돼야 함
    }

    [Test]
    public void 잊으면_다시_발동()
    {
        var rule = new DaggerCritRule();
        rule.CritChance(1, -0.9f, 0.2f, 0.5f);
        rule.Forget(1);
        Assert.AreEqual(0.2f, rule.CritChance(1, -0.9f, 0.2f, 0.5f));
    }
}
