using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 3택지 카드 UI — 카드 내용 채우기/선택 전달만. 배치는 씬 소관 (에디터-네이티브).
// 아이콘·설명은 SkillTable CSV(icon/description 컬럼)가 SSOT.
// UIElement 미편입 의도: Show(cards, owned, cb)가 데이터 주도형이라 무인자 Show() 계약과 불일치.
public class SkillSelectionPanel : MonoBehaviour
{
    [Header("씬 참조 (빌더 조립)")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text levelBadge;        // 꽉 찬 바 오른쪽 레벨 숫자 (원작 #67)
    [SerializeField] private Image[] activeSlots;        // 보유 액티브 4칸
    [SerializeField] private Image[] passiveSlots;       // 보유 패시브 2칸
    [SerializeField] private Button[] buttons;
    [SerializeField] private Image[] icons;
    [SerializeField] private TMP_Text[] names;
    [SerializeField] private TMP_Text[] descriptions;
    [SerializeField] private TMP_Text[] damages;          // ★N 볼 데미지 (원작 #73)
    [SerializeField] private GameObject[] damageBadges;   // 패시브 카드에선 숨김
    [SerializeField] private Image[] diamonds;            // 카드당 3개 flat (i*3+k) — 레벨 표시
    [SerializeField] private Image[] cardInners;          // 투톤 안판 (테두리색은 버튼 이미지)
    [SerializeField] private SkillIconTable iconTable;     // 스킬↔아이콘 (Resources.Load 대체)

    // 카드 투톤 — 바깥 테두리(진함) + 안판(밝음), 액티브 팥 / 패시브 초록 (유저 확정)
    private static readonly Color ActiveEdge = new(0.22f, 0.11f, 0.11f, 0.98f);
    private static readonly Color ActiveInner = new(0.38f, 0.22f, 0.21f);
    private static readonly Color PassiveEdge = new(0.07f, 0.2f, 0.12f, 0.98f);
    private static readonly Color PassiveInner = new(0.16f, 0.36f, 0.25f);
    private static readonly Color DiaOn = new(0.95f, 0.75f, 0.2f);    // 레벨 다이아 켜짐
    private static readonly Color DiaPreview = new(0.5f, 0.38f, 0.1f); // 미보유(0렙) — "고르면 켜질 자리" 어두운 노랑 (유저 확정)
    private static readonly Color DiaOff = new(0.12f, 0.08f, 0.08f);

    private Action<SkillId> onPicked;
    private List<SkillId> currentCards;

    private void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;   // 클로저 캡처용
            buttons[i].onClick.AddListener(() => onPicked?.Invoke(currentCards[index]));
        }
    }

    public void Show(List<SkillId> cards, PlayerSkills owned, int playerLevel, Action<SkillId> onPicked)
    {
        this.onPicked = onPicked;
        currentCards = cards;

        levelBadge.text = playerLevel.ToString();
        FillSlots(activeSlots, owned, SkillKind.ActiveBall);
        FillSlots(passiveSlots, owned, SkillKind.Passive);

        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = i < cards.Count;
            buttons[i].gameObject.SetActive(active);
            if (!active) continue;

            if (cards[i] == SkillId.NormalBall)   // 채움 카드 — 테이블에 없음, 고정 표기
            {
                buttons[i].image.color = ActiveEdge;
                cardInners[i].color = ActiveInner;
                icons[i].sprite = iconTable.Get(SkillId.NormalBall);
                names[i].text = "노멀 볼";
                descriptions[i].text = "기본 볼이 1개 늘어나 연달아 발사됩니다.";
                damageBadges[i].SetActive(true);
                damages[i].text = "볼 +1";
                SetDiamonds(i, 0);
                continue;
            }

            SkillDef def = owned.Table[cards[i]];
            int currentLv = owned.GetLevel(cards[i]);
            bool isActiveKind = def.kind == SkillKind.ActiveBall;
            bool isPlusOne = isActiveKind && currentLv >= PlayerSkills.MaxLevel;   // 만렙 액티브 = "+1개" 카드 (레벨업 아님)
            int showLevel = isPlusOne ? PlayerSkills.MaxLevel : currentLv + 1;     // GetLevel 범위(1~3) 안으로 캡 — 만렙 카드가 4를 참조하지 않게

            buttons[i].image.color = isActiveKind ? ActiveEdge : PassiveEdge;
            cardInners[i].color = isActiveKind ? ActiveInner : PassiveInner;

            icons[i].sprite = iconTable.Get(cards[i]);
            names[i].text = def.displayName;
            descriptions[i].text = isPlusOne ? $"{def.displayName}이 1개 늘어나 연달아 발사됩니다." : def.description;

            // 원작 #73: 액티브는 ★볼데미지, 레벨은 하단 다이아 (선택 시 도달할 레벨만큼 점등). 만렙 카드는 "+1" 표기
            damageBadges[i].SetActive(isActiveKind);
            if (isActiveKind) damages[i].text = isPlusOne ? $"★ {def.GetLevel(showLevel).ballDamage}  볼 +1" : $"★ {def.GetLevel(showLevel).ballDamage}";
            SetDiamonds(i, currentLv);   // 만렙이면 3개 모두 점등(레벨 고정), 4개째 없음
        }

        overlay.SetActive(true);
    }

    // 다이아 = "현재 보유 레벨" (유저 확정): 1렙=1개, 2렙=2개, 3렙=3개 밝은 노랑.
    // 미보유(0렙)만 첫 칸 어두운 노랑 — 고르면 그 자리가 밝게 켜지는 프리뷰
    private void SetDiamonds(int card, int currentLevel)
    {
        for (int k = 0; k < 3; k++)
        {
            Color c = DiaOff;
            if (currentLevel == 0 && k == 0) c = DiaPreview;
            else if (k < currentLevel) c = DiaOn;
            diamonds[card * 3 + k].color = c;
        }
    }

    // 보유 스킬 슬롯 채우기 (퍼즈 패널과 동일 문법) — 빈 칸은 아이콘 숨김
    private void FillSlots(Image[] slots, PlayerSkills owned, SkillKind kind)
    {
        int i = 0;
        foreach (SkillId id in owned.Owned(kind))
        {
            if (i >= slots.Length) break;
            slots[i].enabled = true;
            slots[i].sprite = iconTable.Get(id);
            i++;
        }
        for (; i < slots.Length; i++)
            slots[i].enabled = false;
    }

    public void Hide() => overlay.SetActive(false);
}
