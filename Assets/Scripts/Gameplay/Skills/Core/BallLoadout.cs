// 발사할 볼의 "타입 파라미터 묶음" — Ball은 매니저를 모르므로 발사 시 이 값만 주입받는다.
public struct BallLoadout
{
    public const int NormalBallDamage = 8;   // 기획서 기본값

    public SkillId? skill;      // null = 노멀볼
    public int level;           // 1~3 (노멀볼은 0)
    public int damage;          // 볼 기본 데미지 (SkillTable의 ballDamage 또는 노멀 8)


    public static BallLoadout Normal => new BallLoadout
    { skill = null, level = 0, damage = NormalBallDamage };
}
