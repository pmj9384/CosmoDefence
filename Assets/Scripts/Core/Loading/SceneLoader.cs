using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

// 부팅 로딩 씬 진행자 — Addressables 라벨의 스프라이트를 실제로 로드해 캐시에 채운 뒤 다음 씬으로 넘어간다 (빌드 0번 씬 전용).
// AnimalBreakOut LoadingScene 검수 후 재설계 (2026-08-03): 가짜 진행바 → PercentComplete 실배선 /
// 씬 이름 문자열 계약 → SerializeField / 인게임 위 additive 커버 → 부팅 진입 씬 /
// 실패 시 영구 감금(while 대기) → 로그 남기고 다음 씬 진입 (세이브 폴백과 같은 사상: 부팅은 보장한다)
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "LobbyScene";
    [SerializeField] private string preloadLabel = "Skins";   // Addressables 라벨 — 비우면 선로드 생략(사용 시점 동기 로드로 폴백)
    [SerializeField] private float minShowSeconds = 1f;       // 로컬 로드는 순식간 — 스플래시가 깜빡 사라지는 것 방지
    [SerializeField] private LoadingSceneUI ui;

    private IEnumerator Start()
    {
        float shownAt = Time.realtimeSinceStartup;   // timeScale 무관 기준

        if (!string.IsNullOrEmpty(preloadLabel))
            yield return Preload();

        // 최소 노출 시간 채우기 — 남은 시간 동안 바를 100%까지 마저 채우는 연출
        float remain;
        while ((remain = minShowSeconds - (Time.realtimeSinceStartup - shownAt)) > 0f)
        {
            if (ui != null) ui.UpdateProgress(1f - remain / minShowSeconds);
            yield return null;
        }
        if (ui != null) ui.UpdateProgress(1f);

        yield return SceneManager.LoadSceneAsync(nextSceneName);   // 공식 표준 관용구 — 로드 중에도 로딩 화면이 살아 있게
    }

    private IEnumerator Preload()
    {
        // 라벨 실존을 조용히 먼저 조회 — 없는 키에 바로 로드를 걸면 Addressables가 콘솔에
        // 빨간 InvalidKeyException을 뿌린다 (새 프로젝트에 라벨이 아직 없을 때의 템플릿 방어이기도)
        var probe = Addressables.LoadResourceLocationsAsync(preloadLabel);
        yield return probe;
        IList<IResourceLocation> locations = probe.Status == AsyncOperationStatus.Succeeded ? probe.Result : null;
        if (locations == null || locations.Count == 0)
        {
            Addressables.Release(probe);
            Debug.LogWarning($"[SceneLoader] 프리로드 라벨 '{preloadLabel}' 대상 없음 — 건너뛰고 진입");
            yield break;   // 프리로드는 최적화일 뿐 — 실패가 부팅을 막으면 안 된다
        }

        // 스프라이트 로케이션만 남긴다 — 같은 어드레스에 Texture2D 로케이션이 함께 잡히면
        // LoadAssetsAsync<Sprite>가 거기서 실패하고, 하나라도 실패하면 배치 전체가 무효가 된다
        var spriteLocations = new List<IResourceLocation>();
        foreach (IResourceLocation loc in locations)
            if (typeof(Sprite).IsAssignableFrom(loc.ResourceType)) spriteLocations.Add(loc);
        if (spriteLocations.Count == 0)
        {
            Addressables.Release(probe);
            Debug.LogWarning($"[SceneLoader] 라벨 '{preloadLabel}'에 스프라이트 없음 — 사용 시점 로드로 진행");
            yield break;
        }

        // 조회한 위치 목록을 그대로 재사용해 스프라이트 실체까지 로드한다. 라벨 문자열로 로드하면
        // 완료 콜백이 스프라이트만 주고 어느 어드레스인지는 알려주지 않아 캐시에 넣을 수 없다 —
        // location.PrimaryKey가 곧 어드레스라, 위치 기반으로 로드해야 짝을 지을 수 있다
        AsyncOperationHandle<IList<Sprite>> handle = Addressables.LoadAssetsAsync<Sprite>(spriteLocations, callback: null);

        while (!handle.IsDone)
        {
            if (ui != null) ui.UpdateProgress(handle.PercentComplete * 0.9f);   // 마지막 10%는 최소 노출 연출 몫
            yield return null;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 결과 IList는 넘긴 locations와 같은 크기·같은 순서로 대응한다(Addressables 공식 문서) —
            // 콜백은 완료 순서라 어드레스를 못 붙이므로, 완료 후 인덱스로 짝지어 캐시에 주입한다
            for (int i = 0; i < spriteLocations.Count; i++)
                SkinSprites.Prime(spriteLocations[i].PrimaryKey, handle.Result[i]);
        }
        else
        {
            // 하나라도 실패하면 전체 실패(공식 문서) — 캐시가 비면 SkinSprites.Load의 동기 로드가 대신 받는다
            Debug.LogWarning($"[SceneLoader] 프리로드 실패({preloadLabel}) — 개별 로드는 사용 시점 SkinSprites가 담당");
        }

        Addressables.Release(probe);   // 위치 목록은 짝짓기까지만 필요 — 아래 스프라이트 핸들과는 별개
        // handle은 일부러 반납하지 않는다: 반납하면 방금 캐시에 넣은 스프라이트가 파괴돼 SkinSprites가
        // 죽은 참조를 들고 있게 된다. 스킨은 로컬 40장이라 앱 수명 상주가 적정 —
        // 예전 DownloadDependenciesAsync는 '번들 예열'이 목적이라 즉시 Release가 맞았지만, 목적이 바뀌었다
    }
}
