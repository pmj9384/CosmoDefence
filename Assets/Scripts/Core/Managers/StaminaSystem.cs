using System;
using System.Collections;
using UnityEngine;

public class StaminaSystem : ISaveLoad
{
    public DataSourceType SaveDataSouceType => DataSourceType.Local;

    public int CurrentStamina { get; private set; }
    public const int MinStamina = 0;
    public const int MaxStamina = 5;
    public const float RecoveryInterval = 300f; // 5분

    public bool IsStaminaFull => CurrentStamina >= MaxStamina;

    private DateTime lastStaminaRecoverTime;
    private Coroutine coRecovery;

    public event Action<int> onStaminaChanged;

    public StaminaSystem()
    {
        SaveLoadSystem.Instance.RegisterOnSaveAction(this);
    }

    public void SetInitialValue(int stamina, DateTime lastRecoverTime)
    {
        CurrentStamina = stamina;
        lastStaminaRecoverTime = lastRecoverTime;

        if (!IsStaminaFull)
        {
            var passedTime = DateTime.UtcNow - lastRecoverTime;
            int recovered = Mathf.FloorToInt((float)passedTime.TotalSeconds / RecoveryInterval);
            CurrentStamina = Mathf.Clamp(CurrentStamina + recovered, MinStamina, MaxStamina);

            if (!IsStaminaFull)
            {
                float remainder = (float)passedTime.TotalSeconds % RecoveryInterval;
                lastStaminaRecoverTime = DateTime.UtcNow.AddSeconds(-remainder);
            }
        }

        onStaminaChanged += StartRecoveryIfNeeded;
        onStaminaChanged?.Invoke(CurrentStamina);
    }

    public bool TryConsumeStamina()
    {
        if (CurrentStamina <= 0) return false;

        if (IsStaminaFull)
            lastStaminaRecoverTime = DateTime.UtcNow;

        CurrentStamina--;
        onStaminaChanged?.Invoke(CurrentStamina);
        return true;
    }

    public float GetTimeToNextRecovery()
    {
        if (IsStaminaFull) return 0f;
        var passed = DateTime.UtcNow - lastStaminaRecoverTime;
        return Mathf.Max(0f, RecoveryInterval - (float)passed.TotalSeconds);
    }

    public IEnumerator CoRecovery()
    {
        while (!IsStaminaFull)
        {
            yield return new WaitForSecondsRealtime(GetTimeToNextRecovery());
            lastStaminaRecoverTime = DateTime.UtcNow;
            CurrentStamina = Mathf.Clamp(CurrentStamina + 1, MinStamina, MaxStamina);
            onStaminaChanged?.Invoke(CurrentStamina);
        }
        coRecovery = null;
    }

    private void StartRecoveryIfNeeded(int _)
    {
        if (coRecovery == null && !IsStaminaFull)
            coRecovery = GameDataManager.Instance.StartStaminaRecovery();
    }

    public void Save()
    {
        var saveData = SaveLoadSystem.Instance.CurrentSaveData.staminaSystemSave = new();
        saveData.currentStamina = CurrentStamina;
        saveData.lastStaminaRecoverTime = lastStaminaRecoverTime;
    }

    public void Load()
    {
        SetInitialValue(MaxStamina, DateTime.UtcNow);
    }

    public void Load(StaminaSystemSave saveData)
    {
        if (saveData == null) { Load(); return; }
        SetInitialValue(saveData.currentStamina, saveData.lastStaminaRecoverTime);
    }
}
