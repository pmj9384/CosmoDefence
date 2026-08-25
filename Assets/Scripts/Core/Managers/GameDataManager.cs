using UnityCommunity.UnitySingleton;
using UnityEngine;

public class GameDataManager : PersistentMonoSingleton<GameDataManager>
{
    public PlayerAccountData PlayerAccountData { get; private set; }
    public StaminaSystem StaminaSystem { get; private set; }
    public SkinUserData SkinUserData { get; private set; }

    public override void InitializeSingleton()
    {
        base.InitializeSingleton();
        SaveLoadSystem.Instance.Load();

        PlayerAccountData = new();
        PlayerAccountData.Load(SaveLoadSystem.Instance.CurrentSaveData.playerAccountDataSave);

        StaminaSystem = new();
        StaminaSystem.Load(SaveLoadSystem.Instance.CurrentSaveData.staminaSystemSave);

        SkinUserData = new();
        SkinUserData.Load(SaveLoadSystem.Instance.CurrentSaveData.skinUserDataSave);

        // 프레임 상한은 씬이 아니라 앱 단위 설정이라 부팅에서 한 번만 걸면 된다.
        // 여기가 세이브를 읽은 직후이자 어느 씬에서 시작하든 지나가는 유일한 지점
        FrameRateSetting.Restore(PlayerAccountData.FrameRateFps);
    }

    public Coroutine StartStaminaRecovery()
    {
        return StartCoroutine(StaminaSystem.CoRecovery());
    }
}
