using UnityEngine;

// 몬스터 종류 정의 (Inspector 데이터) — 스프라이트/HP 배수/점유 크기.
// 멀티셀(드리프터 1×2, 벌워크 2×1)은 블록 스프라이트 자체가 그 크기로 제작돼 있어(Block_1x2/2x1)
// 루트 균등 스케일 그대로 두고 콜라이더만 점유 칸에 맞춘다.
[System.Serializable]
public class MonsterTypeData
{
    public string typeName;
    public Sprite bodySprite;
    public Sprite blockSprite;
    public float hpMultiplier = 1f;   // 행 기본 HP에 곱해짐 (종류별 차등용)
    public int width = 1;             // 점유 칸 (기존 씬 데이터엔 키가 없어도 초기값 1로 역직렬화됨)
    public int height = 1;
}
