using System;
using UnityEngine;

public class PlayerAccountData : ISaveLoad
{
    public DataSourceType SaveDataSouceType => DataSourceType.Local;

    public int BestScore { get; private set; }

    public bool TryUpdateBestScore(int score)
    {
        if (score <= BestScore) return false;
        BestScore = score;
        return true;
    }

    private float bgmVolume;
    public float BgmVolume
    {
        get => bgmVolume;
        set { bgmVolume = Mathf.Clamp(value, 0.0001f, 1f); }
    }

    private float sfxVolume;
    public float SfxVolume
    {
        get => sfxVolume;
        set { sfxVolume = Mathf.Clamp(value, 0.0001f, 1f); }
    }

    // 프레임 상한은 원래 기기 설정이라 계정 데이터와 분리하는 게 맞지만, 이 프로젝트는
    // SaveDataSouceType이 전부 Local이고 계정 전환도 없어 실질 차이가 없다. 세이브 체인을 하나로 유지한다.
    // PlayerAccountData가 Firebase로 바뀌는 시점에 기기 설정만 따로 떼어낼 것
    private int frameRateFps = FrameRateSetting.Default;
    public int FrameRateFps
    {
        get => frameRateFps;
        set { frameRateFps = FrameRateSetting.Sanitize(value); }
    }

    public event Action<int> OnCoinsChanged;

    private int coins;
    public int Coins
    {
        get => coins;
        private set { coins = value; OnCoinsChanged?.Invoke(coins); }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
    }
    public bool SpendCoin(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }
        else if (Coins < amount)
        {
            return false;
        }
        else
        {
            Coins -= amount;
            return true;
        }
    }
    public PlayerAccountData()
    {
        SaveLoadSystem.Instance.RegisterOnSaveAction(this);
    }

    public void Save()
    {
        var saveData = SaveLoadSystem.Instance.CurrentSaveData.playerAccountDataSave = new();
        saveData.bgmVolume = BgmVolume;
        saveData.sfxVolume = SfxVolume;
        saveData.bestScore = BestScore;
        saveData.coins = Coins;
        saveData.frameRateFps = FrameRateFps;
    }

    public void Load()
    {
        BgmVolume = 1f;
        SfxVolume = 1f;
        BestScore = 0;
        Coins = 0;
        FrameRateFps = FrameRateSetting.Default;
    }

    public void Load(PlayerAccountDataSave saveData)
    {
        if (saveData == null) { Load(); return; }
        BgmVolume = saveData.bgmVolume;
        SfxVolume = saveData.sfxVolume;
        BestScore = saveData.bestScore;
        Coins = saveData.coins;
        FrameRateFps = saveData.frameRateFps;
    }

}
