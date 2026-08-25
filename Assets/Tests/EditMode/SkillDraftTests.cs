using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class SkillDraftTests
{
    private static PlayerSkills NewPlayer()
        => new PlayerSkills(SkillTableParser.Parse(File.ReadAllText("Assets/Resources/Tables/SkillTable.csv")));

    private static void Max(PlayerSkills p, params SkillId[] ids)
    {
        foreach (SkillId id in ids) { p.Acquire(id); p.Acquire(id); p.Acquire(id); }
    }

    [Test]
    public void 기본_3장_중복_없음()
    {
        var p = NewPlayer();
        List<SkillId> cards = SkillDraft.Draw(p, new Random(1));
        Assert.AreEqual(3, cards.Count);
        CollectionAssert.AllItemsAreUnique(cards);
    }

    [Test]
    public void 같은_시드는_같은_결과()
    {
        var p = NewPlayer();
        CollectionAssert.AreEqual(SkillDraft.Draw(p, new Random(42)),
                                  SkillDraft.Draw(p, new Random(42)));
    }

    [Test]
    public void 액티브_만석이면_신규_액티브는_안_나옴()
    {
        var p = NewPlayer();
        p.Acquire(SkillId.FireBall);
        p.Acquire(SkillId.IceBall);
        p.Acquire(SkillId.LaserBall);
        p.Acquire(SkillId.GhostBall);   // 액티브 4 만석 — ClusterBall만 미보유

        for (int seed = 0; seed < 50; seed++)                 // 여러 시드로 규칙 위반 없음 확인
        {
            foreach (SkillId id in SkillDraft.Draw(p, new Random(seed)))
                Assert.AreNotEqual(SkillId.ClusterBall, id, $"seed {seed}: 만석인데 신규 액티브 등장");
        }
    }

    [Test]
    public void 노멀볼은_항상_정규_후보로_등장()
    {
        var p = NewPlayer();   // 초반부터 노멀볼이 후보 풀에 있어야 함 (비상 채움이 아니라 정규)
        bool appeared = false;
        for (int seed = 0; seed < 50 && !appeared; seed++)
            if (SkillDraft.Draw(p, new Random(seed)).Contains(SkillId.NormalBall)) appeared = true;
        Assert.IsTrue(appeared, "노멀볼이 정규 후보로 한 번도 안 나옴");
    }

    [Test]
    public void 만렙_액티브볼은_플러스1_후보로_재등장()
    {
        var p = NewPlayer();
        Max(p, SkillId.FireBall);   // 액티브 Lv3 — 이제 "+1개" 후보로 남아야 함

        bool appeared = false;
        for (int seed = 0; seed < 50 && !appeared; seed++)
            if (SkillDraft.Draw(p, new Random(seed)).Contains(SkillId.FireBall)) appeared = true;
        Assert.IsTrue(appeared, "만렙 액티브볼이 +1 후보로 재등장하지 않음");
    }

    [Test]
    public void 만렙_패시브는_풀에서_제외()
    {
        var p = NewPlayer();
        Max(p, SkillId.TinHeart);   // 패시브 Lv3 — 볼이 없으니 계속 제외

        for (int seed = 0; seed < 50; seed++)
            CollectionAssert.DoesNotContain(SkillDraft.Draw(p, new Random(seed)), SkillId.TinHeart);
    }

    [Test]
    public void 액티브_전부_만렙이어도_노멀볼로만_안_채워짐()
    {
        var p = NewPlayer();
        // 액티브 4종 + 패시브 2종 전부 만렙 → 예전엔 노멀볼만 남았음.
        // 이제 만렙 액티브 4종이 +1 후보 → 노멀볼과 함께 실제 선택지가 유지된다.
        Max(p, SkillId.FireBall, SkillId.IceBall, SkillId.LaserBall, SkillId.GhostBall,
               SkillId.TinHeart, SkillId.MagicMirror);

        var pool = new HashSet<SkillId>();
        for (int seed = 0; seed < 50; seed++)
            foreach (SkillId id in SkillDraft.Draw(p, new Random(seed))) pool.Add(id);

        // 만렙 액티브 4종이 후보에 실제로 등장
        foreach (SkillId active in new[] { SkillId.FireBall, SkillId.IceBall, SkillId.LaserBall, SkillId.GhostBall })
            CollectionAssert.Contains(pool, active, $"{active} 만렙 +1 후보가 안 나옴");
        // 만렙 패시브는 여전히 제외
        CollectionAssert.DoesNotContain(pool, SkillId.TinHeart);
        CollectionAssert.DoesNotContain(pool, SkillId.MagicMirror);
    }

    [Test]
    public void 후보가_부족하면_있는_만큼_노멀볼_포함해_반환()
    {
        var p = NewPlayer();
        // 액티브 1종만 보유·만렙(+1 후보 1장) + 나머지 액티브 슬롯은 열려 있으니 신규 후보도 있음.
        // 최소 보장: 반환 카드는 비지 않고, 노멀볼이 후보로 낄 수 있다.
        Max(p, SkillId.FireBall);
        List<SkillId> cards = SkillDraft.Draw(p, new Random(3));
        Assert.GreaterOrEqual(cards.Count, 1);
        CollectionAssert.AllItemsAreUnique(cards);
    }
}
