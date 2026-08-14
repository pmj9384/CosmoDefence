using UnityEngine;

// 프레임 상한 설정 — 상한을 실제로 적용하고, 유저가 바꾼 값을 PlayerAccountData에 기록한다.
// SoundManager가 볼륨에 대해 하는 역할을 프레임에 대해 한다 (값의 주인은 PlayerAccountData).
// 부팅 복원은 GameDataManager가, 노출은 OutGameSettingsPanel이 맡는다.
public static class FrameRateSetting
{
    public const int Default = 60;

    // 고를 수 있는 값의 SSOT. 설정 패널이 버튼 라벨을 여기서 찍으므로 화면에 숫자를 하드코딩하지 않는다
    public static readonly int[] Options = { 60, 120 };

    public static int Current { get; private set; } = Default;

    // 세이브 파일이 손상됐거나, 이 필드가 없던 옛 세이브라 0으로 읽힌 경우를 기본값으로 흡수한다.
    // 인덱스가 아니라 fps 값을 저장하므로 나중에 목록이 바뀌어도 마이그레이션이 필요 없다
    public static int Sanitize(int fps)
        => System.Array.IndexOf(Options, fps) >= 0 ? fps : Default;

    // 세이브에서 복원할 때. 방금 읽어온 값이라 되쓰지 않는다
    // (부팅 중 GameDataManager 초기화 안에서 불리므로 그쪽을 다시 건드리면 재진입이 된다)
    public static void Restore(int fps) => ApplyOnly(Sanitize(fps));

    // 유저가 설정창에서 바꿀 때. 적용까지 하고 값의 주인에게 넘긴다 — 영속은 세이브 체인 몫
    public static void Set(int fps)
    {
        ApplyOnly(Sanitize(fps));
        GameDataManager.Instance.PlayerAccountData.FrameRateFps = Current;
    }

    private static void ApplyOnly(int fps)
    {
        Current = fps;
        // vSyncCount가 0이 아니면 targetFrameRate는 통째로 무시된다(공식 문서).
        // 모바일은 vSyncCount 자체를 무시해 실기기엔 영향이 없지만, 에디터·데스크톱 빌드에서
        // 설정이 실제로 먹으려면 꺼줘야 한다
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Current;
    }
}
