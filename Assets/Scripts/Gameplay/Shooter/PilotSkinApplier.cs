using UnityEngine;
using UnityEngine.AddressableAssets;

// 장착 스킨을 파일럿 3파츠에 반영 (MonsterVisual 패턴 — 시각은 컴포넌트가, 데이터는 SkinUserData가).
// 스킨 에셋 = Addressables (유저 결정 2026-07-15: 스킨만 전환 — "늘어나면 원격 배포될 카테고리"라 명분 있는
// 영역만. 사운드·테이블 등은 Resources 유지). 어드레스 규약: Skins/{id}_head|_body|_weapon, 아이콘 Skins/{id}.
// 스프라이트 실체는 부팅 로딩 씬에서 미리 캐시에 채워지므로 여기선 캐시 히트로 끝난다 (SkinSprites 참고).
public class PilotSkinApplier : MonoBehaviour
{
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer weaponRenderer;

    private void Start()   // 매니저 Initialize 완료 후 (관례) — GameDataManager는 접근 시 자동 생성·로드
    {
        string id = GameDataManager.Instance.SkinUserData.EquippedSkinId;
        if (string.IsNullOrEmpty(id)) return;

        Apply(headRenderer, $"Skins/{id}_head");
        Apply(bodyRenderer, $"Skins/{id}_body");
        Apply(weaponRenderer, $"Skins/{id}_weapon");
    }

    private static void Apply(SpriteRenderer renderer, string address)
    {
        if (renderer == null) return;
        var sprite = SkinSprites.Load(address);
        if (sprite != null) renderer.sprite = sprite;   // 없으면 기존(기본 파일럿) 유지
    }
}

// 스킨 스프라이트 로딩의 단일 지점 — Resources→Addressables 전환이 이 안에서 끝났듯,
// 원격 카탈로그/비동기로 갈 때도 여기만 바뀐다 (호출부 3곳: 파츠·아이콘·장착 아이콘).
// 캐시를 채우는 시점은 부팅 로딩 씬(SceneLoader.Preload)으로 앞당겼다 — 스킨 창을 처음 열 때
// 아이콘 10장을 동기로 읽어 프레임이 멈추던 것을, 어차피 비어 있던 로딩 1초로 옮긴 것
public static class SkinSprites
{
    // 어드레스→스프라이트 캐시: 같은 어드레스를 화면 열 때마다 재로드하면 Release 없는
    // Addressables 핸들이 무한 누적된다 (검수 v6). 스킨은 로컬 소량이라 앱 수명 동안 상주가 적정 —
    // Release 체계는 스킨이 원격·대량화될 때 도입 (그때도 이 클래스만 바뀐다)
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> cache = new();

    // 부팅 선로드가 캐시를 미리 채우는 통로 — 캐시의 주인이 이 클래스라 Dictionary를 밖으로 열지 않는다
    public static void Prime(string address, Sprite sprite)
    {
        cache[address] = sprite;   // null도 캐시 — Load와 같은 이유(없는 어드레스 반복 재시도 방지)
    }

    // 캐시 미스 시의 동기 로드 — 선로드가 실패했거나 라벨에서 빠진 어드레스를 위한 폴백으로 남는다
    public static Sprite Load(string address)
    {
        if (cache.TryGetValue(address, out Sprite cached)) return cached;

        Sprite sprite;
        try { sprite = Addressables.LoadAssetAsync<Sprite>(address).WaitForCompletion(); }
        catch { sprite = null; }   // 미등록 어드레스 — 호출부가 폴백 처리
        cache[address] = sprite;   // null도 캐시 — 없는 어드레스를 열 때마다 재시도하지 않게
        return sprite;
    }
}
