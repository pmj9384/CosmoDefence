using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

// 스킨 스프라이트 → Addressables 일괄 등록 (Task D — 유저 결정: 스킨만 전환, 나머지는 Resources 유지).
// 어드레스 규약 = "Skins/{파일명}" (예: Skins/Common_Rookie_head) — 코드의 키 규약과 1:1
public static class SkinAddressablesSetup
{
    private const string SkinFolder = "Assets/Art/Skins";
    private const string GroupName = "Skins";

    // 도메인 리로드 후 1회 자동 실행 — MCP execute_menu_item이 메뉴 등록 타이밍과 경합해서 우회 [임시].
    // delayCall 금지: 에디터가 백그라운드면 다음 틱이 안 와서 영영 안 돈다 (mcp-unity 신버전과 같은 함정)
    [InitializeOnLoadMethod]
    private static void AutoRunOnce()
    {
        if (SessionState.GetBool("SkinAddressablesRegistered", false)) return;
        SessionState.SetBool("SkinAddressablesRegistered", true);
        RegisterSkins();
    }

    [MenuItem("Tools/Skins/Register Addressables")]
    public static void RegisterSkins()
    {
        // 설정 없으면 생성 (Window > Asset Management > Addressables > Groups의 "Create" 버튼과 동일)
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

        var group = settings.FindGroup(GroupName);
        if (group == null)
            group = settings.CreateGroup(GroupName, false, false, true, settings.DefaultGroup.Schemas);

        int count = 0;
        foreach (string file in Directory.GetFiles(SkinFolder, "*.png"))
        {
            string guid = AssetDatabase.AssetPathToGUID(file.Replace("\\", "/"));
            if (string.IsNullOrEmpty(guid)) continue;
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = $"Skins/{Path.GetFileNameWithoutExtension(file)}";
            count++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SkinAddressablesSetup] {count}개 등록 완료 (그룹: {GroupName})");
    }
}
