// DaggerCritRule — 단검(자수정=전면/에메랄드=후면) 치명타 규칙을 소유.
// 충돌 노멀 y로 타격면을 판정하고, 소모 명단(DaggerCritTracker)을 물어 몬스터당 1회만 부여한다.
// 순수 C#: 보너스 수치는 밖에서 값으로 받는다 — PlayerSkills를 몰라야 테스트가 가볍다.
public class DaggerCritRule
{
    // 측면 타격 제외 임계값 — |y|가 이보다 커야 전/후면으로 인정 (원작 [가정3])
    public const float FaceThreshold = 0.3f;

    private readonly DaggerCritTracker tracker = new();

    // 노멀은 표면→볼 방향: y<-임계 = 볼이 아래에서 타격(전면=자수정) / y>임계 = 위에서(후면=에메랄드)
    public float CritChance(int monsterId, float hitNormalY, float amethystBonus, float emeraldBonus)
    {
        if (tracker.HasConsumed(monsterId)) return 0f;

        float bonus = 0f;
        if (hitNormalY < -FaceThreshold) bonus = amethystBonus;
        else if (hitNormalY > FaceThreshold) bonus = emeraldBonus;

        if (bonus > 0f) tracker.MarkConsumed(monsterId);   // 보너스 0(미보유)은 소모하지 않는다 — 획득 후 첫 유효타에 발동해야 하므로
        return bonus;
    }

    public void Forget(int monsterId) => tracker.Forget(monsterId);
    public void Clear() => tracker.Clear();
}
