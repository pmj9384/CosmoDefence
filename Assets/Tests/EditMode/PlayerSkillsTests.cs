using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class PlayerSkillsTests
{
    private static PlayerSkills NewPlayer()
        => new PlayerSkills(SkillTableParser.Parse(File.ReadAllText("Assets/Resources/Tables/SkillTable.csv")));

    [Test]
    public void 신규_획득은_Lv1_재획득은_레벨업()
    {
        var p = NewPlayer();
        Assert.AreEqual(0, p.GetLevel(SkillId.FireBall));   // 미보유 = 0

        Assert.IsTrue(p.Acquire(SkillId.FireBall));
        Assert.AreEqual(1, p.GetLevel(SkillId.FireBall));

        Assert.IsTrue(p.Acquire(SkillId.FireBall));
        Assert.IsTrue(p.Acquire(SkillId.FireBall));
        Assert.AreEqual(3, p.GetLevel(SkillId.FireBall));
    }

    [Test]
    public void 만렙_초과_획득_거부()
    {
        var p = NewPlayer();
        p.Acquire(SkillId.FireBall); p.Acquire(SkillId.FireBall); p.Acquire(SkillId.FireBall);
        Assert.IsFalse(p.Acquire(SkillId.FireBall));         // Lv3 상한
        Assert.AreEqual(3, p.GetLevel(SkillId.FireBall));
    }

    [Test]
    public void 액티브_4개_만석이면_5번째_신규_거부()
    {
        var p = NewPlayer();
        p.Acquire(SkillId.FireBall);
        p.Acquire(SkillId.IceBall);
        p.Acquire(SkillId.LaserBall);
        p.Acquire(SkillId.GhostBall);
        Assert.IsTrue(p.IsFull(SkillKind.ActiveBall));

        Assert.IsFalse(p.Acquire(SkillId.ClusterBall));      // 신규 거부
        Assert.IsTrue(p.Acquire(SkillId.FireBall));          // 보유 레벨업은 허용
    }

    [Test]
    public void 패시브_2개_만석_경계()
    {
        var p = NewPlayer();
        p.Acquire(SkillId.TinHeart);
        Assert.IsFalse(p.IsFull(SkillKind.Passive));         // 1개 — 아직
        p.Acquire(SkillId.MagicMirror);
        Assert.IsTrue(p.IsFull(SkillKind.Passive));          // 2개 — 만석
        Assert.IsFalse(p.Acquire(SkillId.LastMatch));
    }

    [Test]
    public void 미보유_스킬의_PassiveValue는_0()
    {
        var p = NewPlayer();
        Assert.AreEqual(0f, p.PassiveValue(SkillId.TinHeart));
    }

    [Test]
    public void PassiveValue는_현재_레벨의_수치를_반환()   // Lv2까지 올려서 검증 — Lv1 값 고정 반환 실수를 잡는다
    {
        var p = NewPlayer();
        p.Acquire(SkillId.TinHeart);
        p.Acquire(SkillId.TinHeart);
        Assert.AreEqual(p.Table[SkillId.TinHeart].GetLevel(2).a, p.PassiveValue(SkillId.TinHeart));
    }

    [Test]
    public void Owned는_종류별로만_반환()
    {
        var p = NewPlayer();
        p.Acquire(SkillId.FireBall);
        p.Acquire(SkillId.TinHeart);

        var actives = new List<SkillId>(p.Owned(SkillKind.ActiveBall));
        CollectionAssert.AreEquivalent(new[] { SkillId.FireBall }, actives);
    }
}
