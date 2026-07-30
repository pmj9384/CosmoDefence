using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 레벨업 → 3택지 드래프트 진행자 — 킬로 쌓인 드래프트 큐, 게이지 연출 지연, 카드 개방/픽 적용,
// 연속 레벨업 체인과 상태 전환(SkillSelection↔GamePlay)을 소유한다. SkillManager에서 추출 (검수 v6 —
// 매니저는 이벤트 중계·데미지 파이프라인만). 코루틴은 러너 델리게이트 주입 — MonoBehaviour를 모른다.
public class LevelUpDraftController
{
    private readonly GameManager gameManager;
    private readonly PlayerSkills playerSkills;
    private readonly PlayerLevel playerLevel;
    private readonly BallInventory ballInventory;
    private readonly SkillSelectionPanel selectionPanel;
    private readonly System.Random rng;
    private readonly Func<IEnumerator, Coroutine> startCoroutine;

    private int pendingDrafts;        // 연속 레벨업 시 3택지를 연달아 띄우기 위한 큐
    private Coroutine openDelay;
    private const float SelectionOpenDelay = 0.35f;  // 게이지 채움 연출 시간 — 0.6은 "한 템포 늦음"으로 체감 (유저 2026-07-07)

    public int NormalBallLevel { get; private set; } = 1;   // 노멀볼 레벨 (전투 정보 ◆xN) — 채움 카드로 개수와 함께 성장

    public LevelUpDraftController(GameManager gameManager, PlayerSkills playerSkills, PlayerLevel playerLevel,
        BallInventory ballInventory, SkillSelectionPanel selectionPanel, System.Random rng,
        Func<IEnumerator, Coroutine> startCoroutine)
    {
        this.gameManager = gameManager;
        this.playerSkills = playerSkills;
        this.playerLevel = playerLevel;
        this.ballInventory = ballInventory;
        this.selectionPanel = selectionPanel;
        this.rng = rng;
        this.startCoroutine = startCoroutine;
    }

    // 킬 1건 등록 — 레벨 진행을 큐에 반영하고, 플레이 중이면 지연 개방을 건다.
    // 이미 선택 중이면 큐에만 쌓고, 선택이 끝날 때 이어서 연다 (원작 시퀀스)
    public void RegisterKill()
    {
        pendingDrafts += playerLevel.AddKill();
        if (pendingDrafts > 0 && gameManager.CurrentState == GameManager.GameState.GamePlay && openDelay == null)
            openDelay = startCoroutine(OpenSelectionAfterGaugeFill());
    }

    // 레벨업 0.35s 지연 중 퍼즈로 이탈하면 openDelay가 무산되는데, 되살릴 트리거가 다음 킬뿐이라
    // 킬 없이 죽으면 그 선택이 증발한다 (검수 v5 #5) — GamePlay 복귀 시 잔여 드래프트를 재개방
    public void TryReopenPending()
    {
        if (pendingDrafts > 0 && openDelay == null)
            openDelay = startCoroutine(OpenSelectionAfterGaugeFill());
    }

    private IEnumerator OpenSelectionAfterGaugeFill()
    {
        yield return new WaitForSeconds(SelectionOpenDelay);   // 스케일 시간 — 이 동안 게임은 계속 돈다 (원작)
        openDelay = null;
        if (pendingDrafts > 0 && gameManager.CurrentState == GameManager.GameState.GamePlay)
            OpenSelection();
    }

    private void OpenSelection()
    {
        List<SkillId> cards = SkillDraft.Draw(playerSkills, rng);
        if (cards.Count == 0)           // 채움 카드 덕에 사실상 불가능 — 방어적 안전망
        {
            pendingDrafts = 0;
            // 연속 레벨업 체인 "도중" 소진이면 상태가 SkillSelection에 잠긴 채 방치되던 실버그 —
            // 복귀시켜 줄 사람이 없으므로 여기서 직접 GamePlay로
            if (gameManager.CurrentState == GameManager.GameState.SkillSelection)
                gameManager.SetGameState(GameManager.GameState.GamePlay);
            return;
        }

        gameManager.SetGameState(GameManager.GameState.SkillSelection);
        selectionPanel.Show(cards, playerSkills, playerLevel.Level, HandleCardPicked);
    }

    private void HandleCardPicked(SkillId picked)
    {
        ApplyPick(picked);
        selectionPanel.Hide();
        pendingDrafts--;

        if (pendingDrafts > 0)
        {
            OpenSelection();            // 연속 레벨업 — 다음 드래프트 (상태는 SkillSelection 유지)
        }
        else
        {
            gameManager.SetGameState(GameManager.GameState.GamePlay);
        }
    }

    // 카드 1장 선택의 실제 효과 적용 (드래프트 UI와 분리 — 에디터 디버그 획득도 이 경로를 그대로 탄다)
    public void ApplyPick(SkillId picked)
    {
        if (picked == SkillId.NormalBall)
        {
            NormalBallLevel++;              // 채움 카드 — 노멀볼 레벨과 개수가 함께 성장 (◆xN)
            ballInventory.Add(null);
        }
        else
        {
            bool isActiveBall = playerSkills.Table[picked].kind == SkillKind.ActiveBall;
            bool wasMax = playerSkills.GetLevel(picked) >= PlayerSkills.MaxLevel;
            bool isNew = !playerSkills.Has(picked);
            bool acquired = playerSkills.Acquire(picked);   // 만석 신규·만렙이면 false(레벨 안 오름)

            // 액티브볼 볼 추가 조건: (신규 획득 성공) 또는 (만렙 재선택 = "+1개"). 개수만 늘고 레벨은 Lv3 고정.
            // 만석이라 획득 실패한 신규는 제외 — 안 그러면 레벨0(미보유) 볼이 인벤토리에 들어가 발사 시 GetLevel(0) 크래시
            if (isActiveBall && ((isNew && acquired) || wasMax))
                ballInventory.Add(picked);
        }
    }
}
