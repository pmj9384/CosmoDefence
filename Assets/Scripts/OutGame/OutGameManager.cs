using System.Collections.Generic;
using UnityEngine;

public class OutGameManager : MonoBehaviour
{
    private List<IManager> managers = new();

    public OutGameUIManager UIManager { get; private set; }

    private void Start()
    {
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // 태그 미지정/오브젝트 부재 시 원인 불명 NRE 대신 명시 에러 (검수 v6)
        var uiManagerGo = GameObject.FindGameObjectWithTag("UIManager");
        if (uiManagerGo == null || !uiManagerGo.TryGetComponent(out OutGameUIManager uiManager))
        {
            Debug.LogError("[OutGameManager] 'UIManager' 태그의 OutGameUIManager를 찾지 못함 — 씬 구성 확인");
            return;
        }
        UIManager = uiManager;
        managers.Add(UIManager);

        foreach (var manager in managers)
            manager.Initialize();

        UIManager.OpenScreen<LobbyScreen>();

        SoundManager.Instance.PlayBgm(BgmClipId.Title);   // 씬 단위 BGM — 인게임의 GameManager와 같은 관례
    }

    private void OnDestroy()
    {
        foreach (var manager in managers)
            manager.Clear();
    }
}
