using System;
using NUnit.Framework;

public class StagePatternTests
{
    [Test]
    public void 기본_파싱과_유닛_수()
    {
        var p = StagePattern.Parse("1.2.1.2.1\n.........\n2121.1212", 9);

        Assert.AreEqual(3, p.Rows.Count);          // 빈 행도 행
        Assert.AreEqual(13, p.TotalUnits);         // 5 + 0 + 8
        Assert.AreEqual(0, p.Rows[0][0].code);     // '1' → 타입 인덱스 0
        Assert.IsFalse(p.Rows[1][0].IsUnit);
    }

    [Test]
    public void 벌워크는_오른쪽_점유칸을_강제()
    {
        var ok = StagePattern.Parse("B-.......", 9);
        Assert.AreEqual(RowCell.BulwarkAnchor, ok.Rows[0][0].code);
        Assert.AreEqual(1, ok.TotalUnits);         // 멀티셀도 1유닛

        Assert.Throws<FormatException>(() => StagePattern.Parse("B1.......", 9));
        Assert.Throws<FormatException>(() => StagePattern.Parse("........B", 9));   // 오른쪽 끝 앵커 불가
    }

    [Test]
    public void 드리프터는_다음_행_같은_칸_점유를_강제()
    {
        var ok = StagePattern.Parse("D........\n-.1......", 9);
        Assert.AreEqual(RowCell.DrifterAnchor, ok.Rows[0][0].code);
        Assert.AreEqual(2, ok.TotalUnits);

        Assert.Throws<FormatException>(() => StagePattern.Parse("D........\n1........", 9));  // 위 칸 침범
        Assert.Throws<FormatException>(() => StagePattern.Parse("D........", 9));             // 위 행 자체가 없음
    }

    [Test]
    public void 고아_점유칸과_길이_불일치는_예외()
    {
        Assert.Throws<FormatException>(() => StagePattern.Parse("-........", 9));
        Assert.Throws<FormatException>(() => StagePattern.Parse("1.1", 9));
    }
}
