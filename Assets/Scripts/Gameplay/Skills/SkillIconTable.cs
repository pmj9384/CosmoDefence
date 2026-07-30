using System;
using UnityEngine;

// 스킬 아이콘 매핑 (SkillId→Sprite). 인스펙터 GUID 연결로 "문자열 경로→Resources.Load"의
// 조용한 null을 없앤다 (Resources.Load 6곳 대체, 2026-07-30). 11개 고정이라 딕셔너리 없이
// 배열 선형 탐색 — 조회는 패널 열 때·발사 때만이라 비용 무의미.
[CreateAssetMenu(fileName = "SkillIconTable", menuName = "TongTong/Skill Icon Table")]
public class SkillIconTable : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public SkillId id;
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries;

    public Sprite Get(SkillId id) => Find(entries, id);

    // 못 찾으면 null + 경고 — 조용한 실패를 로그로 드러내는 게 이 표의 존재 이유
    public static Sprite Find(Entry[] entries, SkillId id)
    {
        foreach (Entry e in entries)
            if (e.id == id) return e.sprite;
        Debug.LogWarning($"[SkillIconTable] '{id}' 아이콘 미등록");
        return null;
    }
}
