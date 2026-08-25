// 이번 런 점수 누적 (순수 C#) — 가중 점수라 "처치 수"가 아니라 합산(잡몹 10 / 보스 100).
// 최고기록 영속(세이브 파일, PlayerAccountData)은 소유자(StatsManager) 몫 — 이 클래스는 현재 런만 안다(테스트 가능).
public class ScoreCounter
{
    public int Current { get; private set; }

    public void Add(int points) => Current += points;
}
