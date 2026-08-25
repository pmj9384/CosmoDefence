using System;
using System.Collections.Generic;
using UnityEngine;

// 사운드 재생 전담 — 프로젝트에서 유일한 싱글톤 (유저 확정 2026-07-15: "남용만 아니면").
// 근거: 사운드는 씬 수명과 무관한 횡단 관심사라 현업 관행대로 영속 싱글톤. 게임플레이 매니저는 주입 유지.
// 씬 리로드(재시작)에도 살아남으므로 씬 매니저의 이벤트를 구독하지 않는다 — 죽은 구독 잔류 방지.
// 대신 일이 일어나는 자리에서 SoundManager.Instance?.PlaySfx(id) 직접 호출 (표준 관행).
// 클립은 "enum 이름 = 파일명" 규약으로 Resources/Sounds/에서 자동 로드 — 인스펙터 배선 0.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private int sfxChannels = 8;             // 동시 재생 폭 (옛 SoundManager 라운드로빈 이식)
    [SerializeField] private float sameClipCooldown = 0.05f;  // 동일 클립 최소 간격 — 화상/레이저 틱 스팸 방지

    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private AudioSource bgmSource;
    private AudioSource[] sfxSources;
    private readonly Dictionary<BgmClipId, AudioClip> bgmClips = new();
    private readonly Dictionary<SfxClipId, AudioClip> sfxClips = new();
    private readonly Dictionary<SfxClipId, float> lastPlayedAt = new();
    private int channelIndex;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }   // 씬 리로드 중복 가드
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 볼륨 SSOT = PlayerAccountData (세이브 체인 이관 2026-07-15). GameDataManager는 접근 시 자동 생성·로드
        BgmVolume = GameDataManager.Instance.PlayerAccountData.BgmVolume;
        SfxVolume = GameDataManager.Instance.PlayerAccountData.SfxVolume;

        CreateSources();
        LoadClips();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void CreateSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = BgmVolume;

        sfxSources = new AudioSource[sfxChannels];
        for (int i = 0; i < sfxChannels; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].loop = false;
            sfxSources[i].playOnAwake = false;
            sfxSources[i].volume = SfxVolume;
        }
    }

    // 규약 로드 — 파일이 없는 id는 조용히 재생 스킵 (BGM처럼 클립을 아직 조달 못 한 항목 허용)
    private void LoadClips()
    {
        foreach (BgmClipId id in Enum.GetValues(typeof(BgmClipId)))
        {
            var clip = Resources.Load<AudioClip>($"Sounds/Bgm/{id}");
            if (clip != null) bgmClips[id] = clip;
        }
        foreach (SfxClipId id in Enum.GetValues(typeof(SfxClipId)))
        {
            var clip = Resources.Load<AudioClip>($"Sounds/Sfx/{id}");
            if (clip != null) sfxClips[id] = clip;
            else Debug.LogWarning($"[SoundManager] SFX 클립 없음: Resources/Sounds/Sfx/{id}");
        }
    }

    // 같은 곡이면 무시(멱등) — GamePlay 재진입(스킬 선택 복귀 등)마다 불려도 음악이 안 끊긴다
    public void PlayBgm(BgmClipId id)
    {
        if (!bgmClips.TryGetValue(id, out AudioClip clip)) return;   // 미조달 클립(현재 BGM 전부) — 조용히 스킵
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm() => bgmSource.Stop();

    // "조용할 때만" 판단용 — GamePlay 재진입(스킬 선택 복귀)이 보스 BGM을 기본 곡으로 덮지 않게
    public bool IsBgmPlaying => bgmSource != null && bgmSource.isPlaying;

    // volumeScale: 클립별 크기 배율 — 고빈도 소리(타격 등)를 전체 SFX 볼륨과 별개로 죽일 때 사용
    public void PlaySfx(SfxClipId id, float volumeScale = 1f)
    {
        if (!sfxClips.TryGetValue(id, out AudioClip clip)) return;

        // 스팸 쿨다운 — 결과 화면(timeScale 0)에서도 재생돼야 하므로 unscaled 시계
        if (lastPlayedAt.TryGetValue(id, out float last) && Time.unscaledTime - last < sameClipCooldown) return;
        lastPlayedAt[id] = Time.unscaledTime;

        // 라운드로빈: 빈 채널 탐색 (옛 SoundManager 이식), 전부 사용 중이면 다음 채널 강탈
        for (int i = 0; i < sfxSources.Length; i++)
        {
            int idx = (channelIndex + i) % sfxSources.Length;
            if (sfxSources[idx].isPlaying) continue;
            Play(idx, clip, volumeScale);
            return;
        }
        Play((channelIndex + 1) % sfxSources.Length, clip, volumeScale);
    }

    private void Play(int idx, AudioClip clip, float volumeScale)
    {
        channelIndex = idx;
        sfxSources[idx].clip = clip;
        sfxSources[idx].volume = SfxVolume * volumeScale;
        sfxSources[idx].Play();
    }

    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = BgmVolume;
        GameDataManager.Instance.PlayerAccountData.BgmVolume = BgmVolume;   // 영속은 세이브 체인 몫
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        foreach (AudioSource src in sfxSources) src.volume = SfxVolume;
        GameDataManager.Instance.PlayerAccountData.SfxVolume = SfxVolume;
    }
}
