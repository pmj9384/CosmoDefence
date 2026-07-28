using UnityEngine;
using UnityEngine.UI;

// 스킨 아이콘을 Image에 그리는 단일 지점 — 로딩(SkinSprites)과 UI 표현(빈 아트 처리)의 경계.
// 아이콘을 쓰는 3곳(스킨 창·뽑기 결과 다중·뽑기 결과 단일)이 전부 여기를 거친다.
public static class SkinIconView
{
    // 아트가 아직 없는 스킨용. 아웃게임 패널 배경(명도 0.08~0.12)보다 한 단계 밝아 "구멍"이 아니라 빈 슬롯으로 읽힌다.
    private static readonly Color MissingArtColor = new Color32(0x2A, 0x2E, 0x38, 0xFF);

    // sprite가 null이면 Image가 유니티 기본 흰색으로 렌더돼 창이 통째로 깨져 보인다 — 그때만 다크 색을 깐다.
    public static void Apply(Image target, string skinId)
    {
        var sprite = SkinSprites.Load($"Skins/{skinId}");
        target.sprite = sprite;
        target.color = sprite != null ? Color.white : MissingArtColor;
    }
}
