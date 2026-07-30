using System;
using Newtonsoft.Json;

public abstract class SaveData
{
    // [JsonProperty]: setter가 protected면 Json.NET이 파일의 Version을 복원하지 않아(생성자값 유지)
    // 마이그레이션 판별이 불가능해진다 — 이 표식이 있어야 non-public setter에도 값이 들어간다
    [JsonProperty] public int Version { get; protected set; }
    public abstract SaveData VersionUp();
}

public class SaveDataV1 : SaveData
{
    public PlayerAccountDataSave playerAccountDataSave;
    public StaminaSystemSave staminaSystemSave;
    public SkinUserDataSave skinUserDataSave;

    // [AnimalBreakOut] 게임 전용 시스템
    //public GoldAnimalTokenKeySystemSave goldAnimalTokenKeySystemSave;
    //public PlayerLevelSystemSave playerLevelSystemSave;
    //public StaminaSystemSave staminaSystemSave;
    //public AnimalUserDataListSave animalUserDataTableSave;

    public DateTime saveTime = DateTime.Now;

    public SaveDataV1()
    {
        Version = 1;
    }

    public override SaveData VersionUp()
    {
        // V1이 최신인 동안은 호출될 일 없음 (Load의 while 조건 미충족). 조용히 빈 객체를 돌려주면
        // V2 도입 때 유저 데이터가 통째로 초기화되므로, "이사 코드를 짜기 전엔 지나갈 수 없게" 명시적으로 실패시킨다
        throw new NotImplementedException(
            "SaveDataV2 도입 시 구현: V1 필드를 V2로 이사시켜 반환할 것 (새 빈 객체 반환 금지)");
    }
}
